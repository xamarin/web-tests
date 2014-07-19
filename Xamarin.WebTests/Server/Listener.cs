//
// Listener.cs
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
using System.Text;
using System.Threading;
using System.Reflection;
using System.Net.Sockets;
using System.Net.Security;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Security.Cryptography.X509Certificates;

namespace Xamarin.WebTests.Server
{
	public abstract class Listener
	{
		bool abortRequested;
		Socket server;
		TaskCompletionSource<bool> tcs;
		CancellationTokenSource cts;
		bool ssl;
		Uri uri;

		static X509Certificate2 cert;

		static Listener ()
		{
			// openssl req -x509 -newkey rsa:2048 -keyout key.pem -out cert.pem -days XXX
			// openssl pkcs12 -export -in cert.pem -inkey key.pem -out cert.pfx
			using (var stream = GetResourceStream ("cert.pfx")) {
				var buffer = new byte [stream.Length];
				var ret = stream.Read (buffer, 0, buffer.Length);
				if (ret != buffer.Length)
					throw new InvalidOperationException ();
				cert = new X509Certificate2 (buffer, "monkey", X509KeyStorageFlags.Exportable);
			}

			ServicePointManager.ServerCertificateValidationCallback = (o,c,chain,errors) => {
				return c.GetCertHashString ().Equals (cert.GetCertHashString ());
			};
		}

		static Stream GetResourceStream (string name)
		{
			var asm = Assembly.GetExecutingAssembly ();
			return asm.GetManifestResourceStream ("Xamarin.WebTests.Server." + name);
		}

		public Listener (IPAddress address, int port, bool reuseConnection, bool ssl)
		{
			this.ssl = ssl;

			uri = new Uri (string.Format ("http{0}://{1}:{2}/", ssl ? "s" : "", address, port));

			server = new Socket (AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
			server.Bind (new IPEndPoint (address, port));
			server.Listen (1);

			server.BeginAccept (AcceptSocketCB, null);
		}

		public Uri Uri {
			get { return uri; }
		}

		public void Stop ()
		{
			Task<bool> task = null;
			lock (this) {
				if (abortRequested)
					return;
				abortRequested = true;
				if (tcs != null)
					task = tcs.Task;
				Close (server);
				server = null;
				if (cts != null)
					cts.Cancel ();
			}

			try {
				if (task != null)
					task.Wait ();
			} catch (Exception ex) {
				Console.Error.WriteLine ("STOP EX: {0}", ex);
				throw;
			}

			OnStop ();
		}

		protected virtual void OnStop ()
		{
		}

		void AcceptSocketCB (IAsyncResult ar)
		{
			Socket socket;
			try {
				socket = server.EndAccept (ar);
			} catch {
				if (abortRequested)
					return;
				throw;
			}

			TaskCompletionSource<bool> t;
			lock (this) {
				if (abortRequested)
					return;
				t = tcs = new TaskCompletionSource<bool> ();
				cts = new CancellationTokenSource ();
				cts.Token.Register (() => Close (socket));
			}

			try {
				HandleConnection (socket);
				Close (socket);
				socket = null;
				t.SetResult (true);
			} catch (Exception ex) {
				Console.Error.WriteLine ("ACCEPT SOCKET EX: {0}", ex);
				t.SetException (ex);
			} finally {
				lock (this) {
					tcs = null;
					cts = null;
					if (!abortRequested)
						server.BeginAccept (AcceptSocketCB, null);
				}
			}
		}

		void Close (Socket socket)
		{
			try {
				socket.Shutdown (SocketShutdown.Both);
			} catch {
				;
			} finally {
				socket.Close ();
				socket.Dispose ();
			}
		}

		Stream CreateStream (Socket socket)
		{
			var stream = new NetworkStream (socket);
			if (!ssl)
				return stream;

			var authStream = new SslStream (stream);
			authStream.AuthenticateAsServer (cert);
			return authStream;
		}

		bool IsStillConnected (Socket socket, StreamReader reader)
		{
			try {
				if (!socket.Poll (-1, SelectMode.SelectRead))
					return false;
				return socket.Available > 0 && !reader.EndOfStream;
			} catch {
				return false;
			}
		}

		void HandleConnection (Socket socket)
		{
			var stream = CreateStream (socket);
			var reader = new StreamReader (stream, Encoding.ASCII);
			var writer = new StreamWriter (stream, Encoding.ASCII);
			writer.AutoFlush = true;

			while (!abortRequested) {
				var wantToReuse = HandleConnection (socket, reader, writer);
				if (!wantToReuse)
					break;

				bool connectionAvailable = IsStillConnected (socket, reader);
				if (!connectionAvailable && !abortRequested)
					throw new InvalidOperationException ("Expecting another connection, but socket has been shut down.");
			}
		}

		protected abstract bool HandleConnection (Socket socket, StreamReader reader, StreamWriter writer);

		protected void CheckCancellation ()
		{
			lock (this) {
				if (cts != null)
					cts.Token.ThrowIfCancellationRequested ();
				if (abortRequested)
					throw new OperationCanceledException ();
			}
		}
	}
}
