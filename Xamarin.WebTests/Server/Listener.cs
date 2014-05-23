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
using System.Net.Sockets;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Xamarin.WebTests.Server
{
	public abstract class Listener
	{
		bool abortRequested;
		TcpListener listener;
		TaskCompletionSource<bool> tcs;
		Uri uri;

		public Listener (IPAddress address, int port)
		{
			listener = new TcpListener (address, port);
			uri = new Uri (string.Format ("http://{0}:{1}/", address, port));
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

		protected abstract void HandleConnection (Socket socket);
	}
}
