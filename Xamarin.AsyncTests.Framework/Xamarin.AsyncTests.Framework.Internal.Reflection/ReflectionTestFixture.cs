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
		List<ReflectionTestCase> tests;
		IList<string> categories;

		public override IEnumerable<string> Categories {
			get { return categories; }
		}

		public override int CountTests {
			get { return tests.Count; }
		}

		public override TestCase[] Tests {
			get { return tests.ToArray (); }
		}

		public ReflectionTestFixture (TestSuite suite, AsyncTestFixtureAttribute attr, TypeInfo type)
			: base (suite, attr, type)
		{
			Resolve (suite, null, type, out categories);
		}

		public override bool Resolve ()
		{
			tests = new List<ReflectionTestCase> ();

			foreach (var method in Type.DeclaredMethods) {
				if (method.IsStatic || !method.IsPublic)
					continue;
				var attr = method.GetCustomAttribute<AsyncTestAttribute> (true);
				if (attr == null)
					continue;

				tests.Add (new ReflectionTestCase (this, attr, method));
			}

			return true;
		}

		internal static void Resolve (
			TestSuite suite, TestFixture parent, MemberInfo member, out IList<string> categories)
		{
			categories = new List<string> ();

			if (parent != null) {
				foreach (var category in parent.Categories)
					categories.Add (category);
			}

			string fullName;
			if (member is TypeInfo)
				fullName = ((TypeInfo)member).FullName;
			else if (member is MethodInfo) {
				var method = (MethodInfo)member;
				fullName = method.DeclaringType.FullName + "." + method.Name;
			} else {
				fullName = member.ToString ();
			}

			var attrs = member.GetCustomAttributes (typeof(TestCategoryAttribute), false);

			foreach (var obj in attrs) {
				var category = obj as TestCategoryAttribute;
				if (category == null)
					continue;

				if (categories.Contains (category.Name)) {
					Debug.WriteLine ("Duplicate [{0}] in {1}.", category.Name, fullName);
					continue;
				}

				categories.Add (category.Name);
			}
		}

		internal override TestInvoker CreateInvoker (TestContext context)
		{
			var invoker = ReflectionTestFixtureInvoker.Create (context, this);

			var parameterHosts = new List<TestHost> ();

			if (Attribute.Repeat != 0)
				parameterHosts.Add (new RepeatedTestHost (Attribute.Repeat, TestFlags.Browsable));

			var properties = Type.DeclaredProperties.ToArray ();
			for (int i = properties.Length - 1; i >= 0; i--) {
				var member = ReflectionHelper.GetPropertyInfo (properties [i]);

				foreach (var host in ReflectionTestCase.ResolveParameter (context, member))
					parameterHosts.Add (new ReflectionPropertyHost (this, properties [i], host));
			}

			parameterHosts.Add (new FixtureTestHost (this));

			foreach (var parameter in parameterHosts) {
				invoker = parameter.CreateInvoker (invoker);
			}

			return new ProxyTestInvoker (Name.Name, invoker);
		}

		internal override async Task InitializeInstance (TestContext context, CancellationToken cancellationToken)
		{
			var instance = (TestFixtureInstance)context.Instance;
			var fixtureInstance = instance.Instance as IAsyncTestFixture;
			if (fixtureInstance != null)
				await fixtureInstance.SetUp (context, cancellationToken);

			context.Debug (5, "INITIALIZE INSTANCE", instance);
		}

		internal override async Task DestroyInstance (TestContext context, CancellationToken cancellationToken)
		{
			var instance = (TestFixtureInstance)context.Instance;
			var fixtureInstance = instance.Instance as IAsyncTestFixture;
			if (fixtureInstance != null)
				await fixtureInstance.TearDown (context, cancellationToken);
		}

		internal IEnumerable<ReflectionTestCase> Filter (TestContext context)
		{
			return tests.Where (t => context.Filter (t));
		}

		class ReflectionTestFixtureInvoker : AggregatedTestInvoker
		{
			ReflectionTestFixtureInvoker ()
				: base (TestFlags.None)
			{
			}

			public static TestInvoker Create (TestContext context, ReflectionTestFixture fixture)
			{
				var selected = fixture.Filter (context);
				var invoker = new ReflectionTestFixtureInvoker ();
				invoker.Resolve (context, selected);
				return invoker;
			}

			public void Resolve (TestContext context, IEnumerable<TestCase> selectedTests)
			{
				foreach (var test in selectedTests) {
					var invoker = test.CreateInvoker (context);
					InnerTestInvokers.Add (invoker);
				}
			}
		}
	}
}

