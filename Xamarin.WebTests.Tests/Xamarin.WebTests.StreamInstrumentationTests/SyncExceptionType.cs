//
// SyncExceptionType.cs
//
// Author:
//       Martin Baulig <mabaul@microsoft.com>
//
// Copyright (c) 2019 Xamarin Inc. (http://www.xamarin.com)
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
using System.Threading;
using System.Threading.Tasks;
using Xamarin.AsyncTests;
using Xamarin.AsyncTests.Constraints;

namespace Xamarin.WebTests.StreamInstrumentationTests
{
	using ConnectionFramework;
	using Resources;

	public class SyncExceptionType : StreamInstrumentationTestFixture
	{
		protected override bool HandshakeFails => true;

		protected override bool UseCleanShutdown => false;

		protected override ConnectionParameters CreateParameters (TestContext ctx)
		{
			var certificateProvider = DependencyInjector.Get<ICertificateProvider> ();
			var acceptAll = certificateProvider.AcceptAll ();

			return new ConnectionParameters (ResourceManager.SelfSignedServerCertificate) {
				ClientCertificateValidator = acceptAll,
				ClientApiType = SslStreamApiType.Sync, ServerApiType = SslStreamApiType.Sync,
				ExpectClientException = true, ExpectServerException = true,
				CleanShutdown = false
			};
		}

		protected override void ConfigureClientStream (TestContext ctx, StreamInstrumentation instrumentation)
		{
			instrumentation.IgnoreErrors = true;
		}

		protected override void ConfigureServerStream (TestContext ctx, StreamInstrumentation instrumentation)
		{
			instrumentation.IgnoreErrors = true;
		}

		protected override async Task<bool> ClientHandshake (TestContext ctx, Func<Task> handshake, StreamInstrumentation instrumentation)
		{
			instrumentation.OnNextRead ((buffer, offset, count, func, cancellationToken) => Task.FromResult (0));

			await ctx.AssertException<IOException> (handshake, "client handshake").ConfigureAwait (false);

			Server.Dispose ();

			return true;
		}

		protected override async Task<bool> ServerHandshake (TestContext ctx, Func<Task> handshake, StreamInstrumentation instrumentation)
		{
			await ctx.AssertException<ObjectDisposedException> (handshake, "server handshake").ConfigureAwait (false);

			Client.Close ();

			return true;
		}

		protected override Task MainLoop (TestContext ctx, CancellationToken cancellationToken)
		{
			return FinishedTask;
		}
	}
}
