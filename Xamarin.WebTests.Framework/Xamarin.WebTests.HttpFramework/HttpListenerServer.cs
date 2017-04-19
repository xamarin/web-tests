//
// HttpListenerServer.cs
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
using Xamarin.WebTests.HttpHandlers;
using Xamarin.WebTests.Server;

namespace Xamarin.WebTests.HttpFramework {
	public sealed class HttpListenerServer : HttpServer {
		public IPortableEndPoint ListenAddress {
			get;
		}

		public ConnectionParameters Parameters {
			get;
		}

		public HttpListenerServer (IPortableEndPoint clientEndPoint, IPortableEndPoint listenAddress, HttpServerFlags flags,
		                           ConnectionParameters parameters, ISslStreamProvider sslStreamProvider)
		{
			ListenAddress = listenAddress;
			Flags = flags;
			Parameters = parameters;
			SslStreamProvider = sslStreamProvider;

			if (Parameters != null)
				Flags |= HttpServerFlags.SSL;

			if ((Flags & HttpServerFlags.SSL) != 0) {
				if (SslStreamProvider == null) {
					var factory = DependencyInjector.Get<ConnectionProviderFactory> ();
					SslStreamProvider = factory.DefaultSslStreamProvider;
				}
			}

			Uri = new Uri (string.Format ("http{0}://{1}:{2}/", SslStreamProvider != null ? "s" : "", clientEndPoint.Address, clientEndPoint.Port));
		}

		public override HttpServerFlags Flags {
			get;
		}

		public override bool UseSSL {
			get { return SslStreamProvider != null; }
		}

		public ISslStreamProvider SslStreamProvider {
			get;
		}

		public override Uri Uri  {
			get;
		}

		public override Uri TargetUri => Uri;

		public override IWebProxy GetProxy () => null;

		SystemHttpListener currentListener;

		Dictionary<string, Handler> handlers = new Dictionary<string, Handler> ();

		public override void RegisterHandler (string path, Handler handler)
		{
			handlers.Add (path, handler);
		}

		protected internal override Handler GetHandler (string path)
		{
			var handler = handlers [path];
			handlers.Remove (path);
			return handler;
		}

		public override Task Start (TestContext ctx, CancellationToken cancellationToken)
		{
			var listener = new SystemHttpListener (ctx, this);
			if (Interlocked.CompareExchange (ref currentListener, listener, null) != null)
				throw new InternalErrorException ();
			return listener.Start ();
		}

		public override async Task Stop (TestContext ctx, CancellationToken cancellationToken)
		{
			var listener = Interlocked.Exchange (ref currentListener, null);
			if (listener == null || listener.TestContext != ctx)
				throw new InternalErrorException ();
			try {
				await listener.Stop ().ConfigureAwait (false);
			} catch {
				if ((Flags & HttpServerFlags.ExpectException) == 0)
					throw;
			}
		}

		protected override async Task<HttpConnection> DoCreateConnection (TestContext ctx, Stream stream, CancellationToken cancellationToken)
		{
			if (SslStreamProvider == null)
				return new StreamConnection (ctx, this, stream, null);

			var sslStream = await SslStreamProvider.CreateServerStreamAsync (stream, Parameters, cancellationToken).ConfigureAwait (false);
			return new StreamConnection (ctx, this, sslStream.AuthenticatedStream, sslStream);
		}
	}
}
