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
		TcpListener listener;
		TaskCompletionSource<bool> tcs;
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

		public Listener (IPAddress address, int port, bool ssl)
		{
			this.ssl = ssl;
			listener = new TcpListener (address, port);
			uri = new Uri (string.Format ("http{0}://{1}:{2}/", ssl ? "s" : "", address, port));
			listener.Start ();

			listener.BeginAcceptSocket (AcceptSocketCB, null);
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
				listener.Stop ();
			}

			try {
				if (task != null)
					task.Wait ();
			} catch {
				;
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
				socket = listener.EndAcceptSocket (ar);
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
			}

			try {
				HandleConnection (socket);
				t.SetResult (true);
			} catch (Exception ex) {
				Console.Error.WriteLine ("ACCEPT SOCKET EX: {0}", ex);
				t.SetException (ex);
			} finally {
				socket.Close ();
				lock (this) {
					tcs = null;
					if (!abortRequested)
						listener.BeginAcceptSocket (AcceptSocketCB, null);
				}
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

		void HandleConnection (Socket socket)
		{
			var stream = CreateStream (socket);
			HandleConnection (socket, stream);
		}

		protected abstract void HandleConnection (Socket socket, Stream stream);
	}
}
