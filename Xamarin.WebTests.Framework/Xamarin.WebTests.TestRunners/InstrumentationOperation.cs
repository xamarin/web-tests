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
	using Server;

	public class InstrumentationOperation : HttpOperation
	{
		public InstrumentationTestRunner Parent {
			get;
		}

		public InstrumentationOperationType Type {
			get;
		}

		internal override ListenerHandler ListenerHandler => Parent;

		internal InstrumentationOperation (
			InstrumentationTestRunner parent,
			InstrumentationOperationType type,
			HttpOperationFlags? flags = null,
			HttpStatusCode? expectedStatus = null,
			WebExceptionStatus? expectedError = null)
			: base (parent.Server, parent.ME,
			        flags ?? parent.OperationFlags,
			        expectedStatus ?? parent.ExpectedStatus,
			        expectedError ?? parent.ExpectedError)
		{
			Parent = parent;
			Type = type;
		}

		protected sealed override Request CreateRequest (TestContext ctx, Uri uri)
		{
			return Parent.CreateRequest (ctx, this, uri);
		}

		protected sealed override void ConfigureRequest (TestContext ctx, Uri uri, Request request)
		{
			Parent.ConfigureRequest (ctx, this, request, uri);

			var proxy = Parent.Server.GetProxy ();
			if (proxy != null)
				request.SetProxy (proxy);
		}

		protected sealed override Task<Response> RunInner (TestContext ctx, Request request, CancellationToken cancellationToken)
		{
			ctx.LogDebug (2, $"{ME} RUN INNER");

			return ctx.RunWithDisposableContext (
				innerCtx => Parent.Run (
					innerCtx, request, cancellationToken));
		}

		protected override bool CheckResponseInner (TestContext ctx, Response response) => Parent.CheckResponse (ctx, response);

		internal IPEndPoint RemoteEndPoint {
			get;
			private set;
		}

		StreamInstrumentation instrumentation;

		internal sealed override Stream CreateNetworkStream (TestContext ctx, Socket socket, bool ownsSocket)
		{
			RemoteEndPoint = (IPEndPoint)socket.RemoteEndPoint;

			instrumentation = new StreamInstrumentation (ctx, ME, socket, ownsSocket);

			if (Parent.ConfigureNetworkStream (ctx, instrumentation))
				InstallReadHandler (ctx);

			return instrumentation;
		}

		void InstallReadHandler (TestContext ctx)
		{
			ctx.Assert (Server.UseSSL, "must use SSL");
			instrumentation.OnNextRead ((b, o, s, f, c) => ReadHandler (ctx, b, o, s, f, c));
		}

		async Task<int> ReadHandler (TestContext ctx,
					     byte[] buffer, int offset, int size,
					     StreamInstrumentation.AsyncReadFunc func,
					     CancellationToken cancellationToken)
		{
			cancellationToken.ThrowIfCancellationRequested ();

			var ret = await func (buffer, offset, size, cancellationToken).ConfigureAwait (false);

			if (await Parent.ReadHandler (
				ctx, this, buffer, offset, size, ret, cancellationToken))
				InstallReadHandler (ctx);

			return ret;
		}

		protected sealed override void Destroy ()
		{
			instrumentation?.Dispose ();
			instrumentation = null;
		}
	}
}
