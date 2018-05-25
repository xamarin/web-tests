﻿//
// BuiltinHttpServer.cs
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
using System.IO;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using Xamarin.AsyncTests;
using Xamarin.AsyncTests.Portable;
using Xamarin.WebTests.ConnectionFramework;
using Xamarin.WebTests.Server;
using Xamarin.WebTests.HttpHandlers;

namespace Xamarin.WebTests.HttpFramework {
	public sealed class BuiltinHttpServer : HttpServer
	{
		public BuiltinHttpServer (EndPoint clientEndPoint, EndPoint listenAddress, HttpServerFlags flags,
					  ConnectionParameters parameters, ISslStreamProvider sslStreamProvider)
			: base (listenAddress, flags, parameters, sslStreamProvider)
		{
			Uri = new Uri ($"http{(SslStreamProvider != null ? "s" : "")}://{clientEndPoint}/");
		}

		public BuiltinHttpServer (Uri uri, EndPoint listenAddress, HttpServerFlags flags,
					  ConnectionParameters parameters, ISslStreamProvider sslStreamProvider)
			: base (listenAddress, flags | HttpServerFlags.SSL, parameters, sslStreamProvider)
		{
			Uri = uri;
		}

		public override Uri Uri {
			get;
		}

		public override Uri TargetUri => Uri;

		public override IWebProxy GetProxy ()
		{
			return null;
		}

		Listener currentListener;
		ListenerBackend currentBackend;
		int initialized;
		int started;

		public void Initialize (TestContext ctx)
		{
			if (Interlocked.CompareExchange (ref initialized, 1, 0) != 0)
				return;

			if ((Flags & HttpServerFlags.HttpListener) != 0)
				currentBackend = new HttpListenerBackend (ctx, this);
			else
				currentBackend = new SocketBackend (ctx, this);

			var type = (Flags & HttpServerFlags.ParallelListener) != 0 ?
				ListenerType.Parallel : ListenerType.Instrumentation;

			currentListener = new Listener (ctx, this, type, currentBackend);
		}

		public override Task Start (TestContext ctx, CancellationToken cancellationToken)
		{
			if (Interlocked.CompareExchange (ref started, 1, 0) != 0)
				throw new InternalErrorException ();

			Initialize (ctx);

			currentListener.Start ();

			return Handler.CompletedTask;
		}

		public override async Task Stop (TestContext ctx, CancellationToken cancellationToken)
		{
			var listener = Interlocked.Exchange (ref currentListener, null);
			if (listener == null || listener.TestContext != ctx)
				throw new InternalErrorException ();
			try {
				await listener.Shutdown ().ConfigureAwait (false);
				listener.Dispose ();
			} catch {
				if ((Flags & HttpServerFlags.ExpectException) == 0)
					throw;
			} finally {
				currentBackend = null;
				started = 0;
				initialized = 0;
			}
		}

		internal override Listener Listener {
			get { return currentListener; }
		}

		internal override void CloseAll ()
		{
			currentListener.Dispose ();
		}
	}
}
