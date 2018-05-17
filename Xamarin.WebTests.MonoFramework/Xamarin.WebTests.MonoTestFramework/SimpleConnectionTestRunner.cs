//
// SimpleConnectionTestRunner.cs
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
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using Mono.Security.Interface;
using Xamarin.AsyncTests;
using Xamarin.AsyncTests.Constraints;
using Xamarin.WebTests.ConnectionFramework;
using Xamarin.WebTests.MonoTestFramework;
using Xamarin.WebTests.TestRunners;
using Xamarin.WebTests.Resources;
using Xamarin.WebTests.TestFramework;

namespace Xamarin.WebTests.MonoTestFramework
{
	using MonoTestFeatures;

	[SimpleConnectionTestRunner]
	public class SimpleConnectionTestRunner : MonoConnectionTestRunner
	{
		protected override ConnectionParameters CreateParameters (TestContext ctx) => ctx.GetParameter<SimpleConnectionParameters> ();

		public static IEnumerable<SimpleConnectionType> GetTestTypes (TestContext ctx, ConnectionTestCategory category)
		{
			switch (category) {
			case ConnectionTestCategory.SimpleMonoClient:
				yield return SimpleConnectionType.SimpleTls10;
				yield return SimpleConnectionType.SimpleTls11;
				yield return SimpleConnectionType.SimpleTls12;
				yield break;

			case ConnectionTestCategory.SimpleMonoServer:
				yield return SimpleConnectionType.SimpleTls10;
				yield return SimpleConnectionType.SimpleTls11;
				yield return SimpleConnectionType.SimpleTls12;
				yield break;

			case ConnectionTestCategory.SimpleMonoConnection:
				yield return SimpleConnectionType.SimpleTls10;
				yield return SimpleConnectionType.SimpleTls11;
				yield return SimpleConnectionType.SimpleTls12;
				yield break;

			case ConnectionTestCategory.MonoProtocolVersions:
				yield return SimpleConnectionType.Simple;
				yield return SimpleConnectionType.ValidateCertificate;
				yield return SimpleConnectionType.RequestClientCertificate;
				yield return SimpleConnectionType.RequireClientCertificateRSA;
				yield return SimpleConnectionType.RequireClientCertificateDHE;
				yield break;

			case ConnectionTestCategory.SecurityFramework:
				yield return SimpleConnectionType.Simple;
				yield break;

			case ConnectionTestCategory.MartinTest:
				yield return SimpleConnectionType.MartinTest;
				yield break;

			default:
				ctx.AssertFail ("Unspported connection category: '{0}.", category);
				yield break;
			}
		}

		public static IEnumerable<SimpleConnectionParameters> GetParameters (TestContext ctx, ClientAndServerProvider provider, ConnectionTestCategory category)
		{
			return GetTestTypes (ctx, category).Select (t => Create (ctx, provider, category, t));
		}

		static SimpleConnectionParameters CreateParameters (ConnectionTestCategory category, SimpleConnectionType type, params object[] args)
		{
			var sb = new StringBuilder ();
			sb.Append (type);
			foreach (var arg in args) {
				sb.AppendFormat (":{0}", arg);
			}
			var name = sb.ToString ();

			return new SimpleConnectionParameters (category, type, name, ResourceManager.SelfSignedServerCertificate) {
				ClientCertificateValidator = AcceptAnyCertificate
			};
		}

		static SimpleConnectionParameters Create (TestContext ctx, ClientAndServerProvider provider, ConnectionTestCategory category, SimpleConnectionType type)
		{
			var parameters = CreateParameters (category, type);

			var certificateProvider = DependencyInjector.Get<ICertificateProvider> ();
			var acceptSelfSigned = certificateProvider.AcceptThisCertificate (ResourceManager.SelfSignedServerCertificate);
			var acceptFromCA = certificateProvider.AcceptFromCA (ResourceManager.LocalCACertificate);

			switch (type) {
			case SimpleConnectionType.Simple:
				break;

			case SimpleConnectionType.ValidateCertificate:
				parameters.ServerCertificate = ResourceManager.ServerCertificateFromCA;
				parameters.ClientCertificateValidator = acceptFromCA;
				break;

			case SimpleConnectionType.SimpleTls10:
				parameters.ProtocolVersion = ProtocolVersions.Tls10;
				break;

			case SimpleConnectionType.SimpleTls11:
				parameters.ProtocolVersion = ProtocolVersions.Tls11;
				break;

			case SimpleConnectionType.SimpleTls12:
				parameters.ProtocolVersion = ProtocolVersions.Tls12;
				break;

			case SimpleConnectionType.RequestClientCertificate:
				/*
				 * Request client certificate, but do not require it.
				 *
				 * FIXME:
				 * SslStream with Mono's old implementation fails here.
				 */
				parameters.ClientCertificate = ResourceManager.MonkeyCertificate;
				parameters.ClientCertificateValidator = acceptSelfSigned;
				parameters.AskForClientCertificate = true;
				parameters.ServerCertificateValidator = acceptFromCA;
				break;

			case SimpleConnectionType.RequireClientCertificateRSA:
				/*
				 * Require client certificate.
				 *
				 */
				parameters.ClientCertificate = ResourceManager.MonkeyCertificate;
				parameters.ClientCertificateValidator = acceptSelfSigned;
				parameters.RequireClientCertificate = true;
				parameters.ServerCertificateValidator = acceptFromCA;
				break;

			case SimpleConnectionType.RequireClientCertificateDHE:
				/*
				 * Require client certificate.
				 *
				 */
				parameters.ClientCertificate = ResourceManager.MonkeyCertificate;
				parameters.ClientCertificateValidator = acceptSelfSigned;
				parameters.RequireClientCertificate = true;
				parameters.ServerCertificateValidator = acceptFromCA;
				break;

			case SimpleConnectionType.MartinTest:
				parameters.ServerCertificate = ResourceManager.GetCertificateWithKey (CertificateResourceType.SelfSignedServerCertificate);
				break;

			default:
				ctx.AssertFail ("Unsupported connection type: '{0}'.", type);
				break;
			}

			return parameters;
		}
	}
}

