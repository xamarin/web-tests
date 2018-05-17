//
// RenegotiationTestRunner.cs
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
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Security.Authentication;
using Mono.Security.Interface;
using Xamarin.AsyncTests;
using Xamarin.AsyncTests.Constraints;

namespace Xamarin.WebTests.MonoTestFramework
{
	using MonoConnectionFramework;
	using ConnectionFramework;
	using TestFramework;
	using TestRunners;
	using Resources;

	public abstract class RenegotiationTestRunner : StreamInstrumentationTestRunner
	{
		protected MonoServer MonoServer => (MonoServer)Server;

		protected MonoClient MonoClient => (MonoClient)Client;

		protected sealed override ConnectionParameters CreateParameters (TestContext ctx)
		{
			var certificateProvider = DependencyInjector.Get<ICertificateProvider> ();
			var acceptAll = certificateProvider.AcceptAll ();

			var parameters = new ConnectionParameters (ResourceManager.SelfSignedServerCertificate) {
				ClientCertificateValidator = acceptAll,
				ExpectClientException = HandshakeFails, ExpectServerException = HandshakeFails,
				ClientApiType = SslStreamApiType.AuthenticationOptions,
				ServerApiType = SslStreamApiType.AuthenticationOptions
			};

			CreateParameters (ctx, parameters);

			return parameters;
		}

		protected virtual void CreateParameters (TestContext ctx, ConnectionParameters parameters)
		{
		}

		protected sealed override async Task MainLoop (TestContext ctx, CancellationToken cancellationToken)
		{
			var me = $"{ME}.{nameof (MainLoop)}";
			ctx.LogDebug (LogCategory, 4, $"{me}");

			ctx.LogDebug (LogCategory, 4, $"{me} - client={MonoClient.Provider} server={MonoServer.Provider} - {MonoServer.CanRenegotiate}");
			ctx.Assert (MonoServer.CanRenegotiate, "MonoServer.CanRenegotiate");

			try {
				await base.MainLoop (ctx, cancellationToken).ConfigureAwait (false);
				ctx.LogDebug (LogCategory, 4, $"{me} - complete.");
			} catch (Exception ex) {
				ctx.LogDebug (LogCategory, 4, $"{me} - failed: {ex.Message}");
			}
		}
	}
}
