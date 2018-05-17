//
// ClientCertificateSelector.cs
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
	using TestAttributes;
	using Resources;

	public class ClientCertificateSelector : RenegotiationTestFixture
	{
		public enum SelectorType {
			NoCertificate,
			SetAsConnectionParameter,
			[Renegotiation]
			SetOnRenegotiateCallback,
			[Renegotiation]
			RenegotiateButSetOnFirstCallback,
			[NotWorking]
			NoSelectorCallback
		}

		public SelectorType Type {
			get;
		}

		[AsyncTest]
		public ClientCertificateSelector (SelectorType type)
		{
			Type = type;
		}

		protected override void CreateParameters (TestContext ctx, ConnectionParameters parameters)
		{
			var certificateProvider = DependencyInjector.Get<ICertificateProvider> ();
			var acceptMonkey = certificateProvider.AcceptThisCertificate (ResourceManager.MonkeyCertificate);

			parameters.RequireClientCertificate = true;
			parameters.ServerCertificateValidator = certificateProvider.AcceptAll ();

			switch (Type) {
			case SelectorType.SetOnRenegotiateCallback:
			case SelectorType.RenegotiateButSetOnFirstCallback:
				parameters.AllowRenegotiation = true;
				parameters.ClientApiType = SslStreamApiType.AuthenticationOptions;
				parameters.ServerApiType = SslStreamApiType.AuthenticationOptions;
				break;
			case SelectorType.SetAsConnectionParameter:
				parameters.ClientCertificate = ResourceManager.MonkeyCertificate;
				break;
			}

			if (Type != SelectorType.NoSelectorCallback)
				parameters.ClientCertificateSelector = new CertificateSelector (ctx, CertificateSelectionCallback);

			base.CreateParameters (ctx, parameters);
		}

		int selectorCalled;

		X509Certificate CertificateSelectionCallback (
			TestContext ctx, string targetHost, X509CertificateCollection localCertificates,
			X509Certificate remoteCertificate, string[] acceptableIssuers)
		{
			Interlocked.Increment (ref selectorCalled);
			LogDebug (ctx, 4, $"{nameof (CertificateSelectionCallback)}: {targetHost} {Client.SslStream.IsAuthenticated}");

			switch (Type) {
			case SelectorType.NoCertificate:
				ctx.Assert (selectorCalled, Is.EqualTo (1), "we're only called once.");
				return null;

			case SelectorType.SetAsConnectionParameter:
				ctx.Assert (localCertificates, Is.Not.Null);
				ctx.Assert (localCertificates.Count, Is.EqualTo (1));
				ctx.Assert (localCertificates[0], Is.EqualTo (ResourceManager.MonkeyCertificate));
				return localCertificates[0];

			case SelectorType.RenegotiateButSetOnFirstCallback:
				ctx.Assert (Client.SslStream.IsAuthenticated, Is.False, "Not authenticated");
				return ResourceManager.MonkeyCertificate;

			case SelectorType.SetOnRenegotiateCallback:
				if (!Client.SslStream.IsAuthenticated) {
					ctx.Assert (remoteCertificate, Is.Null, "Remote certificate must be null while we're not authenticated.");
					return null;
				}

				ctx.Assert (remoteCertificate, Is.Not.Null, "Remote certificate must be non-null when called during renegotiation.");
				return ResourceManager.MonkeyCertificate;

			default:
				throw ctx.AssertFail (Type);
			}
		}

		protected override async Task HandleServer (TestContext ctx, CancellationToken cancellationToken)
		{
			var me = nameof (HandleServer);

			ctx.Assert (Server.SslStream.IsAuthenticated, nameof (Server.SslStream.IsAuthenticated));

			if (Type != SelectorType.NoSelectorCallback)
				ctx.Assert (selectorCalled, Is.EqualTo (1), $"{nameof (CertificateSelectionCallback)} called once.");

			switch (Type) {
			case SelectorType.NoCertificate:
				AssertNotMutuallyAuthenticated ();
				break;

			case SelectorType.SetAsConnectionParameter:
				AssertMutuallyAuthenticated ();
				break;

			case SelectorType.RenegotiateButSetOnFirstCallback:
				AssertMutuallyAuthenticated ();

				await Renegotiate ().ConfigureAwait (false);

				ctx.Assert (selectorCalled, Is.EqualTo (1), $"{nameof (CertificateSelectionCallback)} not called again.");
				AssertMutuallyAuthenticated ();
				break;

			case SelectorType.SetOnRenegotiateCallback:
				AssertNotMutuallyAuthenticated ();

				await Renegotiate ().ConfigureAwait (false);

				ctx.Assert (selectorCalled, Is.EqualTo (2), $"{nameof (CertificateSelectionCallback)} called again.");
				AssertMutuallyAuthenticated ();
				break;

			case SelectorType.NoSelectorCallback:
				AssertMutuallyAuthenticated ();
				break;

			default:
				throw ctx.AssertFail (Type);
			}

			await base.HandleServer (ctx, cancellationToken).ConfigureAwait (false);

			LogDebug (ctx, 4, $"{me} done");

			async Task Renegotiate ()
			{
				LogDebug (ctx, 4, $"{me} before RenegotiateAsync().");

				var setup = DependencyInjector.Get<IConnectionFrameworkSetup> ();
				if (!setup.CanRenegotiate (Server.SslStream))
					throw ctx.AssertFail ("Renegotiation not supported.");

				await setup.RenegotiateAsync (Server.SslStream, cancellationToken).ConfigureAwait (false);

				LogDebug (ctx, 4, $"{me} after RenegotiateAsync().");
			}

			void AssertNotMutuallyAuthenticated ()
			{
				ctx.Assert (Server.SslStream.IsMutuallyAuthenticated, Is.False, nameof (Server.SslStream.IsMutuallyAuthenticated));
				ctx.Assert (Server.SslStream.RemoteCertificate, Is.Null, nameof (MonoServer.SslStream.RemoteCertificate));
			}

			void AssertMutuallyAuthenticated ()
			{
				ctx.Assert (Server.SslStream.IsMutuallyAuthenticated, Is.True, nameof (Server.SslStream.IsMutuallyAuthenticated));
				ctx.Assert (Server.SslStream.RemoteCertificate, Is.EqualTo (ResourceManager.MonkeyCertificate), nameof (MonoServer.SslStream.RemoteCertificate));
			}
		}
	}
}
