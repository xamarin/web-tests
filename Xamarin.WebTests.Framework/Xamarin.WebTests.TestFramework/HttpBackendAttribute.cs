//
// HttpBackendAttribute.cs
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
using Xamarin.AsyncTests;

namespace Xamarin.WebTests.TestFramework {
	using ConnectionFramework;
	using HttpFramework;
	using Resources;
	using Server;

	public sealed class HttpBackendAttribute : TestHostAttribute, ITestHost<HttpBackend>
	{
		HttpFlags httpFlags;

		public HttpBackendAttribute (HttpFlags httpFlags = HttpFlags.None)
			: base (typeof (HttpBackendAttribute), TestFlags.Hidden)
		{
			this.httpFlags = httpFlags;
		}

		HttpFlags GetHttpFlags (TestContext ctx)
		{
			HttpFlags flags = httpFlags;

			bool reuseConnection;
			if (ctx.TryGetParameter<bool> (out reuseConnection, "ReuseConnection") && reuseConnection)
				flags |= HttpFlags.ReuseConnection;

			return flags;
		}

		bool GetParameters (TestContext ctx, out ConnectionParameters parameters)
		{
			bool useSSL;
			if (((httpFlags & HttpFlags.SSL) == 0) && (!ctx.TryGetParameter<bool> (out useSSL, "UseSSL") || !useSSL)) {
				parameters = null;
				return false;
			}

			var certificate = ResourceManager.SelfSignedServerCertificate;
			parameters = new ConnectionParameters ("http", certificate);
			return true;
		}

		static ISslStreamProvider GetSslStreamProvider (TestContext ctx)
		{
			var factory = DependencyInjector.Get<ConnectionProviderFactory> ();
			ConnectionProviderType providerType;
			if (!ctx.TryGetParameter (out providerType))
				return factory.DefaultSslStreamProvider;

			var provider = factory.GetProvider (providerType);
			return provider.SupportsSslStreams ? provider.SslStreamProvider : null;
		}

		public HttpBackend CreateInstance (TestContext ctx)
		{
			var endpoint = ConnectionTestHelper.GetEndPoint (ctx);

			ConnectionParameters parameters;
			ISslStreamProvider sslStreamProvider = null;

			var flags = GetHttpFlags (ctx);
			if (GetParameters (ctx, out parameters))
				sslStreamProvider = GetSslStreamProvider (ctx);

			HttpBackend backend;
			if ((flags & HttpFlags.HttpListener) != 0)
				backend = new HttpListenerBackend (endpoint, endpoint, flags, parameters, sslStreamProvider);
			else
				backend = new BuiltinHttpBackend (endpoint, endpoint, flags, parameters, sslStreamProvider);
			return backend;
		}
	}
}
