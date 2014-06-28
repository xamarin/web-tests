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

namespace Xamarin.AsyncTests.Framework.Reflection
{
	class ReflectionTestFixture : ReflectionTest
	{
		public TypeInfo Type {
			get;
			private set;
		}

		internal override TestInvoker Invoker {
			get { return invoker; }
		}

		readonly TestInvoker invoker;

		public ReflectionTestFixture (TestSuite suite, AsyncTestFixtureAttribute attr, TypeInfo type)
			: base (new TestName (type.Name), attr, ReflectionHelper.GetTypeInfo (type))
		{
			Type = type;

			invoker = Resolve ();
		}

		TestInvoker Resolve ()
		{
			var aggregatedInvoker = new AggregatedTestInvoker (TestFlags.ContinueOnError);

			var tests = new List<TestCase> ();

			foreach (var method in Type.DeclaredMethods) {
				if (method.IsStatic || !method.IsPublic)
					continue;
				var attr = method.GetCustomAttribute<AsyncTestAttribute> (true);
				if (attr == null)
					continue;

				var test = new ReflectionTestCase (this, attr, method);
				aggregatedInvoker.InnerTestInvokers.Add (test.Invoker);
				tests.Add (test);
			}

			var parameterHosts = new List<TestHost> ();

			if (Attribute.Repeat != 0)
				parameterHosts.Add (new RepeatedTestHost (Attribute.Repeat, TestFlags.Browsable));

			var properties = Type.DeclaredProperties.ToArray ();
			for (int i = properties.Length - 1; i >= 0; i--) {
				var member = ReflectionHelper.GetPropertyInfo (properties [i]);

				foreach (var host in ReflectionTest.ResolveParameter (Type, member))
					parameterHosts.Add (new ReflectionPropertyHost (this, properties [i], host));
			}

			parameterHosts.Add (new ReflectionTestFixtureHost (this));

			TestInvoker invoker = aggregatedInvoker;

			foreach (var parameter in parameterHosts) {
				invoker = parameter.CreateInvoker (invoker);
			}

			return new ProxyTestInvoker (Name.Name, invoker);
		}

		class ReflectionTestFixtureHost : TestHost
		{
			public ReflectionTestFixture Fixture {
				get;
				private set;
			}

			public ReflectionTestFixtureHost (ReflectionTestFixture fixture)
			{
				Flags = TestFlags.ContinueOnError;
				Fixture = fixture;
			}

			internal override TestInstance CreateInstance (TestContext context, TestInstance parent)
			{
				var instance = Activator.CreateInstance (Fixture.Type.AsType ());
				return new FixtureTestInstance (this, instance);
			}
		}
	}
}

