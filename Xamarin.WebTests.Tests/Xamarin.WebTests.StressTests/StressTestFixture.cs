//
// StressTestFixture.cs
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
using System.Threading;
using System.Threading.Tasks;
using Xamarin.AsyncTests;

namespace Xamarin.WebTests.StressTests
{
	using ConnectionFramework;
	using TestAttributes;
	using TestFramework;
	using HttpFramework;
	using Resources;

	[Stress]
	[AsyncTestFixture (Prefix = "StressTests")]
	public abstract class StressTestFixture : AbstractConnection
	{
		public HttpServer Server {
			get;
			private set;
		}

		public string ME => GetType ().Name;

		protected const string LogCategory = LogCategories.Stress;

		ConnectionParameters GetParameters (TestContext ctx)
		{
			var parameters = new ConnectionParameters (ResourceManager.SelfSignedServerCertificate);
			CreateParameters (ctx, parameters);
			return parameters;
		}

		protected virtual void CreateParameters (TestContext ctx, ConnectionParameters parameters)
		{
		}

		[AsyncTest]
		public static Task Run (
			TestContext ctx, CancellationToken cancellationToken,
			HttpServerProvider provider, StressTestFixture fixture)
		{
			return fixture.Run (ctx, cancellationToken);
		}

		[AsyncTest]
		[Martin (null, UseFixtureName = true)]
		public static Task MartinTest (
			TestContext ctx, CancellationToken cancellationToken,
			HttpServerProvider provider, StressTestFixture fixture)
		{
			return fixture.Run (ctx, cancellationToken);
		}

		protected abstract Task Run (TestContext ctx, CancellationToken cancellationToken);

		protected override async Task Initialize (TestContext ctx, CancellationToken cancellationToken)
		{
			var provider = ctx.GetParameter<HttpServerProvider> ();
			var serverFlags = provider.ServerFlags | HttpServerFlags.ParallelListener;

			var endPoint = ConnectionTestHelper.GetEndPoint ();

			var proto = (serverFlags & HttpServerFlags.NoSSL) != 0 ? "http" : "https";
			var uri = new Uri ($"{proto}://{endPoint}/");

			var parameters = GetParameters (ctx);

			Server = new BuiltinHttpServer (uri, endPoint, serverFlags, parameters, provider.SslStreamProvider);

			await Server.Initialize (ctx, cancellationToken).ConfigureAwait (false);
		}

		protected override async Task Destroy (TestContext ctx, CancellationToken cancellationToken)
		{
			await Server.Destroy (ctx, cancellationToken).ConfigureAwait (false);
		}

		protected override async Task PreRun (TestContext ctx, CancellationToken cancellationToken)
		{
			await Server.PreRun (ctx, cancellationToken).ConfigureAwait (false);
		}

		protected override async Task PostRun (TestContext ctx, CancellationToken cancellationToken)
		{
			await Server.PostRun (ctx, cancellationToken).ConfigureAwait (false);
		}

		protected override void Stop ()
		{
		}
	}
}
