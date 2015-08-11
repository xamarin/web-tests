//
// ConnectionTestRunner.cs
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

namespace Xamarin.WebTests.TestRunners
{
	using ConnectionFramework;
	using Providers;
	using Resources;
	using Portable;

	public abstract class ConnectionTestRunner : ClientAndServerTestRunner
	{
		public ConnectionTestRunner (IServer server, IClient client, ConnectionTestParameters parameters)
			: base (server, client, parameters)
		{
		}

		public static IEnumerable<ConnectionTestType> GetConnectionTestTypes (TestContext ctx, ConnectionTestCategory category)
		{
			switch (category) {
			case ConnectionTestCategory.Https:
				return AllTestTypes;
			case ConnectionTestCategory.HttpsWithMono:
				return OldMonoTestTypes;
			case ConnectionTestCategory.HttpsWithDotNet:
				return DotNetTestTypes;
			default:
				ctx.AssertFail ("Unsupported test category: '{0}'.", category);
				throw new NotImplementedException ();
			}
		}

		static IEnumerable<ConnectionTestType> OldMonoTestTypes {
			get {
				yield return ConnectionTestType.Default;
				yield return ConnectionTestType.AcceptFromLocalCA;
				yield return ConnectionTestType.RejectAll;
				yield return ConnectionTestType.UnrequestedClientCertificate;
				yield return ConnectionTestType.RejectClientCertificate;
				yield return ConnectionTestType.MissingClientCertificate;
			}
		}

		static IEnumerable<ConnectionTestType> AllTestTypes {
			get {
				yield return ConnectionTestType.Default;
				yield return ConnectionTestType.AcceptFromLocalCA;
				yield return ConnectionTestType.NoValidator;
				yield return ConnectionTestType.RejectAll;
				yield return ConnectionTestType.RequestClientCertificate;
				yield return ConnectionTestType.RequireClientCertificate;
				yield return ConnectionTestType.RejectClientCertificate;
				yield return ConnectionTestType.UnrequestedClientCertificate;
				yield return ConnectionTestType.OptionalClientCertificate;
				yield return ConnectionTestType.RejectClientCertificate;
				yield return ConnectionTestType.MissingClientCertificate;
			}
		}

		static IEnumerable<ConnectionTestType> DotNetTestTypes {
			get {
				yield return ConnectionTestType.NoValidator;
				yield return ConnectionTestType.RequestClientCertificate;
				yield return ConnectionTestType.RequireClientCertificate;
				yield return ConnectionTestType.RejectClientCertificate;
				yield return ConnectionTestType.UnrequestedClientCertificate;
				yield return ConnectionTestType.OptionalClientCertificate;
				yield return ConnectionTestType.RejectClientCertificate;
				yield return ConnectionTestType.MissingClientCertificate;
			}
		}
	}
}

