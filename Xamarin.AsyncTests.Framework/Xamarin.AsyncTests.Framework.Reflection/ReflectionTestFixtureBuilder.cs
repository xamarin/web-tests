//
// ReflectionTestFixtureBuilder.cs
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
using System.Xml.Linq;
using System.Reflection;
using System.Collections.Generic;

namespace Xamarin.AsyncTests.Framework.Reflection
{
	class ReflectionTestFixtureBuilder : ReflectionTestBuilder
	{
		public TypeInfo Type {
			get;
			private set;
		}

		public HeavyTestHost FixtureHost {
			get;
			private set;
		}

		List<TestBuilder> innerBuilders;
		Dictionary<string,ReflectionTestCaseBuilder> testByName;

		public ReflectionTestFixtureBuilder (TestSuite suite, AsyncTestAttribute attr, TypeInfo type)
			: base (suite, new TestName (type.Name), attr, ReflectionHelper.GetTypeInfo (type))
		{
			Type = type;
			Resolve ();
		}

		protected override IEnumerable<TestBuilder> CreateChildren ()
		{
			innerBuilders = new List<TestBuilder> ();
			testByName = new Dictionary<string,ReflectionTestCaseBuilder> ();

			foreach (var method in Type.DeclaredMethods) {
				if (method.IsStatic || !method.IsPublic)
					continue;
				var attr = method.GetCustomAttribute<AsyncTestAttribute> (true);
				if (attr == null)
					continue;

				var builder = new ReflectionTestCaseBuilder (this, attr, method);
				testByName.Add (builder.FullName, builder);
				innerBuilders.Add (builder);
				yield return builder;
			}
		}

		protected override TestHost CreateParameterHost (TestHost parent)
		{
			TestHost current = new FixtureInstanceTestHost (parent, this);

			var properties = Type.DeclaredProperties.ToArray ();
			for (int i = 0; i < properties.Length; i++) {
				var host = (ParameterizedTestHost)ReflectionHelper.ResolveParameter (current, this, properties [i]);
				if (host.Serializer == null) {
					ReflectionHelper.ResolveParameter (current, this, properties [i]);
				}
				current = new ReflectionPropertyHost (current, this, properties [i], host);
			}

			if (Attribute.Repeat != 0)
				current = CreateRepeatHost (current, Attribute.Repeat);

			return current;
		}

		protected override TestBuilderHost CreateHost (TestHost parent)
		{
			return new ReflectionTestFixtureHost (parent, this);
		}

		class ReflectionTestFixtureHost : TestBuilderHost
		{
			new public ReflectionTestFixtureBuilder Builder {
				get;
				private set;
			}

			public ReflectionTestFixtureHost (TestHost parent, ReflectionTestFixtureBuilder builder)
				: base (parent, builder)
			{
				Builder = builder;
			}

			public override TestInvoker CreateInnerInvoker ()
			{
				var innerInvokers = Builder.Children.Select (b => b.Invoker).ToArray ();
				return AggregatedTestInvoker.Create (TestFlags.ContinueOnError, innerInvokers);
			}
		}

		class FixtureInstanceTestHost : HeavyTestHost
		{
			public ReflectionTestFixtureBuilder Builder {
				get;
				private set;
			}

			public FixtureInstanceTestHost (TestHost parent, ReflectionTestFixtureBuilder builder)
				: base (parent, null)
			{
				Flags = TestFlags.ContinueOnError;
				Builder = builder;
			}

			internal override TestInstance CreateInstance (TestInstance parent)
			{
				var instance = Activator.CreateInstance (Builder.Type.AsType ());
				return new FixtureTestInstance (this, instance, parent);
			}

			internal override bool Serialize (XElement node, TestInstance instance)
			{
				return true;
			}

			internal override TestHost Deserialize (XElement node, TestHost parent)
			{
				return new FixtureInstanceTestHost (parent, Builder);
			}
		}
	}
}

