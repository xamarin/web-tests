//
// BuiltinHttpBackend.cs
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
using Xamarin.AsyncTests;
using Xamarin.AsyncTests.Portable;
using Xamarin.WebTests.ConnectionFramework;
using Xamarin.WebTests.Server;

namespace Xamarin.WebTests.HttpFramework {
	public sealed class BuiltinHttpBackend : HttpBackend {
		public IPortableEndPoint ListenAddress {
			get;
		}
		public ConnectionParameters Parameters {
			get;
		}

		public BuiltinHttpBackend (IPortableEndPoint clientEndPoint, IPortableEndPoint listenAddress, ListenerFlags flags,
		                           ConnectionParameters parameters, ISslStreamProvider sslStreamProvider)
		{
			ListenAddress = listenAddress;
			Flags = flags;
			Parameters = parameters;
			SslStreamProvider = sslStreamProvider;

			if (Parameters != null)
				Flags |= ListenerFlags.SSL;

			if ((Flags & ListenerFlags.SSL) != 0) {
				if (SslStreamProvider == null) {
					var factory = DependencyInjector.Get<ConnectionProviderFactory> ();
					SslStreamProvider = factory.DefaultSslStreamProvider;
				}
			}

			Uri = new Uri (string.Format ("http{0}://{1}:{2}/", SslStreamProvider != null ? "s" : "", clientEndPoint.Address, clientEndPoint.Port));
		}

		public BuiltinHttpBackend (Uri uri, IPortableEndPoint listenAddress, ListenerFlags flags,
		                           ConnectionParameters parameters, ISslStreamProvider sslStreamProvider)
		{
			Uri = uri;
			ListenAddress = listenAddress;
			Flags = flags;
			Parameters = parameters;
			SslStreamProvider = sslStreamProvider;

			if (SslStreamProvider == null) {
				var factory = DependencyInjector.Get<ConnectionProviderFactory> ();
				SslStreamProvider = factory.DefaultSslStreamProvider;
			}
		}

		public override ListenerFlags Flags {
			get;
		}

		public override bool UseSSL {
			get { return SslStreamProvider != null; }
		}

		public ISslStreamProvider SslStreamProvider {
			get;
		}

		public override Uri Uri {
			get;
		}

		public override Uri TargetUri => Uri;

		public override IWebProxy GetProxy ()
		{
			return null;
		}

		BuiltinHttpListener currentListener;

		public override Task Start (TestContext ctx, HttpServer server, CancellationToken cancellationToken)
		{
			var listener = new BuiltinHttpListener (ctx, this, server);
			if (Interlocked.CompareExchange (ref currentListener, listener, null) != null)
				throw new InternalErrorException ();
			return listener.Start ();
		}

		public override async Task Stop (TestContext ctx, HttpServer server, CancellationToken cancellationToken)
		{
			var listener = Interlocked.Exchange (ref currentListener, null);
			if (listener == null || listener.Context != ctx)
				throw new InternalErrorException ();
			try {
				await listener.Stop ().ConfigureAwait (false);
			} catch {
				if ((Flags & ListenerFlags.ExpectException) == 0)
					throw;
			}
		}

		public override HttpConnection CreateConnection (TestContext ctx, HttpServer server, Stream stream)
		{
			if (SslStreamProvider == null)
				return new StreamConnection (ctx, server, stream, null);

			var sslStream = SslStreamProvider.CreateServerStream (stream, Parameters);
			return new StreamConnection (ctx, server, sslStream.AuthenticatedStream, sslStream);
		}
	}
}
