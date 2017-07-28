//
// InstrumentationListener.cs
//
// Author:
//       Martin Baulig <mabaul@microsoft.com>
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
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using Xamarin.AsyncTests;

namespace Xamarin.WebTests.Server
{
	using HttpFramework;

	class InstrumentationListener : Listener
	{
		LinkedList<InstrumentationListenerContext> connections;

		public InstrumentationListener (TestContext ctx, HttpServer server, ListenerBackend backend)
			: base (ctx, server, backend)
		{
			connections = new LinkedList<InstrumentationListenerContext> ();
		}

		(ListenerContext context, bool reused) FindOrCreateContext (HttpOperation operation, bool reuse)
		{
			lock (this) {
				var iter = connections.First;
				while (reuse && iter != null) {
					var node = iter.Value;
					iter = iter.Next;

					if (node.StartOperation (operation))
						return (node, true);
				}

				var context = new InstrumentationListenerContext (this);
				context.StartOperation (operation);
				connections.AddLast (context);
				return (context, false);
			}
		}

		internal void Continue (TestContext ctx, InstrumentationListenerContext context, bool keepAlive)
		{
			lock (this) {
				ctx.LogDebug (5, $"{ME} CONTINUE: {keepAlive}");
				if (keepAlive) {
					context.Continue ();
					return;
				}
				connections.Remove (context);
				context.Dispose ();
			}
		}

		public ListenerContext CreateContext (TestContext ctx, HttpOperation operation, bool reusing)
		{
			var (context, _) = FindOrCreateContext (operation, reusing);
			return context;
		}

		public async Task<ListenerContext> CreateContext (
			TestContext ctx, HttpOperation operation, CancellationToken cancellationToken)
		{
			var reusing = !operation.HasAnyFlags (HttpOperationFlags.DontReuseConnection);
			var (context, reused) = FindOrCreateContext (operation, reusing);

			if (reused && operation.HasAnyFlags (HttpOperationFlags.ClientUsesNewConnection)) {
				try {
					await context.Connection.ReadRequest (ctx, cancellationToken).ConfigureAwait (false);
					throw ctx.AssertFail ("Expected client to use a new connection.");
				} catch (OperationCanceledException) {
					throw;
				} catch (Exception ex) {
					ctx.LogDebug (2, $"{ME} EXPECTED EXCEPTION: {ex.GetType ()} {ex.Message}");
				}
				context.Dispose ();
				(context, reused) = FindOrCreateContext (operation, false);
			}

			return context;
		}

		protected override ListenerOperation CreateOperation (HttpOperation operation, Uri uri)
		{
			return new Operation (this, operation, uri);
		}

		protected override void Close ()
		{
			TestContext.LogDebug (5, $"{ME}: CLOSE ALL");

			var iter = connections.First;
			while (iter != null) {
				var node = iter.Value;
				iter = iter.Next;

				node.Dispose ();
				connections.Remove (node);
			}
		}

		class Operation : ListenerOperation
		{
			public Operation (InstrumentationListener listener, HttpOperation operation, Uri uri)
				: base (listener, operation, uri)
			{
			}

			public override Task ServerInitTask {
				get {
					throw new NotImplementedException ();
				}
			}

			public override Task ServerStartTask {
				get {
					throw new NotImplementedException ();
				}
			}
		}
	}
}
