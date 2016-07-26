//
// MonoValidationTestRunner.cs
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
using System.Text;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;
using Mono.Security.Interface;
using Xamarin.AsyncTests;
using Xamarin.AsyncTests.Constraints;
using Xamarin.WebTests.Resources;
using Xamarin.WebTests.ConnectionFramework;
using Xamarin.WebTests.MonoConnectionFramework;

namespace Xamarin.WebTests.MonoTestFramework
{
	using MonoTestFeatures;

	[MonoValidationTestRunner]
	public class MonoValidationTestRunner : ValidationTestRunner
	{
		new public MonoValidationTestParameters Parameters {
			get {
				return (MonoValidationTestParameters)base.Parameters;
			}
		}

		public MonoValidationTestType Type {
			get { return Parameters.Type; }
		}

		public MonoValidationTestRunner (MonoValidationTestParameters parameters)
			: base (parameters)
		{
		}

		public static IEnumerable<MonoValidationTestType> GetTestTypes (TestContext ctx, ValidationTestCategory category)
		{
			switch (category) {
			case ValidationTestCategory.Default:
			case ValidationTestCategory.UseProvider:
				yield return MonoValidationTestType.EmptyHost;
				yield return MonoValidationTestType.WrongHost;
				yield return MonoValidationTestType.Success;
				yield return MonoValidationTestType.RejectSelfSigned;
				yield return MonoValidationTestType.RejectHamillerTube;
				yield return MonoValidationTestType.TestRunnerCallback;
				yield break;

			case ValidationTestCategory.AppleTls:
				yield return MonoValidationTestType.TestRunnerCallbackChain;
				yield break;

			case ValidationTestCategory.MartinTest:
				yield return MonoValidationTestType.MartinTest;
				yield break;

			default:
				ctx.AssertFail ("Unspported validation category: '{0}.", category);
				yield break;
			}
		}

		public static IEnumerable<MonoValidationTestParameters> GetParameters (TestContext ctx, ValidationTestCategory category)
		{
			return GetTestTypes (ctx, category).Select (t => Create (ctx, category, t));
		}

		static MonoValidationTestParameters CreateParameters (ValidationTestCategory category, MonoValidationTestType type, params object[] args)
		{
			var sb = new StringBuilder ();
			sb.Append (type);
			foreach (var arg in args) {
				sb.AppendFormat (":{0}", arg);
			}
			var name = sb.ToString ();

			return new MonoValidationTestParameters (category, type, name);
		}

		static MonoValidationTestParameters Create (TestContext ctx, ValidationTestCategory category, MonoValidationTestType type)
		{
			var parameters = CreateParameters (category, type);

			switch (type) {
			case MonoValidationTestType.MartinTest:
				parameters.Host = "tlstest-1.xamdev.com";
				parameters.Add (CertificateResourceType.TlsTestXamDevNew);
				parameters.Add (CertificateResourceType.TlsTestXamDevCA);
				parameters.UseTestRunnerCallback = true;
				parameters.ExpectSuccess = true;
				parameters.UseProvider = true;
				break;

			case MonoValidationTestType.EmptyHost:
				parameters.Host = string.Empty;
				parameters.Add (CertificateResourceType.TlsTestXamDevNew);
				parameters.Add (CertificateResourceType.TlsTestXamDevCA);
				parameters.ExpectSuccess = true;
				break;

			case MonoValidationTestType.WrongHost:
				parameters.Host = "invalid.xamdev-error.com";
				parameters.Add (CertificateResourceType.TlsTestXamDevNew);
				parameters.Add (CertificateResourceType.TlsTestXamDevCA);
				parameters.ExpectSuccess = false;
				break;

			case MonoValidationTestType.Success:
				parameters.Host = "tlstest-1.xamdev.com";
				parameters.Add (CertificateResourceType.TlsTestXamDevNew);
				parameters.Add (CertificateResourceType.TlsTestXamDevCA);
				parameters.ExpectSuccess = true;
				break;

			case MonoValidationTestType.RejectSelfSigned:
				parameters.Host = string.Empty;
				parameters.Add (CertificateResourceType.SelfSignedServerCertificate);
				parameters.ExpectSuccess = false;
				break;

			case MonoValidationTestType.RejectHamillerTube:
				parameters.Host = string.Empty;
				parameters.Add (CertificateResourceType.ServerCertificateFromLocalCA);
				parameters.Add (CertificateResourceType.HamillerTubeCA);
				parameters.ExpectSuccess = false;
				break;

			case MonoValidationTestType.TestRunnerCallback:
				parameters.Host = "tlstest-1.xamdev.com";
				parameters.Add (CertificateResourceType.TlsTestXamDevNew);
				parameters.Add (CertificateResourceType.TlsTestXamDevCA);
				parameters.UseTestRunnerCallback = true;
				parameters.ExpectSuccess = true;
				break;

			case MonoValidationTestType.TestRunnerCallbackChain:
				parameters.Host = "tlstest-1.xamdev.com";
				parameters.Add (CertificateResourceType.TlsTestXamDevNew);
				parameters.Add (CertificateResourceType.TlsTestXamDevCA);
				parameters.AddExpectedChainEntry (CertificateResourceType.TlsTestXamDevNew);
				parameters.AddExpectedChainEntry (CertificateResourceType.TlsTestXamDevCA);
				parameters.UseTestRunnerCallback = true;
				parameters.ExpectSuccess = true;
				break;

			default:
				ctx.AssertFail ("Unsupported validation type: '{0}'.", type);
				break;
			}

			return parameters;
		}

		public override void Run (TestContext ctx)
		{
			ctx.LogMessage ("RUN: {0}", this);

			var validator = GetValidator (ctx);
			ctx.Assert (validator, Is.Not.Null, "has validator");

			var certificates = GetCertificates ();

			validatorInvoked = 0;

			var result = validator.ValidateCertificate (Parameters.Host, false, certificates);
			AssertResult (ctx, result);
		}

		int validatorInvoked;

		bool ValidationCallback (TestContext ctx, string targetHost, X509Certificate certificate, X509Chain chain, MonoSslPolicyErrors sslPolicyErrors)
		{
			// `targetHost` is only non-null if we're called from `HttpWebRequest`.
			ctx.Assert (targetHost, Is.Null, "target host");
			ctx.Assert (certificate, Is.Not.Null, "certificate");
			if (Parameters.ExpectSuccess)
				ctx.Assert (sslPolicyErrors, Is.EqualTo (MonoSslPolicyErrors.None), "errors");
			else
				ctx.Assert (sslPolicyErrors, Is.Not.EqualTo (MonoSslPolicyErrors.None), "expect error");
			ctx.Assert (chain, Is.Not.Null, "chain");
			++validatorInvoked;

			if (Parameters.ExpectedChain != null) {
				var extraStore = chain.ChainPolicy.ExtraStore;
				ctx.Assert (extraStore, Is.Not.Null, "ChainPolicy.ExtraStore");
				ctx.Assert (extraStore.Count, Is.EqualTo (Parameters.ExpectedChain.Count), "ChainPolicy.ExtraStore.Count");
				var extraStoreCert = extraStore[0];
				ctx.Assert (extraStoreCert, Is.Not.Null, "ChainPolicy.ExtraStore[0]");
				ctx.Assert (extraStoreCert, Is.InstanceOfType (typeof (X509Certificate2)), "ChainPolicy.ExtraStore[0].GetType()");
			}

			return true;
		}

		ICertificateValidator GetValidator (TestContext ctx)
		{
			MonoTlsSettings settings = null;
			if (Parameters.UseTestRunnerCallback) {
				settings = MonoTlsSettings.CopyDefaultSettings ();
				settings.CallbackNeedsCertificateChain = true;
				settings.UseServicePointManagerCallback = false;
				settings.RemoteCertificateValidationCallback = (t, c, ch, e) => ValidationCallback (ctx, t, c, ch, e);
			}

			if (Parameters.UseProvider || Parameters.Category == ValidationTestCategory.UseProvider) {
				var factory = DependencyInjector.Get<ConnectionProviderFactory> ();
				ConnectionProviderType providerType;
				if (!ctx.TryGetParameter<ConnectionProviderType> (out providerType))
					providerType = ConnectionProviderType.DotNet;

				var provider = (MonoConnectionProvider)factory.GetProvider (providerType);
				return CertificateValidationHelper.GetValidator (settings, provider.MonoTlsProvider);
			} else {
				return CertificateValidationHelper.GetValidator (settings);
			}
		}

		protected void AssertResult (TestContext ctx, ValidationResult result)
		{
			if (Parameters.ExpectSuccess) {
				ctx.Assert (result, Is.Not.Null, "has result");
				ctx.Assert (result.Trusted, Is.True, "trusted");
				ctx.Assert (result.UserDenied, Is.False, "not user denied");
				ctx.Assert (result.ErrorCode, Is.EqualTo (0), "error code");
			} else {
				ctx.Assert (result, Is.Not.Null, "has result");
				ctx.Assert (result.Trusted, Is.False, "not trusted");
				ctx.Assert (result.UserDenied, Is.False, "not user denied");
				if (Parameters.ExpectError != null)
					ctx.Assert (result.ErrorCode, Is.EqualTo (Parameters.ExpectError.Value), "error code");
				else
					ctx.Assert (result.ErrorCode, Is.Not.EqualTo (0), "error code");
			}

			if (Parameters.UseTestRunnerCallback)
				ctx.Assert (validatorInvoked, Is.EqualTo (1), "validator invoked");
		}
	}
}

