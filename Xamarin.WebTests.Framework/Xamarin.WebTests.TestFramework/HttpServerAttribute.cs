//
// HttpServerAttribute.cs
//
// Author:
//       Martin Baulig <martin.baulig@xamarin.com>
//
// Copyright (c) 2015 Xamarin, Inc.
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
using Xamarin.AsyncTests;
using Xamarin.AsyncTests.Framework;
using Xamarin.AsyncTests.Portable;

namespace Xamarin.WebTests.TestFramework
{
	using ConnectionFramework;
	using HttpFramework;
	using Resources;
	using Server;

	public class HttpServerAttribute : TestHostAttribute, ITestHost<HttpServer>
	{
		ListenerFlags listenerFlags;

		public HttpServerAttribute (ListenerFlags listenerFlags = ListenerFlags.None)
			: base (typeof (HttpServerAttribute))
		{
			this.listenerFlags = listenerFlags;
		}

		protected HttpServerAttribute (Type type, TestFlags flags = TestFlags.None,
		                               ListenerFlags listenerFlags = ListenerFlags.None)
			: base (type, flags)
		{
		}

		protected virtual ListenerFlags GetListenerFlags (TestContext ctx)
		{
			ListenerFlags flags = listenerFlags;

			bool reuseConnection;
			if (ctx.TryGetParameter<bool> (out reuseConnection, "ReuseConnection") && reuseConnection)
				flags |= ListenerFlags.ReuseConnection;

			return flags;
		}

		protected virtual bool GetParameters (TestContext ctx, out ConnectionParameters parameters)
		{
			bool useSSL;
			if (((listenerFlags & ListenerFlags.SSL) == 0) && (!ctx.TryGetParameter<bool> (out useSSL, "UseSSL") || !useSSL)) {
				parameters = null;
				return false;
			}

			var certificate = ResourceManager.SelfSignedServerCertificate;
			parameters = new ConnectionParameters ("http", certificate);
			return true;
		}

		protected static ISslStreamProvider GetSslStreamProvider (TestContext ctx)
		{
			var factory = DependencyInjector.Get<ConnectionProviderFactory> ();
			ConnectionProviderType providerType;
			if (!ctx.TryGetParameter (out providerType))
				return factory.DefaultSslStreamProvider;

			var provider = factory.GetProvider (providerType);
			return provider.SupportsSslStreams ? provider.SslStreamProvider : null;
		}

		public virtual HttpServer CreateInstance (TestContext ctx)
		{
			var endpoint = ConnectionTestHelper.GetEndPoint (ctx);

			ConnectionParameters parameters;
			ISslStreamProvider sslStreamProvider = null;

			var flags = GetListenerFlags (ctx);
			if (GetParameters (ctx, out parameters))
				sslStreamProvider = GetSslStreamProvider (ctx);

			return new HttpServer (endpoint, endpoint, flags, parameters, sslStreamProvider);
		}
	}
}

