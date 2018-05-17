//
// AcceptableIssuers.cs
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
using System.Security.Cryptography.X509Certificates;
using Xamarin.AsyncTests;
using Xamarin.AsyncTests.Constraints;

namespace Xamarin.WebTests.RenegotiationTests
{
	using ConnectionFramework;
	using Resources;

	public class AcceptableIssuers : RenegotiationTestFixture
	{
		protected override void CreateParameters (TestContext ctx, ConnectionParameters parameters)
		{
			var certificateProvider = DependencyInjector.Get<ICertificateProvider> ();
			var acceptMonkey = certificateProvider.AcceptThisCertificate (ResourceManager.MonkeyCertificate);

			parameters.AllowRenegotiation = true;
			parameters.RequireClientCertificate = true;
			parameters.ServerCertificateValidator = certificateProvider.AcceptAll ();
			parameters.ClientCertificateIssuers = new[] { ResourceManager.LocalCACertificate.Subject };

			parameters.ClientCertificateSelector = new CertificateSelector ((s, t, lc, rc, ai) => CertificateSelectionCallback (ctx, t, lc, rc, ai));

			base.CreateParameters (ctx, parameters);
		}

		int selectorCalled;

		X509Certificate CertificateSelectionCallback (
			TestContext ctx, string targetHost, X509CertificateCollection localCertificates,
			X509Certificate remoteCertificate, string[] acceptableIssuers)
		{
			Interlocked.Increment (ref selectorCalled);
			LogDebug (ctx, 4, $"{nameof (CertificateSelectionCallback)}: {targetHost} {Client.SslStream.IsAuthenticated} {acceptableIssuers}");

			ctx.Assert (acceptableIssuers, Is.Not.Null, nameof (acceptableIssuers));
			ctx.Assert (acceptableIssuers.Length, Is.EqualTo (1), nameof (acceptableIssuers.Length));
			ctx.Assert (acceptableIssuers[0], Is.EqualTo (ResourceManager.LocalCACertificate.Subject));

			if (!Client.SslStream.IsAuthenticated) {
				ctx.Assert (remoteCertificate, Is.Null, "Remote certificate must be null while we're not authenticated.");
				return null;
			}

			ctx.Assert (remoteCertificate, Is.Not.Null, "Remote certificate must be non-null when called during renegotiation.");
			return ResourceManager.MonkeyCertificate;
		}

		protected override async Task HandleServer (TestContext ctx, CancellationToken cancellationToken)
		{
			var me = nameof (HandleServer);

			ctx.Assert (selectorCalled, Is.EqualTo (1), $"{nameof (CertificateSelectionCallback)} called once.");
			ctx.Assert (Server.SslStream.IsAuthenticated, nameof (Server.SslStream.IsAuthenticated));
			ctx.Assert (Server.SslStream.IsMutuallyAuthenticated, Is.False, nameof (Server.SslStream.IsMutuallyAuthenticated));
			ctx.Assert (Server.SslStream.RemoteCertificate, Is.Null, nameof (MonoServer.SslStream.RemoteCertificate));

			LogDebug (ctx, 4, $"{me} before RenegotiateAsync().");

			await MonoServer.RenegotiateAsync (cancellationToken);

			LogDebug (ctx, 4, $"{me} after RenegotiateAsync().");

			ctx.Assert (selectorCalled, Is.EqualTo (2), $"{nameof (CertificateSelectionCallback)} called once.");
			ctx.Assert (Server.SslStream.IsAuthenticated, nameof (MonoServer.SslStream.IsAuthenticated));
			ctx.Assert (Server.SslStream.IsMutuallyAuthenticated, nameof (MonoServer.SslStream.IsMutuallyAuthenticated));

			var remoteCertificate = MonoServer.SslStream.RemoteCertificate;
			ctx.Assert (remoteCertificate, Is.Not.Null, nameof (Server.SslStream.RemoteCertificate));
			ctx.Assert (remoteCertificate, Is.EqualTo (ResourceManager.MonkeyCertificate));

			await base.HandleServer (ctx, cancellationToken).ConfigureAwait (false);

			LogDebug (ctx, 4, $"{me} done");
		}
	}
}
