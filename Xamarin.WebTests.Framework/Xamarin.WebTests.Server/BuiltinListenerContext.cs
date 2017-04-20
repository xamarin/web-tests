//
// BuiltinListenerContext.cs
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
using System.Linq;
using System.Text;
using System.Threading;
using System.Reflection;
using System.Net.Sockets;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Net.Security;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using SD = System.Diagnostics;

using Xamarin.AsyncTests;
using Xamarin.AsyncTests.Portable;
using Xamarin.WebTests.HttpFramework;

namespace Xamarin.WebTests.Server
{
	abstract class BuiltinListenerContext : IDisposable
	{
		public BuiltinListener Listener {
			get;
		}

		protected BuiltinListenerContext (BuiltinListener listener, IPEndPoint remoteEndPoint)
		{
			Listener = listener;
		}

		public abstract Task<HttpConnection> CreateConnection (TestContext ctx, CancellationToken cancellationToken);

		int disposed;

		protected abstract void Close ();

		void Dispose (bool disposing)
		{
			if (Interlocked.CompareExchange (ref disposed, 1, 0) != 0)
				return;

			Close ();
		}

		public void Dispose ()
		{
			Dispose (true);
			GC.SuppressFinalize (this);
		}
	}
}
