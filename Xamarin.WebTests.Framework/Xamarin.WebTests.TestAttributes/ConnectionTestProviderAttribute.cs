//
// ConnectionTestProviderAttribute.cs
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

namespace Xamarin.WebTests.TestAttributes
{
	using ConnectionFramework;
	using TestFramework;
	using TestRunners;

	[AttributeUsage (AttributeTargets.Class | AttributeTargets.Parameter, AllowMultiple = false)]
	public class ConnectionTestProviderAttribute : TestParameterAttribute, ITestParameterSource<ConnectionTestProvider>
	{
		public ConnectionTestProviderAttribute (string filter = null, TestFlags flags = TestFlags.Browsable)
			: base (filter, flags)
		{
			Optional = true;
		}

		public bool Optional {
			get; set;
		}

		public IEnumerable<ConnectionTestProvider> GetParameters (TestContext ctx, string argument)
		{
			if (!ctx.TryGetParameter<ConnectionProviderFilter> (out var filter)) {
				ConnectionTestFlags flags = ConnectionTestFlags.None;
				if (ctx.TryGetParameter<ConnectionTestCategory> (out var category))
					flags = ConnectionTestRunner.GetConnectionFlags (ctx, category);

				if (ctx.TryGetParameter<ConnectionTestFlags> (out var explicitFlags))
					flags |= explicitFlags;

				filter = new ConnectionTestProviderFilter (flags);
			}

			var supportedProviders = filter.GetSupportedProviders (ctx, argument).Cast<ConnectionTestProvider> ().ToList ();
			if (!Optional && supportedProviders.Count == 0)
				ctx.AssertFail ("Could not find any supported ConnectionTestProvider.");

			return supportedProviders;
		}
	}
}
