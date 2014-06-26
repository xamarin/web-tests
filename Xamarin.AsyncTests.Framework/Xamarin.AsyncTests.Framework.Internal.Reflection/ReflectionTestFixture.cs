//
// ReflectionTestFixture.cs
//
// Author:
//       Martin Baulig <martin.baulig@xamarin.com>
//
// Copyright (c) 2014 Xamarin Inc. (http://www.xamarin.com)
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
using System.Diagnostics;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace Xamarin.AsyncTests.Framework.Internal.Reflection
{
	class ReflectionTestFixture : TestFixture
	{
		List<TestHost> parameterHosts;

		public ReflectionTestFixture (TestSuite suite, AsyncTestFixtureAttribute attr, TypeInfo type)
			: base (suite, attr, type)
		{
			ResolveParameters ();
		}

		void ResolveParameters ()
		{
			foreach (var method in Type.DeclaredMethods) {
				if (method.IsStatic || !method.IsPublic)
					continue;
				var attr = method.GetCustomAttribute<AsyncTestAttribute> (true);
				if (attr == null)
					continue;

				Tests.Add (new ReflectionTestCase (this, attr, method));
			}

			parameterHosts = new List<TestHost> ();

			if (Attribute.Repeat != 0)
				parameterHosts.Add (new RepeatedTestHost (Attribute.Repeat, TestFlags.Browsable));

			var properties = Type.DeclaredProperties.ToArray ();
			for (int i = properties.Length - 1; i >= 0; i--) {
				var member = ReflectionHelper.GetPropertyInfo (properties [i]);

				foreach (var host in ReflectionTestCase.ResolveParameter (member))
					parameterHosts.Add (new ReflectionPropertyHost (this, properties [i], host));
			}

			parameterHosts.Add (new FixtureTestHost (this));
		}

		internal override TestInvoker CreateInvoker (TestContext context)
		{
			var invoker = Tests.CreateInvoker (context);

			foreach (var parameter in parameterHosts) {
				invoker = parameter.CreateInvoker (invoker);
			}

			return new ProxyTestInvoker (Name.Name, invoker);
		}

		internal override async Task InitializeInstance (TestContext context, CancellationToken cancellationToken)
		{
			var instance = (FixtureTestInstance)context.Instance;
			var fixtureInstance = instance.Instance as IAsyncTestFixture;
			if (fixtureInstance != null)
				await fixtureInstance.SetUp (context, cancellationToken);
		}

		internal override async Task ReuseInstance (TestContext context, CancellationToken cancellationToken)
		{
			var instance = (FixtureTestInstance)context.Instance;
			var fixtureInstance = instance.Instance as IAsyncTestFixture;
			if (fixtureInstance != null)
				await fixtureInstance.ReuseInstance (context, cancellationToken);
		}

		internal override async Task DestroyInstance (TestContext context, CancellationToken cancellationToken)
		{
			var instance = (FixtureTestInstance)context.Instance;
			var fixtureInstance = instance.Instance as IAsyncTestFixture;
			if (fixtureInstance != null)
				await fixtureInstance.TearDown (context, cancellationToken);
		}
	}
}

