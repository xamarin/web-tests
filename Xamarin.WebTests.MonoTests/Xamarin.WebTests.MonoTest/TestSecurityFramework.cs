//
// TestSecurityFramework.cs
//
// Author:
//       Martin Baulig <martin.baulig@xamarin.com>
//
// Copyright (c) 2016 Xamarin Inc. (http://www.xamarin.com)
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
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Xamarin.AsyncTests;
using Xamarin.AsyncTests.Framework;
using Xamarin.AsyncTests.Constraints;
using Xamarin.AsyncTests.Portable;
using Xamarin.WebTests.TestFramework;
using Xamarin.WebTests.Resources;
using Mono.Security.Interface;

namespace Xamarin.WebTests.MonoTests
{
	using MonoConnectionFramework;
	using MonoTestFramework;
	using MonoTestFeatures;

	[Mobile]
	[NotWorking]
	[AsyncTestFixture]
	public class TestSecurityFramework
	{
		[Martin]
		[AsyncTest]
		public async Task MartinTest (TestContext ctx, CancellationToken cancellationToken)
		{
			await Task.Yield ();
			ctx.LogMessage ("HELLO WORLD!");
		}

		[AsyncTest]
		public void DefaultProvider (TestContext ctx)
		{
			var available = DependencyInjector.IsAvailable (typeof(IAppleCertificateProvider));
			var provider = MonoTlsProviderFactory.GetDefaultProvider ();
			ctx.LogMessage ("Default TLS Provider: {0} {1} {2}", provider.Name, provider.ID, available);
			if (available)
				ctx.Assert (provider.ID, Is.EqualTo ("981af8af-a3a3-419a-9f01-a518e3a17c1c"), "apple-tls");
			else
				ctx.Assert (provider.ID, Is.EqualTo ("97d31751-d0b3-4707-99f7-a6456b972a19"), "old-tls");
		}

		[AsyncTest]
		public void DefaultSecurityProtocolType (TestContext ctx)
		{
			var available = DependencyInjector.IsAvailable (typeof(IAppleCertificateProvider));
			var protocol = ServicePointManager.SecurityProtocol;
			ctx.LogMessage ("Default ServicePointManager.SecurityProtocol: {0:x} ({1})", protocol, available);

			var has12 = (protocol & SecurityProtocolType.Tls12) == SecurityProtocolType.Tls12;
			var hasSsl = (protocol & SecurityProtocolType.Ssl3) != 0;

			ctx.Assert (hasSsl, Is.False, "SecurityProtocolType.Ssl3 must be disabled.");
			if (available)
				ctx.Assert (has12, Is.True, "Tls12 must be enabled.");
		}

		[Work]
		[AsyncTest]
		[SecurityFramework]
		[MonoConnectionTestFlags (MonoConnectionTestFlags.RequireTls12)]
		[MonoConnectionTestCategory (MonoConnectionTestCategory.SecurityFramework)]
		public async Task TestInKeyChain (TestContext ctx, CancellationToken cancellationToken,
			AppleCertificateHost certificateHost,
			MonoConnectionTestProvider provider,
			SimpleConnectionParameters parameters,
			SimpleConnectionTestRunner runner)
		{
			var appleCertificateProvider = DependencyInjector.Get<IAppleCertificateProvider> ();
			ctx.Assert (appleCertificateProvider.IsInKeyChain (certificateHost.AppleCertificate), "certificate must be in keychain");
			await runner.Run (ctx, cancellationToken);
		}

		[Work]
		[AsyncTest]
		[SecurityFramework]
		[MonoConnectionTestFlags (MonoConnectionTestFlags.RequireTls12)]
		[MonoConnectionTestCategory (MonoConnectionTestCategory.SecurityFramework)]
		public async Task TestInKeyChain2 (TestContext ctx, CancellationToken cancellationToken,
			[AppleCertificateHost (CertificateResourceType.ServerCertificateFromLocalCA)]
			AppleCertificateHost certificateHost,
			MonoConnectionTestProvider provider,
			SimpleConnectionParameters parameters,
			SimpleConnectionTestRunner runner)
		{
			var appleCertificateProvider = DependencyInjector.Get<IAppleCertificateProvider> ();
			ctx.Assert (appleCertificateProvider.IsInKeyChain (certificateHost.AppleCertificate), "certificate must be in keychain");
			await runner.Run (ctx, cancellationToken);
		}

		[Work]
		[AsyncTest]
		[SecurityFramework] [IOS]
		[MonoConnectionTestFlags (MonoConnectionTestFlags.RequireTls12)]
		[MonoConnectionTestCategory (MonoConnectionTestCategory.SecurityFramework)]
		public async Task TestNotInKeyChain (TestContext ctx, CancellationToken cancellationToken,
			MonoConnectionTestProvider provider,
			SimpleConnectionParameters parameters,
			SimpleConnectionTestRunner runner)
		{
			// FIXME: SecPKCS12Import actually imports the certificate and its private key into the keychain.
			var appleCertificateProvider = DependencyInjector.Get<IAppleCertificateProvider> ();
			ctx.Assert (appleCertificateProvider.IsInKeyChain (parameters.ServerCertificate), Is.False, "certificate must not be in keychain");
			await runner.Run (ctx, cancellationToken);
		}
	}
}

