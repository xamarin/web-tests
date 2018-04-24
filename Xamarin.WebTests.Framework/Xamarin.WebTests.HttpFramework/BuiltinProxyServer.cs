//
// BuiltinProxyServer.cs
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
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Xamarin.AsyncTests;
using Xamarin.AsyncTests.Portable;
using Xamarin.WebTests.Server;
using Xamarin.WebTests.HttpHandlers;

namespace Xamarin.WebTests.HttpFramework {
	public sealed class BuiltinProxyServer : HttpServer {
		public HttpServer Target {
			get;
		}

		public BuiltinProxyServer (HttpServer target, IPortableEndPoint listenAddress, HttpServerFlags flags,
		                           AuthenticationType proxyAuth = AuthenticationType.None)
			: base (listenAddress, GetFlags (flags, target, proxyAuth), null, null)
		{
			Target = target;
			AuthenticationType = proxyAuth;

			Uri = new Uri (string.Format ("http://{0}:{1}/", ListenAddress.Address, ListenAddress.Port));
		}

		static HttpServerFlags GetFlags (HttpServerFlags flags, HttpServer target, AuthenticationType proxyAuth)
		{
			flags |= HttpServerFlags.Proxy;
			if (target.UseSSL)
				flags |= HttpServerFlags.ProxySSL;
			if (proxyAuth != AuthenticationType.None)
				flags |= HttpServerFlags.ProxyAuthentication;
			return flags;
		}

		public override Uri Uri {
			get;
		}

		public override Uri TargetUri => Target.Uri;

		public ICredentials Credentials {
			get; set;
		}

		public AuthenticationType AuthenticationType {
			get;
		}

		public AuthenticationManager AuthenticationManager {
			get; private set;
		}

		Listener currentListener;
		ProxyBackend currentBackend;

		internal override Listener Listener {
			get { return currentListener; }
		}

		public override async Task Start (TestContext ctx, CancellationToken cancellationToken)
		{
			var backend = new ProxyBackend (ctx, this);
			if (Interlocked.CompareExchange (ref currentBackend, backend, null) != null)
				throw new InternalErrorException ();

			if (AuthenticationType != AuthenticationType.None)
				AuthenticationManager = new AuthenticationManager (AuthenticationType, true);

			await Target.Start (ctx, cancellationToken).ConfigureAwait (false);

			var listener = new Listener (ctx, this, ListenerType.Proxy, backend);
			listener.Start ();
			currentListener = listener;
		}

		public override async Task Stop (TestContext ctx, CancellationToken cancellationToken)
		{
			var listener = Interlocked.Exchange (ref currentListener, null);
			if (listener == null || listener.TestContext != ctx)
				throw new InternalErrorException ();
			try {
				listener.Dispose ();
				await Target.Stop (ctx, cancellationToken);
			} catch {
				if ((Flags & HttpServerFlags.ExpectException) == 0)
					throw;
			} finally {
				currentBackend = null;
				AuthenticationManager = null;
			}
		}

		internal override void CloseAll ()
		{
			currentListener.Dispose ();
			Target.CloseAll ();
		}

		public override IWebProxy GetProxy ()
		{
			var proxy = new SimpleProxy (Uri);
			if (Credentials != null)
				proxy.Credentials = Credentials;
			return proxy;
		}

		public static IWebProxy CreateSimpleProxy (Uri uri)
		{
			return new SimpleProxy (uri);
		}

		class SimpleProxy : IWebProxy {
			readonly Uri uri;

			public SimpleProxy (Uri uri)
			{
				this.uri = uri;
			}

			public Uri Uri {
				get { return uri; }
			}

			public ICredentials Credentials {
				get; set;
			}

			public Uri GetProxy (Uri destination)
			{
				return uri;
			}

			public bool IsBypassed (Uri host)
			{
				return false;
			}
		}
	}
}
