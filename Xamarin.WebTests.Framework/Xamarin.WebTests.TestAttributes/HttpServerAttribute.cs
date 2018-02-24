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
using System.Linq;
using Xamarin.AsyncTests;
using Xamarin.AsyncTests.Constraints;
using Xamarin.AsyncTests.Framework;
using Xamarin.AsyncTests.Portable;

namespace Xamarin.WebTests.TestAttributes {
	using ConnectionFramework;
	using TestFramework;
	using HttpFramework;
	using Resources;
	using Server;

	public sealed class HttpServerAttribute : TestHostAttribute, ITestHost<HttpServer> {
		internal HttpServerFlags? ExplicitServerFlags;

		public HttpServerAttribute (HttpServerFlags serverFlags)
			: base (null, TestFlags.Hidden)
		{
			ExplicitServerFlags = serverFlags;
		}

		public HttpServerAttribute ()
			: base (null, TestFlags.Hidden)
		{
		}

		HttpServerFlags? GetFixedFlags (TestContext ctx)
		{
			HttpServerFlags fixedFlags;
			if (ctx.TryGetParameter (out fixedFlags))
				return fixedFlags;
			return null;
		}

		bool GetReuseConnection (TestContext ctx)
		{
			bool reuseConnection;
			return ctx.TryGetParameter (out reuseConnection, "ReuseConnection") && reuseConnection;
		}

		HttpServerFlags GetServerFlags (TestContext ctx)
		{
			HttpServerFlags flags;
			if (ExplicitServerFlags != null)
				flags = ExplicitServerFlags.Value;
			else if (!ctx.TryGetParameter (out flags)) {
				flags = HttpServerFlags.None;

				bool reuseConnection;
				if (ctx.TryGetParameter (out reuseConnection, "ReuseConnection") && reuseConnection)
					flags |= HttpServerFlags.ReuseConnection;

				bool ssl;
				if (ctx.TryGetParameter (out ssl, "SSL") && ssl)
					flags |= HttpServerFlags.SSL;
			}

			return flags;
		}

		bool GetParameters (TestContext ctx, HttpServerFlags flags, out ConnectionParameters parameters)
		{
			if ((flags & HttpServerFlags.SSL) == 0) {
				parameters = null;
				return false;
			}

			var certificate = ResourceManager.SelfSignedServerCertificate;
			parameters = new ConnectionParameters ("http", certificate);
			return true;
		}

		static void CheckConnectionFlags (ref ConnectionTestFlags flags, HttpServerFlags serverFlags)
		{
			var sslListenerFlags = HttpServerFlags.HttpListener | HttpServerFlags.SSL;
			if ((serverFlags & sslListenerFlags) == sslListenerFlags)
				flags |= ConnectionTestFlags.RequireMonoServer | ConnectionTestFlags.RequireHttpListener;
		}

		const HttpServerFlags HttpListenerSsl = HttpServerFlags.HttpListener | HttpServerFlags.SSL;

		static ISslStreamProvider GetSslStreamProvider (TestContext ctx, HttpServerFlags serverFlags)
		{
			var factory = DependencyInjector.Get<ConnectionProviderFactory> ();

			ConnectionProvider provider;
			ConnectionTestFlags explicitFlags;
			ConnectionProviderType providerType;

			if (ctx.TryGetParameter (out providerType)) {
				provider = factory.GetProvider (providerType);
			} else if (ctx.TryGetParameter (out explicitFlags)) {
				explicitFlags |= ConnectionTestFlags.RequireSslStream;
				var filter = ConnectionProviderFilter.CreateSimpleFilter (explicitFlags);
				provider = filter.GetDefaultServer (ctx, null);
			} else if ((serverFlags & HttpListenerSsl) == HttpListenerSsl) {
				explicitFlags = ConnectionTestFlags.RequireHttpListener;
				var filter = ConnectionProviderFilter.CreateSimpleFilter (explicitFlags);
				provider = filter.GetDefaultServer (ctx, null);
			} else {
				return factory.DefaultSslStreamProvider;
			}

			ctx.Assert (provider, Is.Not.Null, "Failed to resolve ConnectionProvider");
			ctx.Assert (provider.SupportsSslStreams, "Seleced ConnectionProvider `{0}' does not support SSL.", provider);

			return provider.SslStreamProvider;
		}

		public HttpServer CreateInstance (TestContext ctx)
		{
			var endpoint = ConnectionTestHelper.GetEndPoint ();

			ConnectionParameters parameters;
			ISslStreamProvider sslStreamProvider = null;

			var flags = GetServerFlags (ctx);
			if (GetParameters (ctx, flags, out parameters))
				sslStreamProvider = GetSslStreamProvider (ctx, flags);

			return new BuiltinHttpServer (endpoint, endpoint, flags, parameters, sslStreamProvider);
		}
	}
}

