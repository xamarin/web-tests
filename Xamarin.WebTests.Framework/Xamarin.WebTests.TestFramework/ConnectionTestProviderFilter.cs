//
// ConnectionTestProviderFilter.cs
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
using Xamarin.AsyncTests;

namespace Xamarin.WebTests.TestFramework
{
	using ConnectionFramework;
	using TestRunners;

	public class ConnectionTestProviderFilter : ConnectionProviderFilter
	{
		public ConnectionTestProviderFilter (ConnectionTestFlags flags)
			: base (flags)
		{
		}

		protected override ClientAndServerProvider Create (ConnectionProvider client, ConnectionProvider server)
		{
			return new ConnectionTestProvider (client, server, Flags);
		}

		public static ConnectionTestFlags GetConnectionFlags (TestContext ctx, ConnectionTestCategory category)
		{
			switch (category) {
			case ConnectionTestCategory.Https:
				return ConnectionTestFlags.RequireSslStream;
			case ConnectionTestCategory.HttpsWithMono:
				return ConnectionTestFlags.RequireSslStream;
			case ConnectionTestCategory.HttpsWithDotNet:
				return ConnectionTestFlags.RequireSslStream | ConnectionTestFlags.RequireTls12 | ConnectionTestFlags.RequireClientCertificates;
			case ConnectionTestCategory.SslStreamWithTls12:
				return ConnectionTestFlags.RequireSslStream | ConnectionTestFlags.RequireTls12;
			case ConnectionTestCategory.InvalidCertificatesInTls12:
				return ConnectionTestFlags.RequireSslStream | ConnectionTestFlags.RequireTls12 | ConnectionTestFlags.RequireClientCertificates;
			case ConnectionTestCategory.HttpsCertificateValidators:
				return ConnectionTestFlags.RequireHttp;
			case ConnectionTestCategory.SslStreamCertificateValidators:
				return ConnectionTestFlags.RequireSslStream;
			case ConnectionTestCategory.TrustedRoots:
			case ConnectionTestCategory.CertificateStore:
				return ConnectionTestFlags.RequireTrustedRoots;
			case ConnectionTestCategory.SimpleMonoClient:
				return ConnectionTestFlags.RequireMono;
			case ConnectionTestCategory.SimpleMonoServer:
				return ConnectionTestFlags.RequireMono;
			case ConnectionTestCategory.SimpleMonoConnection:
			case ConnectionTestCategory.MonoProtocolVersions:
				return ConnectionTestFlags.RequireMono;
			case ConnectionTestCategory.CertificateChecks:
			case ConnectionTestCategory.SecurityFramework:
				return ConnectionTestFlags.RequireMono;
			case ConnectionTestCategory.SslStreamInstrumentation:
			case ConnectionTestCategory.SslStreamInstrumentationExperimental:
				return ConnectionTestFlags.RequireSslStream | ConnectionTestFlags.RequireTls12 | ConnectionTestFlags.RequireMono;
			case ConnectionTestCategory.SslStreamInstrumentationMono:
				return ConnectionTestFlags.RequireSslStream | ConnectionTestFlags.RequireTls12 | ConnectionTestFlags.RequireMono;
			case ConnectionTestCategory.SslStreamInstrumentationShutdown:
				return ConnectionTestFlags.RequireSslStream | ConnectionTestFlags.RequireMono | ConnectionTestFlags.RequireCleanShutdown;
			case ConnectionTestCategory.SslStreamInstrumentationServerShutdown:
				return ConnectionTestFlags.RequireSslStream | ConnectionTestFlags.RequireMono | ConnectionTestFlags.RequireCleanServerShutdown;
			case ConnectionTestCategory.SslStreamInstrumentationRecentlyFixed:
				return ConnectionTestFlags.RequireSslStream | ConnectionTestFlags.RequireTls12;
			case ConnectionTestCategory.SslStreamInstrumentationNewWebStack:
				return ConnectionTestFlags.RequireSslStream | ConnectionTestFlags.RequireTls12;
			case ConnectionTestCategory.HttpStress:
			case ConnectionTestCategory.HttpStressExperimental:
				return ConnectionTestFlags.RequireHttp | ConnectionTestFlags.RequireSslStream | ConnectionTestFlags.RequireTls12;
			case ConnectionTestCategory.MartinTest:
				return ConnectionTestFlags.AssumeSupportedByTest;
			default:
				ctx.AssertFail ("Unsupported instrumentation category: '{0}'.", category);
				return ConnectionTestFlags.None;
			}
		}
	}
}

