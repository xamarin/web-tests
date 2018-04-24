//
// ParallelServerTestRunner.cs
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
using System.IO;
using System.Linq;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using System.Collections;
using System.Collections.Generic;
using Xamarin.AsyncTests;
using Xamarin.AsyncTests.Constraints;
using Xamarin.AsyncTests.Portable;

namespace Xamarin.WebTests.TestRunners
{
	using ConnectionFramework;
	using TestAttributes;
	using HttpFramework;
	using HttpHandlers;
	using HttpOperations;
	using TestFramework;
	using Resources;
	using Server;

	public abstract class ParallelServerTestRunner : AbstractConnection, ListenerHandler
	{
		public ConnectionTestProvider Provider {
			get;
		}

		protected Uri Uri {
			get;
			private set;
		}

		protected HttpServerFlags ServerFlags {
			get;
		}

		public BuiltinHttpServer Server {
			get;
			private set;
		}

		public string ME {
			get;
		}

		string ListenerHandler.Value => ME;

		RequestFlags ListenerHandler.RequestFlags => RequestFlags.CloseConnection;

		const string LogCategory = "ParallelServer";

		static ConnectionParameters GetParameters (string identifier)
		{
			var certificateProvider = DependencyInjector.Get<ICertificateProvider> ();
			var acceptAll = certificateProvider.AcceptAll ();

			return new ConnectionParameters (identifier, ResourceManager.SelfSignedServerCertificate) {
				ClientCertificateValidator = acceptAll
			};
		}

		internal ListenerOperation Operation {
			get;
			private set;
		}

		public async Task Run (TestContext ctx, CancellationToken cancellationToken)
		{
			var me = $"{ME}.{nameof (Run)}";
			ctx.LogDebug (2, $"{me}");

			ctx.LogDebug (LogCategory, 2, $"RUN: {Uri} - {Operation.Uri}");

			for (int i = 0; i < 10; i++) {
				var request = new TraditionalRequest (Operation.Uri);
				var response = await request.Send (ctx, cancellationToken).ConfigureAwait (false);

				ctx.LogDebug (LogCategory, 2, $"GOT RESPONSE #{i+1}: {response.Status}");
			}
		}

		protected override async Task Initialize (TestContext ctx, CancellationToken cancellationToken)
		{
			var provider = ctx.GetParameter<HttpServerProvider> ();

			var serverFlags = provider.ServerFlags | HttpServerFlags.ParallelListener | HttpServerFlags.ReuseConnection;

			var endPoint = ConnectionTestHelper.GetEndPoint ();

			var proto = (serverFlags & HttpServerFlags.NoSSL) != 0 ? "http" : "https";
			Uri = new Uri ($"{proto}://{endPoint.Address}:{endPoint.Port}/");

			var parameters = GetParameters (ME);

			Server = new BuiltinHttpServer (
				Uri, endPoint, serverFlags, parameters,
				provider.SslStreamProvider);

			Server.Initialize (ctx);

			Server.Listener.ParallelConnections = 2;
			Operation = Server.Listener.RegisterOperation (ctx, HttpOperationFlags.PersistentHandler, this, null);

			ctx.LogDebug (LogCategory, 2, $"INITIALIZE: {Uri}");

			await Server.Initialize (ctx, cancellationToken).ConfigureAwait (false);
		}

		protected override Task Destroy (TestContext ctx, CancellationToken cancellationToken)
		{
			return Server.Destroy (ctx, cancellationToken);
		}

		protected override Task PreRun (TestContext ctx, CancellationToken cancellationToken)
		{
			return Server.PreRun (ctx, cancellationToken);
		}

		protected override Task PostRun (TestContext ctx, CancellationToken cancellationToken)
		{
			return Server.PostRun (ctx, cancellationToken);
		}

		protected override void Stop ()
		{
		}

		Task<HttpResponse> ListenerHandler.HandleRequest (
			TestContext ctx, HttpOperation operation, HttpConnection connection, HttpRequest request,
			RequestFlags effectiveFlags, CancellationToken cancellationToken)
		{
			return HandleRequest (ctx, connection, request, cancellationToken);
		}

		protected abstract Task<HttpResponse> HandleRequest (
			TestContext ctx, HttpConnection connection, HttpRequest request, CancellationToken cancellationToken);
	}
}
