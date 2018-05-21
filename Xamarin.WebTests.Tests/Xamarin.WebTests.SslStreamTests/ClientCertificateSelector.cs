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
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using Xamarin.AsyncTests;
using Xamarin.AsyncTests.Constraints;

namespace Xamarin.WebTests.SslStreamTests
{
	using ConnectionFramework;
	using TestAttributes;
	using TestFramework;
	using Resources;

	[Work]
	[ConnectionTestFlags (ConnectionTestFlags.AllowWildcardMatches | ConnectionTestFlags.RequireClientCertificates)]
	public class ClientCertificateSelector : SslStreamTestFixture
	{
		public enum SelectorType
		{
			NoCertificate,
			SetAsConnectionParameter,
			[ProviderFilter (NeedRenegotiation = true)]
			SetOnRenegotiateCallback,
			[ProviderFilter (NeedRenegotiation = true)]
			RenegotiateButSetOnFirstCallback,
			NoSelectorCallback,
			MultipleCertificates,
			[ProviderFilter (AcceptableIssuers = true)]
			AcceptableIssuers,
			[ProviderFilter (AcceptableIssuers = true)]
			AcceptableIssuersNoCallback,
			CallbackReturnsNullButHaveCertificate
		}

		class ProviderFilter : EnumValueFilter
		{
			public bool NeedRenegotiation {
				get; set;
			}

			public bool AcceptableIssuers {
				get; set;
			}

			public override bool Filter (TestContext ctx, object value)
			{
				var provider = ctx.GetParameter<ConnectionTestProvider> ();
				if (provider == null)
					return true;

				if (AcceptableIssuers) {
					if (!provider.Client.HasFlag (ConnectionProviderFlags.SupportsClientRenegotiation))
						return false;
					// We are intentionally checking for `SupportsClientRenegotiation` here, it is set
					// on both AppleTLS and BTLS, whereas only AppleTLS supports server-side renegotiation.
					if (!provider.Server.HasFlag (ConnectionProviderFlags.SupportsClientRenegotiation))
						return false;
				}

				if (NeedRenegotiation) {
					if (!provider.Client.HasFlag (ConnectionProviderFlags.SupportsClientRenegotiation))
						return false;
					if (!provider.Server.HasFlag (ConnectionProviderFlags.SupportsServerRenegotiation))
						return false;
				}

				return true;
			}
		}

		public SelectorType Type {
			get;
		}

		readonly IConnectionFrameworkSetup setup;

		[AsyncTest]
		public ClientCertificateSelector (SelectorType type)
		{
			Type = type;

			setup = DependencyInjector.Get<IConnectionFrameworkSetup> ();
		}

		protected override ConnectionParameters CreateParameters (TestContext ctx)
		{
			var certificateProvider = DependencyInjector.Get<ICertificateProvider> ();
			var acceptMonkey = certificateProvider.AcceptThisCertificate (ResourceManager.MonkeyCertificate);

			var parameters = new ConnectionParameters (ResourceManager.SelfSignedServerCertificate) {
				ClientCertificateValidator = AcceptSelfSigned, RequireClientCertificate = true,
				ServerCertificateValidator = new CertificateValidator (ctx, CertificateValidationCallback),
				ClientCertificateSelector = new CertificateSelector (ctx, CertificateSelectionCallback)
			};

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
			case SelectorType.NoSelectorCallback:
				parameters.ClientCertificateSelector = null;
				parameters.ClientCertificate = ResourceManager.MonkeyCertificate;
				break;
			case SelectorType.MultipleCertificates:
				parameters.ClientCertificateSelector = null;
				parameters.ClientCertificates = new X509CertificateCollection {
					ResourceManager.PenguinCertificate, ResourceManager.MonkeyCertificate
				};
				break;
			case SelectorType.AcceptableIssuers:
				parameters.ClientCertificateIssuers = new[] { ResourceManager.LocalCACertificate.Subject };
				break;
			case SelectorType.AcceptableIssuersNoCallback:
				parameters.ClientCertificateSelector = null;
				parameters.ClientCertificates = new X509CertificateCollection {
					ResourceManager.PenguinCertificate, ResourceManager.MonkeyCertificate
				};
				parameters.ClientCertificateIssuers = new[] { ResourceManager.LocalCACertificate.Subject };
				break;
			case SelectorType.CallbackReturnsNullButHaveCertificate:
				parameters.ClientCertificate = ResourceManager.MonkeyCertificate;
				break;
			}

			return parameters;
		}

		int selectorCalled;
		int validatorCalled;

		X509Certificate CertificateSelectionCallback (
			TestContext ctx, string targetHost, X509CertificateCollection localCertificates,
			X509Certificate remoteCertificate, string[] acceptableIssuers)
		{
			Interlocked.Increment (ref selectorCalled);
			LogDebug (ctx, 4, $"{nameof (CertificateSelectionCallback)}: {targetHost} {Client.SslStream.IsAuthenticated}");

			switch (Type) {
			case SelectorType.NoCertificate:
			case SelectorType.CallbackReturnsNullButHaveCertificate:
				if (!setup.UsingDotNet)
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

			case SelectorType.AcceptableIssuers:
				ctx.Assert (acceptableIssuers, Is.Not.Null);
				ctx.Assert (acceptableIssuers.Length, Is.EqualTo (1));
				ctx.Assert (acceptableIssuers[0], Is.EqualTo (ResourceManager.LocalCACertificate.Subject));
				return ResourceManager.MonkeyCertificate;

			default:
				throw ctx.AssertFail (Type);
			}
		}

		bool CertificateValidationCallback (TestContext ctx, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors)
		{
			Interlocked.Increment (ref validatorCalled);
			LogDebug (ctx, 4, $"{nameof (CertificateValidationCallback)}: {certificate?.Subject} {Client.SslStream.IsAuthenticated}");

			switch (Type) {
			case SelectorType.NoCertificate:
			case SelectorType.CallbackReturnsNullButHaveCertificate:
				ctx.Assert (certificate, Is.Null);
				break;

			case SelectorType.SetAsConnectionParameter:
			case SelectorType.NoSelectorCallback:
			case SelectorType.RenegotiateButSetOnFirstCallback:
			case SelectorType.AcceptableIssuers:
			case SelectorType.AcceptableIssuersNoCallback:
				ctx.Assert (certificate, Is.EqualTo (ResourceManager.MonkeyCertificate));
				break;

			case SelectorType.SetOnRenegotiateCallback:
				if (validatorCalled == 1)
					ctx.Assert (certificate, Is.Null);
				else if (validatorCalled == 2)
					ctx.Assert (certificate, Is.EqualTo (ResourceManager.MonkeyCertificate));
				else
					throw ctx.AssertFail ($"{nameof (CertificateValidationCallback)} called too many times.");
				break;

			case SelectorType.MultipleCertificates:
				ctx.Assert (certificate, Is.EqualTo (ResourceManager.PenguinCertificate));
				break;

			default:
				throw ctx.AssertFail (Type);
			}

			return true;
		}

		protected override async Task HandleServer (TestContext ctx, CancellationToken cancellationToken)
		{
			var me = nameof (HandleServer);

			ctx.Assert (Server.SslStream.IsAuthenticated, nameof (Server.SslStream.IsAuthenticated));

			var oldSelectorCalled = selectorCalled;
			var selectorConstraint = setup.UsingDotNet ? Is.GreaterThanOrEqualTo (1) : Is.EqualTo (1);
			if (Parameters.ClientCertificateSelector != null)
				ctx.Assert (selectorCalled, selectorConstraint, $"{nameof (CertificateSelectionCallback)} called once.");
			if (Parameters.ServerCertificateValidator != null)
				ctx.Assert (validatorCalled, Is.EqualTo (1), $"{nameof (CertificateValidationCallback)} called.");

			switch (Type) {
			case SelectorType.NoCertificate:
			case SelectorType.CallbackReturnsNullButHaveCertificate:
				AssertNotMutuallyAuthenticated ();
				break;

			case SelectorType.SetAsConnectionParameter:
				AssertMutuallyAuthenticated ();
				break;

			case SelectorType.RenegotiateButSetOnFirstCallback:
				AssertMutuallyAuthenticated ();

				await Renegotiate ().ConfigureAwait (false);

				ctx.Assert (selectorCalled, selectorConstraint, $"{nameof (CertificateSelectionCallback)} not called again.");
				ctx.Assert (validatorCalled, Is.EqualTo (oldSelectorCalled + 1), $"{nameof (CertificateValidationCallback)} called again.");
				AssertMutuallyAuthenticated ();
				break;

			case SelectorType.SetOnRenegotiateCallback:
				AssertNotMutuallyAuthenticated ();

				await Renegotiate ().ConfigureAwait (false);

				ctx.Assert (selectorCalled, Is.EqualTo (oldSelectorCalled + 1), $"{nameof (CertificateSelectionCallback)} called again.");
				ctx.Assert (validatorCalled, Is.EqualTo (2), $"{nameof (CertificateValidationCallback)} called again.");
				AssertMutuallyAuthenticated ();
				break;

			case SelectorType.NoSelectorCallback:
				AssertMutuallyAuthenticated ();
				break;

			case SelectorType.AcceptableIssuers:
			case SelectorType.AcceptableIssuersNoCallback:
				AssertMutuallyAuthenticated ();
				break;

			case SelectorType.MultipleCertificates:
				AssertMutuallyAuthenticated (ResourceManager.PenguinCertificate);
				break;

			default:
				throw ctx.AssertFail (Type);
			}

			await base.HandleServer (ctx, cancellationToken).ConfigureAwait (false);

			LogDebug (ctx, 4, $"{me} done");

			async Task Renegotiate ()
			{
				LogDebug (ctx, 4, $"{me} before RenegotiateAsync().");

				if (!setup.CanRenegotiate (Server.SslStream))
					throw ctx.AssertFail ("Renegotiation not supported.");

				await setup.RenegotiateAsync (Server.SslStream, cancellationToken).ConfigureAwait (false);

				LogDebug (ctx, 4, $"{me} after RenegotiateAsync().");
			}

			void AssertNotMutuallyAuthenticated ()
			{
				ctx.Assert (Server.SslStream.IsMutuallyAuthenticated, Is.False, nameof (Server.SslStream.IsMutuallyAuthenticated));
				ctx.Assert (Server.SslStream.RemoteCertificate, Is.Null, nameof (Server.SslStream.RemoteCertificate));
			}

			void AssertMutuallyAuthenticated (X509Certificate expectedCertificate = null)
			{
				if (expectedCertificate == null)
					expectedCertificate = ResourceManager.MonkeyCertificate;
				ctx.Assert (Server.SslStream.IsMutuallyAuthenticated, Is.True, nameof (Server.SslStream.IsMutuallyAuthenticated));
				ctx.Assert (Server.SslStream.RemoteCertificate, Is.EqualTo (expectedCertificate), nameof (Server.SslStream.RemoteCertificate));
			}
		}

		protected override Task OnRun (TestContext ctx, CancellationToken cancellationToken) => FinishedTask;
	}
}
