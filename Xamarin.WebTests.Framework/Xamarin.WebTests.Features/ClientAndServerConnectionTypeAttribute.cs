//
// ClientAndServerConnectionTypeAttribute.cs
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
using System.Collections.Generic;
using Xamarin.AsyncTests;
using Xamarin.AsyncTests.Framework;
using Xamarin.AsyncTests.Portable;

namespace Xamarin.WebTests.Features
{
	using ConnectionFramework;
	using TestRunners;
	using Providers;

	[AttributeUsage (AttributeTargets.Parameter | AttributeTargets.Property, AllowMultiple = false)]
	public class ClientAndServerConnectionTypeAttribute : TestParameterAttribute, ITestParameterSource<ClientAndServerConnectionType>
	{
		public ClientAndServerConnectionTypeAttribute (string filter = null, TestFlags flags = TestFlags.Browsable)
			: base (filter, flags)
		{
		}

		bool MatchesFilter (ConnectionProviderType type, string filter)
		{
			if (filter == null)
				return true;

			var parts = filter.Split (',');
			foreach (var part in parts) {
				if (type.ToString ().Equals (part))
					return true;
			}

			return false;
		}

		public IEnumerable<ClientAndServerConnectionType> GetParameters (TestContext ctx, string filter)
		{
			var category = ctx.GetParameter<ConnectionTestCategory> ();

			string clientFilter, serverFilter;
			if (filter == null)
				clientFilter = serverFilter = null;
			else {
				int pos = filter.IndexOf (':');
				if (pos < 0)
					clientFilter = serverFilter = filter;
				else {
					clientFilter = filter.Substring (0, pos);
					serverFilter = filter.Substring (pos + 1);
				}
			}

			var factory = DependencyInjector.Get<ConnectionProviderFactory> ();
			var providers = factory.GetProviders ();

			var supportedClientProviders = providers.Where (p => {
				if (!string.IsNullOrEmpty (clientFilter))
					return MatchesFilter (p, clientFilter);
				else if (factory.IsExplicit (p))
					return false;
				return ConnectionTestRunner.IsClientSupported (ctx, category, p);
			});

			var supportedServerProviders = providers.Where (p => {
				if (!string.IsNullOrEmpty (serverFilter))
					return MatchesFilter (p, serverFilter);
				else if (factory.IsExplicit (p))
					return false;
				return ConnectionTestRunner.IsServerSupported (ctx, category, p);
			});

			return ConnectionTestFeatures.Join (supportedClientProviders, supportedServerProviders, (c, s) => new ClientAndServerConnectionType (category, c, s));
		}
	}
}
