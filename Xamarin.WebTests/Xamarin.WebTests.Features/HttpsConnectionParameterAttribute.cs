//
// HttpsConnectionParameterAttribute.cs
//
// Author:
//       Martin Baulig <martin.baulig@xamarin.com>
//
// Copyright (c) 2015 Xamarin, Inc.
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
using System.Collections.Generic;
using Xamarin.AsyncTests;
using Xamarin.AsyncTests.Framework;
using Xamarin.AsyncTests.Portable;

namespace Xamarin.WebTests.Features
{
	using ConnectionFramework;
	using HttpFramework;
	using Portable;
	using Providers;
	using Resources;

	[AttributeUsage (AttributeTargets.Parameter | AttributeTargets.Property, AllowMultiple = false)]
	public class HttpsConnectionParameterAttribute : TestParameterAttribute, ITestParameterSource<IClientAndServerParameters>
	{
		public ICertificateValidator AcceptAll {
			get;
			private set;
		}

		public ICertificateValidator RejectAll {
			get;
			private set;
		}

		public IServerCertificate DefaultCertificate {
			get;
			private set;
		}

		public IServerCertificate SelfSignedCertificate {
			get;
			private set;
		}

		public HttpsConnectionParameterAttribute (string filter = null, TestFlags flags = TestFlags.Browsable)
			: base (filter, flags)
		{
			var certificateProvider = DependencyInjector.Get<ICertificateProvider> ();
			AcceptAll = certificateProvider.AcceptAll ();
			RejectAll = certificateProvider.RejectAll ();

			DefaultCertificate = ResourceManager.DefaultServerCertificate;
			SelfSignedCertificate = ResourceManager.SelfSignedServerCertificate;
		}

		public IEnumerable<IClientAndServerParameters> GetParameters (TestContext ctx, string filter)
		{
			var defaultServer = new ServerParameters ("default", DefaultCertificate);
			var selfSignedServer = new ServerParameters ("self-signed", SelfSignedCertificate);

			var acceptAllClient = new ClientParameters ("accept-all") { CertificateValidator = AcceptAll };

			yield return ClientAndServerParameters.Create (acceptAllClient, defaultServer);
			yield return ClientAndServerParameters.Create (acceptAllClient, selfSignedServer);

			yield return ClientAndServerParameters.Create (new ClientParameters ("no-validator") {
				ExpectTrustFailure = true
			}, new ServerParameters ("self-signed-error", SelfSignedCertificate) {
				ExpectException = true
			});

			// Explicit validator overrides the default ServicePointManager.ServerCertificateValidationCallback.
			yield return ClientAndServerParameters.Create (new ClientParameters ("reject-all") {
				ExpectTrustFailure = true, CertificateValidator = RejectAll
			}, new ServerParameters ("default-error", DefaultCertificate) {
				ExpectException = true
			});
		}
	}
}

