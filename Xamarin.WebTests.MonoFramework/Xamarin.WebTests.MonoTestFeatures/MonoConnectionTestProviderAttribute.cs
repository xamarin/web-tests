//
// MonoConnectionTestProviderAttribute.cs
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

namespace Xamarin.WebTests.MonoTestFeatures
{
	using MonoTestFramework;

	[AttributeUsage (AttributeTargets.Class, AllowMultiple = false)]
	public class MonoConnectionTestProviderAttribute : TestParameterAttribute, ITestParameterSource<MonoConnectionTestProvider>
	{
		public MonoConnectionTestProviderAttribute (string filter = null, TestFlags flags = TestFlags.Browsable)
			: base (filter, flags)
		{
			// FIXME
			Optional = true;
		}

		public bool Optional {
			get; set;
		}

		public IEnumerable<MonoConnectionTestProvider> GetParameters (TestContext ctx, string argument)
		{
			var category = ctx.GetParameter<MonoConnectionTestCategory> ();

			MonoConnectionProviderFilter filter;
			if (!ctx.TryGetParameter<MonoConnectionProviderFilter> (out filter)) {
				var flags = MonoConnectionTestRunner.GetConnectionFlags (ctx, category);

				MonoConnectionTestFlags explicitFlags;
				if (ctx.TryGetParameter<MonoConnectionTestFlags> (out explicitFlags))
					flags |= explicitFlags;

				filter = new MonoConnectionProviderFilter (category, flags);
			}

			var supportedProviders = filter.GetSupportedProviders (ctx, argument).Cast<MonoConnectionTestProvider> ().ToList ();
			if (!Optional && supportedProviders.Count == 0)
				ctx.AssertFail ("Could not find any supported MonoConnectionTestProvider.");

			return supportedProviders;
		}
	}
}
