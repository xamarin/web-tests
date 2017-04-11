//
// HttpBackend.cs
//
// Author:
//       Martin Baulig <martin.baulig@xamarin.com>
//
// Copyright (c) 2017 Xamarin Inc. (http://www.xamarin.com)
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
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xamarin.AsyncTests;
using Xamarin.AsyncTests.Portable;
using Xamarin.WebTests.ConnectionFramework;
using Xamarin.WebTests.HttpHandlers;
using Xamarin.WebTests.Server;

namespace Xamarin.WebTests.HttpFramework {
	[FriendlyName ("HttpBackend")]
	public abstract class HttpBackend {
		public abstract ListenerFlags Flags {
			get;
		}

		public abstract bool UseSSL {
			get;
		}

		public abstract Uri Uri {
			get;
		}

		public abstract Uri TargetUri {
			get;
		}

		public abstract IWebProxy GetProxy ();

		public abstract Task Start (TestContext ctx, HttpServer server, CancellationToken cancellationToken);

		public abstract Task Stop (TestContext ctx, HttpServer server, CancellationToken cancellationToken);

		public abstract HttpConnection CreateConnection (TestContext ctx, HttpServer server, Stream stream);

		public int CountRequests => countRequests;

		static long nextId;
		int countRequests;
		Dictionary<string, Handler> handlers = new Dictionary<string, Handler> ();

		public Uri RegisterHandler (Handler handler)
		{
			var path = string.Format ("/{0}/{1}/", handler.GetType (), ++nextId);
			handlers.Add (path, handler);
			return new Uri (TargetUri, path);
		}

		public void RegisterHandler (string path, Handler handler)
		{
			handlers.Add (path, handler);
		}

		public bool HandleConnection (TestContext ctx, HttpConnection connection, HttpRequest request)
		{
			++countRequests;

			var path = request.Path;
			var handler = handlers[path];
			handlers.Remove (path);

			return handler.HandleRequest (connection, request);
		}

		protected virtual string MyToString ()
		{
			var sb = new StringBuilder ();
			if ((Flags & ListenerFlags.ReuseConnection) != 0)
				sb.Append ("shared");
			if (UseSSL) {
				if (sb.Length > 0)
					sb.Append (",");
				sb.Append ("ssl");
			}
			return sb.ToString ();
		}

		public override string ToString ()
		{
			var description = MyToString ();
			var padding = string.IsNullOrEmpty (description) ? string.Empty : ": ";
			return string.Format ("[{0}{1}{2}]", GetType ().Name, padding, description);
		}
	}
}
