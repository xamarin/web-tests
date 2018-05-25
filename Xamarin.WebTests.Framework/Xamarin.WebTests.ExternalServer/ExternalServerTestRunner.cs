//
// ExternalServerTestRunner.cs
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
using System.Xml.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using System.Collections;
using System.Collections.Generic;
using Xamarin.AsyncTests;
using Xamarin.AsyncTests.Framework;
using Xamarin.AsyncTests.Constraints;

namespace Xamarin.WebTests.ExternalServer
{
	using ConnectionFramework;
	using TestAttributes;
	using HttpFramework;
	using HttpHandlers;
	using TestFramework;
	using Resources;

	[ForkedSupport]
	[Fork (ForkType.FromContext)]
	public abstract class ExternalServerTestRunner : AbstractConnection, IForkedTestInstance
	{
		public ConnectionTestProvider Provider {
			get;
		}

		public string ME {
			get;
		}

		[FixtureParameter]
		public abstract ForkType ForkType {
			get;
		}

		public bool IsReverseFork => ForkType == ForkType.ReverseFork || ForkType == ForkType.ReverseDomain;

		public bool IsForked {
			get;
			private set;
		}

		const string LogCategory = LogCategories.ExternalServer;

		static ConnectionParameters GetParameters (string identifier)
		{
			var certificateProvider = DependencyInjector.Get<ICertificateProvider> ();
			var acceptAll = certificateProvider.AcceptAll ();

			return new ConnectionParameters (ResourceManager.SelfSignedServerCertificate) {
				ClientCertificateValidator = acceptAll
			};
		}

		protected ExternalServerTestRunner ()
		{
			ME = $"{DebugHelper.FormatType (this)}";
		}

		protected abstract Task Run (TestContext ctx, CancellationToken cancellationToken);

		protected abstract void Initialize (TestContext ctx);

		protected virtual void RemoteInitialize (TestContext ctx)
		{
		}

		protected ExternalServerRegistration RegisterServer (string name, int parallel = 0, HttpServerFlags flags = HttpServerFlags.None)
		{
			var registration = new ExternalServerRegistration (name, parallel, flags);
			ServerRegistry.Add (name, registration);
			return registration;
		}

		int initialized;

		protected Registry<ExternalServerRegistration> ServerRegistry {
			get;
			private set;
		}

		void RunInitialize (TestContext ctx)
		{
			if (Interlocked.CompareExchange (ref initialized, 1, 0) != 0)
				return;

			ctx.LogDebug (LogCategory, 2, $"RUN INITIALIZE: {ctx.FriendlyName} {IsForked}");

			if (IsReverseFork) {
				if (!IsForked)
					return;
				// Discard one port.
				ConnectionTestHelper.GetEndPoint ();
			} else {
				ctx.Assert (IsForked, Is.False, "We are never called in forked context.");
			}

			ServerRegistry = new Registry<ExternalServerRegistration> ();

			Initialize (ctx);

			var provider = ctx.GetParameter<HttpServerProvider> ();

			var providerFlags = provider.ServerFlags | HttpServerFlags.ParallelListener | HttpServerFlags.ReuseConnection;

			foreach (var server in ServerRegistry) {
				server.EndPoint = ConnectionTestHelper.GetEndPoint ();
				server.EffectiveFlags = server.Flags | providerFlags;
				var proto = (server.EffectiveFlags & HttpServerFlags.NoSSL) != 0 ? "http" : "https";
				server.Uri = new Uri ($"{proto}://{server.EndPoint}/");

				var parameters = GetParameters (ME);

				server.Server = new BuiltinHttpServer (
					server.Uri, server.EndPoint, providerFlags, parameters,
					provider.SslStreamProvider);

				server.Server.Initialize (ctx);

				if (server.ParallelConnections > 0)
					server.Server.Listener.ParallelConnections = server.ParallelConnections;

				foreach (var handler in server.HandlerRegistry) {
					handler.Operation = server.Server.Listener.RegisterOperation (
						ctx, HttpOperationFlags.PersistentHandler, handler.Handler, null);
					handler.Uri = handler.Operation.Uri;
				}

				server.Initialized = true;

				ctx.LogDebug (LogCategory, 2, $"RUN INITIALIZE: {server.Uri}");
			}
		}

		protected override async Task Initialize (TestContext ctx, CancellationToken cancellationToken)
		{
			ctx.LogDebug (LogCategory, 2, $"INITIALIZE: {ctx.FriendlyName} {IsForked}");

			if (!IsReverseFork && IsForked)
				RemoteInitialize (ctx);

			if (IsReverseFork != IsForked)
				return;

			RunInitialize (ctx);

			var tasks = ServerRegistry.Select (s => s.Server.Initialize (ctx, cancellationToken));
			await Task.WhenAll (tasks).ConfigureAwait (false);
		}

		protected override async Task Destroy (TestContext ctx, CancellationToken cancellationToken)
		{
			if (IsReverseFork != IsForked)
				return;

			var tasks = ServerRegistry.Select (s => s.Server.Destroy (ctx, cancellationToken));
			await Task.WhenAll (tasks).ConfigureAwait (false);
		}

		protected override async Task PreRun (TestContext ctx, CancellationToken cancellationToken)
		{
			if (IsReverseFork != IsForked)
				return;

			var tasks = ServerRegistry.Select (s => s.Server.PreRun (ctx, cancellationToken));
			await Task.WhenAll (tasks).ConfigureAwait (false);
		}

		protected override async Task PostRun (TestContext ctx, CancellationToken cancellationToken)
		{
			if (IsReverseFork != IsForked)
				return;

			var tasks = ServerRegistry.Select (s => s.Server.PostRun (ctx, cancellationToken));
			await Task.WhenAll (tasks).ConfigureAwait (false);
		}

		protected override void Stop ()
		{
		}

		public void Serialize (TestContext ctx, XElement element)
		{
			if (IsReverseFork != IsForked)
				return;
			RunInitialize (ctx);

			foreach (var server in ServerRegistry) {
				element.Add (server.Serialize ());
			}
		}

		public void Deserialize (TestContext ctx, XElement element)
		{
			IsForked = true;

			ServerRegistry = new Registry<ExternalServerRegistration> ();

			foreach (var serverElement in element.Elements ("Server")) {
				var registration = new ExternalServerRegistration (serverElement);
				ServerRegistry.Add (registration.Name, registration);
			}
		}

		protected async Task TraditionalSend (TestContext ctx, ExternalServerHandler handler, CancellationToken cancellationToken)
		{
			cancellationToken.ThrowIfCancellationRequested ();

			var request = new TraditionalRequest (handler.Uri);
			using (var response = await request.Send (ctx, cancellationToken).ConfigureAwait (false)) {
				ctx.Assert (response.Status, Is.EqualTo (HttpStatusCode.OK));
			}
		}
	}
}
