//
// InstrumentationOperation.cs
//
// Author:
//       Martin Baulig <mabaul@microsoft.com>
//
// Copyright (c) 2018 Xamarin Inc. (http://www.xamarin.com)
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
using System.Text;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using System.Collections;
using System.Collections.Generic;
using Xamarin.AsyncTests;
using Xamarin.AsyncTests.Constraints;
using Xamarin.AsyncTests.Portable;

namespace Xamarin.WebTests.TestRunners
{
	using ConnectionFramework;
	using HttpFramework;
	using HttpHandlers;
	using TestFramework;

	abstract class InstrumentationOperation : HttpOperation
	{
		public InstrumentationTestRunner Parent {
			get;
		}

		public InstrumentationOperationType Type {
			get;
		}

		public InstrumentationOperation (InstrumentationTestRunner parent, string me, Handler handler,
						 InstrumentationOperationType type, HttpOperationFlags flags,
						 HttpStatusCode expectedStatus, WebExceptionStatus expectedError)
			: base (parent.Server, me, handler, flags, expectedStatus, expectedError)
		{
			Parent = parent;
			Type = type;
		}

		StreamInstrumentation instrumentation;

		internal override Stream CreateNetworkStream (TestContext ctx, Socket socket, bool ownsSocket)
		{
			instrumentation = new StreamInstrumentation (ctx, ME, socket, ownsSocket);

			ConfigureNetworkStream (ctx, instrumentation);

			return instrumentation;
		}

		protected abstract void ConfigureNetworkStream (TestContext ctx, StreamInstrumentation instrumentation);

		protected void InstallReadHandler (TestContext ctx)
		{
			instrumentation.OnNextRead ((b, o, s, f, c) => ReadHandler (ctx, b, o, s, f, c));
		}

		async Task<int> ReadHandler (TestContext ctx,
					     byte[] buffer, int offset, int size,
					     StreamInstrumentation.AsyncReadFunc func,
					     CancellationToken cancellationToken)
		{
			cancellationToken.ThrowIfCancellationRequested ();

			var ret = await func (buffer, offset, size, cancellationToken).ConfigureAwait (false);

			if (await ReadHandler (ctx, buffer, offset, size, ret, cancellationToken))
				InstallReadHandler (ctx);

			return ret;
		}

		protected virtual Task<bool> ReadHandler (
			TestContext ctx,
			byte[] buffer, int offset, int size, int ret,
			CancellationToken cancellationToken)
		{
			return Parent.ReadHandler (ctx, Type, ret, cancellationToken);
		}

		protected override void Destroy ()
		{
			instrumentation?.Dispose ();
			instrumentation = null;
		}
	}
}
