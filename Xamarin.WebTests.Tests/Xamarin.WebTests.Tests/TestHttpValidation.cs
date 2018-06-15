﻿//
// TestHttpValidation.cs
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
using System.IO;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using System.Collections;
using System.Collections.Generic;

using Xamarin.AsyncTests;
using Xamarin.AsyncTests.Portable;
using Xamarin.AsyncTests.Constraints;

namespace Xamarin.WebTests.Tests
{
	using TestAttributes;
	using TestFramework;
	using TestRunners;
	using HttpFramework;
	using HttpHandlers;
	using HttpOperations;

	[New]
	[AsyncTestFixture (Timeout = 5000)]
	public class TestHttpValidation
	{
		[AsyncTest]
		[ConnectionTestCategory (ConnectionTestCategory.HttpsWithMono)]
		public async Task TestMonoConnection (TestContext ctx, CancellationToken cancellationToken,
			ConnectionTestProvider provider, HttpValidationTestParameters parameters,
			HttpValidationTestRunner runner)
		{
			await runner.Run (ctx, cancellationToken);
		}

		[AsyncTest]
		[ConnectionTestCategory (ConnectionTestCategory.HttpsWithDotNet)]
		public async Task TestDotNetConnection (TestContext ctx, CancellationToken cancellationToken,
			ConnectionTestProvider provider, HttpValidationTestParameters parameters,
			HttpValidationTestRunner runner)
		{
			await runner.Run (ctx, cancellationToken);
		}

		[AsyncTest]
		[ConnectionTestCategory (ConnectionTestCategory.HttpsCertificateValidators)]
		public async Task TestCertificateValidators (TestContext ctx, CancellationToken cancellationToken,
			ConnectionTestProvider provider, HttpValidationTestParameters parameters,
			HttpValidationTestRunner runner)
		{
			await runner.Run (ctx, cancellationToken);
		}

		[AsyncTest]
		[ConnectionTestCategory (ConnectionTestCategory.TrustedRoots)]
		public async Task TestTrustedRoots (TestContext ctx, CancellationToken cancellationToken,
			ConnectionTestProvider provider, HttpValidationTestParameters parameters,
			HttpValidationTestRunner runner)
		{
			await runner.Run (ctx, cancellationToken);
		}

		[AsyncTest]
		[Martin ("HttpsCertificateStore")]
		[CertificateStore]
		[ConnectionTestFlags (ConnectionTestFlags.RequireTrustedRoots)]
		[ConnectionTestCategory (ConnectionTestCategory.CertificateStore)]
		public async Task TestCertificateStore (TestContext ctx, CancellationToken cancellationToken,
			ConnectionTestProvider provider, HttpValidationTestParameters parameters,
			HttpValidationTestRunner runner)
		{
			await runner.Run (ctx, cancellationToken);
		}

		[AsyncTest]
		[Martin ("Https")]
		[ConnectionTestFlags (ConnectionTestFlags.RequireTrustedRoots)]
		[ConnectionTestCategory (ConnectionTestCategory.MartinTest)]
		public async Task MartinTest (TestContext ctx, CancellationToken cancellationToken,
			ConnectionTestProvider provider, HttpValidationTestParameters parameters,
			HttpValidationTestRunner runner)
		{
			await runner.Run (ctx, cancellationToken);
		}

		[AsyncTest]
		[Martin ("HttpsAuth")]
		[ConnectionTestFlags (ConnectionTestFlags.RequireTrustedRoots)]
		[ConnectionTestCategory (ConnectionTestCategory.MartinTest)]
		public async Task MartinTest2 (TestContext ctx, HttpServer server, CancellationToken cancellationToken)
		{
			var handler = HelloWorldHandler.GetSimple ();
			var authHandler = new AuthenticationHandler (AuthenticationType.NTLM, handler);
			using (var operation = new TraditionalOperation (server, authHandler, true))
				await operation.Run (ctx, cancellationToken).ConfigureAwait (false);

		}
	}
}

