//
// ProxyListener.cs
//
// Author:
//       Martin Baulig <martin.baulig@xamarin.com>
//
// Copyright (c) 2014 Xamarin Inc. (http://www.xamarin.com)
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Xamarin.WebTests.Server
{
	using Framework;

	public class ProxyListener : Listener
	{
		readonly HttpListener target;
		ProxyAuthManager authManager;

		public ProxyListener (HttpListener target, IPAddress address, int port, AuthenticationType authType)
			: base (address, port, false, false)
		{
			this.target = target;
			if (authType != AuthenticationType.None)
				authManager = new ProxyAuthManager (authType);
		}

		protected override bool HandleConnection (Socket socket, StreamReader reader, StreamWriter writer)
		{
			var connection = new Connection (reader, writer);
			var request = connection.ReadRequest ();

			var remoteAddress = ((IPEndPoint)socket.RemoteEndPoint).Address;
			request.AddHeader ("X-Forwarded-For", remoteAddress);

			if (authManager != null) {
				string authHeader;
				if (!request.Headers.TryGetValue ("Proxy-Authorization", out authHeader))
					authHeader = null;
				var response = authManager.HandleAuthentication (request, authHeader);
				if (response != null) {
					request.ReadBody ();
					connection.WriteResponse (response);
					return false;
				}

				// HACK: Mono rewrites chunked requests into non-chunked.
				request.AddHeader ("X-Mono-Redirected", "true");
			}

			if (request.Method.Equals ("CONNECT")) {
				CreateTunnel (connection, socket, reader, writer, request);
				return false;
			}

			var targetSocket = new Socket (AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
			targetSocket.Connect (target.Uri.Host, target.Uri.Port);

			using (var targetStream = new NetworkStream (targetSocket)) {
				var targetReader = new StreamReader (targetStream);
				var targetWriter = new StreamWriter (targetStream);
				targetWriter.AutoFlush = true;

				var targetConnection = new ProxyConnection (connection, targetReader, targetWriter);
				targetConnection.HandleRequest (request);

				targetConnection.Close ();
			}

			targetSocket.Close ();
			return false;
		}

		IPEndPoint GetConnectEndpoint (HttpRequest request)
		{
			var pos = request.Path.IndexOf (':');
			if (pos < 0)
				return new IPEndPoint (IPAddress.Parse (request.Path), 443);

			var address = IPAddress.Parse (request.Path.Substring (0, pos));
			var port = int.Parse (request.Path.Substring (pos + 1));
			return new IPEndPoint (address, port);
		}

		void CreateTunnel (Connection connection, Socket socket, StreamReader reader, StreamWriter writer, HttpRequest request)
		{
			var targetEndpoint = GetConnectEndpoint (request);
			var targetSocket = new Socket (AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
			targetSocket.Connect (targetEndpoint);
			targetSocket.NoDelay = true;

			var connectionEstablished = new HttpResponse (HttpStatusCode.OK, HttpProtocol.Http10, "Connection established");
			connectionEstablished.Write (writer);

			RunTunnel (socket, targetSocket);
			targetSocket.Close ();
		}

		void RunTunnel (Socket input, Socket output)
		{
			bool doneSending = false;
			bool doneReading = false;
			while (!doneSending || !doneReading) {
				CheckCancellation ();

				var readList = new List<Socket> ();
				if (!doneSending)
					readList.Add (input);
				if (!doneReading)
					readList.Add (output);

				Socket.Select (readList, null, null, 500);

				if (readList.Contains (input)) {
					if (!Copy (input, output))
						doneSending = true;
				}

				if (readList.Contains (output)) {
					if (Copy (output, input))
						continue;

					doneReading = true;
					while (!doneSending && Copy (input, output)) {
						;
					}
					break;
				}
			}
		}

		bool Copy (Socket input, Socket output)
		{
			var buffer = new byte [4096];
			var ret = input.Receive (buffer);
			if (ret == 0) {
				try {
					output.Shutdown (SocketShutdown.Send);
				} catch {
					;
				}
				return false;
			}

			var ret2 = output.Send (buffer, ret, SocketFlags.None);
			if (ret2 != ret)
				throw new InvalidOperationException ();
			return true;
		}

		class ProxyAuthManager : AuthenticationManager
		{
			public ProxyAuthManager (AuthenticationType type)
				: base (type)
			{ }

			protected override HttpResponse OnUnauthenticated (HttpRequest request, string token, bool omitBody)
			{
				var response = new HttpResponse (HttpStatusCode.ProxyAuthenticationRequired);
				response.AddHeader ("Proxy-Authenticate", token);
				return response;
			}
		}
	}
}
