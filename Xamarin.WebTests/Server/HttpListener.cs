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
using System.Threading;
using System.Threading.Tasks;

namespace Xamarin.WebTests.Server
{
	using Handlers;

	public class HttpListener : Listener
	{
		Dictionary<string,Handler> handlers = new Dictionary<string, Handler> ();
		List<Handler> allHandlers = new List<Handler> ();

		static int nextId;

		public HttpListener (IPAddress address, int port, bool reuseConnection, bool ssl)
			: base (address, port, reuseConnection, ssl)
		{
		}

		protected override void OnStop ()
		{
			foreach (var handler in allHandlers)
				handler.Reset ();

			base.OnStop ();
		}

		public Uri RegisterHandler (Handler handler)
		{
			var path = string.Format ("/{0}/{1}/", handler.GetType (), ++nextId);
			handlers.Add (path, handler);
			allHandlers.Add (handler);
			return new Uri (Uri, path);
		}

		public void RegisterHandler (string path, Handler handler)
		{
			handlers.Add (path, handler);
			allHandlers.Add (handler);
		}

		protected override bool HandleConnection (
			Socket socket, StreamReader reader, StreamWriter writer, CancellationToken cancellationToken)
		{
			var connection = new HttpConnection (this, reader, writer);
			var request = connection.ReadRequest ();

			var path = request.Path;
			var handler = handlers [path];
			handlers.Remove (path);

			return handler.HandleRequest (connection, request);
		}
	}
}

