﻿﻿﻿//
// Connection.cs
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
using System.Net.Security;
using System.Text;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Xamarin.AsyncTests;
using Xamarin.AsyncTests.Portable;
using Xamarin.AsyncTests.Constraints;

namespace Xamarin.WebTests.HttpFramework
{
	using ConnectionFramework;
	using Server;

	public abstract class HttpConnection : IDisposable
	{
		public HttpServer Server {
			get;
		}

		public abstract SslStream SslStream {
			get;
		}

		internal HttpConnection (HttpServer server, IPEndPoint remoteEndPoint)
		{
			Server = server;
			RemoteEndPoint = remoteEndPoint;
		}

		internal IPEndPoint RemoteEndPoint {
			get;
		}

		internal abstract bool IsStillConnected ();

		public abstract Task Initialize (TestContext ctx, CancellationToken cancellationToken);

		public abstract Task<bool> HasRequest (CancellationToken cancellationToken);

		public abstract Task<HttpRequest> ReadRequest (CancellationToken cancellationToken);

		public abstract Task<HttpResponse> ReadResponse (CancellationToken cancellationToken);

		internal abstract Task WriteRequest (HttpRequest request, CancellationToken cancellationToken);

		internal abstract Task WriteResponse (HttpResponse response, CancellationToken cancellationToken);

		int disposed;

		protected abstract void Close ();

		public void Dispose ()
		{
			if (Interlocked.CompareExchange (ref disposed, 1, 0) == 0)
				Close ();
			GC.SuppressFinalize (this);
		}
	}
}

