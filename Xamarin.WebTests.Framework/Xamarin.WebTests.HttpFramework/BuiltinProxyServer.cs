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
using System.Net;
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

		public BuiltinProxyServer (HttpServer target, IPortableEndPoint listenAddress, HttpServerFlags flags)
			: base (listenAddress, flags | HttpServerFlags.Proxy, null, null)
		{
			Target = target;

			Uri = new Uri (string.Format ("http://{0}:{1}/", ListenAddress.Address, ListenAddress.Port));
		}

		public override Uri Uri {
			get;
		}

		public override Uri TargetUri => Target.Uri;

		public ICredentials Credentials {
			get; set;
		}

		public AuthenticationType AuthenticationType {
			get { return authType; }
			set { authType = value; }
		}

		AuthenticationType authType = AuthenticationType.None;

		public override void RegisterHandler (TestContext ctx, string path, Handler handler)
		{
			Target.RegisterHandler (ctx, path, handler);
		}

		protected internal override Handler GetHandler (TestContext ctx, string path)
		{
			return Target.GetHandler (ctx, path);
		}

		ProxyListener currentListener;

		public override async Task Start (TestContext ctx, CancellationToken cancellationToken)
		{
			var listener = new ProxyListener (ctx, this);
			if (Interlocked.CompareExchange (ref currentListener, listener, null) != null)
				throw new InternalErrorException ();
			await Target.Start (ctx, cancellationToken).ConfigureAwait (false);
			await listener.Start ();
		}

		public override async Task Stop (TestContext ctx, CancellationToken cancellationToken)
		{
			var listener = Interlocked.Exchange (ref currentListener, null);
			if (listener == null || listener.TestContext != ctx)
				throw new InternalErrorException ();
			try {
				await listener.Stop ().ConfigureAwait (false);
				await Target.Stop (ctx, cancellationToken);
			} catch {
				if ((Flags & HttpServerFlags.ExpectException) == 0)
					throw;
			}
		}

		public override void CloseAll ()
		{
			currentListener.CloseAll ();
			Target.CloseAll ();
		}

		public override Task StartParallel (TestContext ctx, CancellationToken cancellationToken)
		{
			throw new NotSupportedException ();
		}

		public override Task<T> RunWithContext<T> (TestContext ctx, Func<CancellationToken, Task<T>> func, CancellationToken cancellationToken)
		{
			return currentListener.RunWithContext (ctx, func, cancellationToken);
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

		protected override string MyToString ()
		{
			return string.Format ("SSL={0}, AuthenticationType={1}", Target.UseSSL, AuthenticationType);
		}
	}
}
