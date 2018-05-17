//
// AskForClientCertificate.cs
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
using Xamarin.AsyncTests.Constraints;

namespace Xamarin.WebTests.RenegotiationTests
{
	using ConnectionFramework;
	using Resources;

	public class AskForClientCertificate : RenegotiationTestFixture
	{
		protected override void CreateParameters (TestContext ctx, ConnectionParameters parameters)
		{
			var certificateProvider = DependencyInjector.Get<ICertificateProvider> ();
			var acceptMonkey = certificateProvider.AcceptThisCertificate (ResourceManager.MonkeyCertificate);

			parameters.AllowRenegotiation = true;
			parameters.RequireClientCertificate = true;
			parameters.ServerCertificateValidator = acceptMonkey;
			parameters.ClientCertificate = ResourceManager.MonkeyCertificate;

			base.CreateParameters (ctx, parameters);
		}

		protected override async Task HandleServer (TestContext ctx, CancellationToken cancellationToken)
		{
			ctx.Assert (MonoServer.SslStream.IsAuthenticated, nameof (MonoServer.SslStream.IsAuthenticated));
			ctx.Assert (MonoServer.SslStream.IsMutuallyAuthenticated, nameof (MonoServer.SslStream.IsMutuallyAuthenticated));

			var remoteCertifcate = MonoServer.SslStream.RemoteCertificate;
			ctx.Assert (remoteCertifcate, Is.Not.Null, nameof (MonoServer.SslStream.RemoteCertificate));

			await base.HandleServer (ctx, cancellationToken).ConfigureAwait (false);
		}
	}
}
