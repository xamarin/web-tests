//
// MartinTest.cs
//
// Author:
//       Martin Baulig <martin.baulig@xamarin.com>
//
// Copyright (c) 2015 Xamarin Inc. (http://www.xamarin.com)
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
using Xamarin.AsyncTests.Framework;

namespace Xamarin.WebTests
{
	using TestFramework;
	using HttpFramework;
	using HttpHandlers;
	using TestRunners;
	using Server;

	[AsyncTestFixture]
	public class MartinTest
	{
		Handler CreateAuthMaybeNone (Handler handler, AuthenticationType authType)
		{
			if (authType == AuthenticationType.None)
				return handler;
			var auth = new AuthenticationHandler (authType, handler);
			return auth;
		}

		void ConfigureWebClient (WebClient client, Handler handler, CancellationToken cancellationToken)
		{
			cancellationToken.Register (() => client.CancelAsync ());
			var authHandler = handler as AuthenticationHandler;
			if (authHandler != null)
				client.Credentials = authHandler.GetCredentials ();
		}

		[Work]
		[AsyncTest]
		public async Task TestMartinFixed (
			TestContext ctx, [HttpServer (ListenerFlags.SSL)] HttpServer server,
			CancellationToken cancellationToken)
		{
			var post = new PostHandler ("Martin Test", HttpContent.HelloWorld);

			var handler = CreateAuthMaybeNone (post, AuthenticationType.NTLM);

			var uri = handler.RegisterRequest (server);

			using (var client = new WebClient ()) {
				ConfigureWebClient (client, handler, cancellationToken);

				var stream = await client.OpenWriteTaskAsync (uri, "PUT");

				using (var writer = new StreamWriter (stream)) {
					await post.Content.WriteToAsync (writer);
				}
			}
		}

		[Work]
		[AsyncTest]
		[ConnectionTestFlags (ConnectionTestFlags.RequireSslStream)]
		[ConnectionTestCategory (ConnectionTestCategory.MartinTest)]
		public async Task TestMartinSslStream (TestContext ctx, CancellationToken cancellationToken,
			[ConnectionTestProvider ("AppleTls:AppleTls")] ConnectionTestProvider provider,
			SslStreamTestParameters parameters, SslStreamTestRunner runner)
		{
			await runner.Run (ctx, cancellationToken);
		}

		[Work]
		[AsyncTest]
		[ConnectionTestFlags (ConnectionTestFlags.RequireSslStream)]
		[ConnectionTestCategory (ConnectionTestCategory.SslStreamCertificateValidators)]
		public async Task TestMartinSslStreamWorking (TestContext ctx, CancellationToken cancellationToken,
			[ConnectionTestProvider ("DotNet:DotNet")] ConnectionTestProvider provider,
			SslStreamTestParameters parameters, SslStreamTestRunner runner)
		{
			await runner.Run (ctx, cancellationToken);
		}

		[Work]
		[AsyncTest]
		[ConnectionTestFlags (ConnectionTestFlags.RequireSslStream)]
		[ConnectionTestCategory (ConnectionTestCategory.MartinTest)]
		public async Task TestMartinSslStream2 (TestContext ctx, CancellationToken cancellationToken,
			[ConnectionTestProvider ("DotNet:DotNet")] ConnectionTestProvider provider,
			SslStreamTestParameters parameters, SslStreamTestRunner runner)
		{
			await runner.Run (ctx, cancellationToken);
		}

		[Work]
		[AsyncTest]
		[ConnectionTestCategory (ConnectionTestCategory.MartinTest)]
		public async Task TestMartinHttpWorking (TestContext ctx, CancellationToken cancellationToken,
			[ConnectionTestProvider ("DotNet:DotNet")] ConnectionTestProvider provider,
			HttpsTestParameters parameters, HttpsTestRunner runner)
		{
			await runner.Run (ctx, cancellationToken);
		}

		[Work]
		[AsyncTest]
		[ConnectionTestCategory (ConnectionTestCategory.MartinTest)]
		public async Task TestMartinHttp (TestContext ctx, CancellationToken cancellationToken,
			[ConnectionTestProvider ("DotNet:DotNet")] ConnectionTestProvider provider,
			HttpsTestParameters parameters, HttpsTestRunner runner)
		{
			await runner.Run (ctx, cancellationToken);
		}
	}
}

