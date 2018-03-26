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
	class ReflectionTestFixtureBuilder : TestBuilder
	{
		public ReflectionTestAssemblyBuilder AssemblyBuilder {
			get;
		}

		public TypeInfo Type {
			get;
		}

		public HeavyTestHost FixtureHost {
			get;
		}

		public AsyncTestFixtureAttribute Attribute {
			get;
		}

		public override TestBuilder Parent {
			get { return AssemblyBuilder; }
		}

		TestFilter filter;

		public ReflectionTestFixtureBuilder (ReflectionTestAssemblyBuilder assembly, AsyncTestFixtureAttribute attr, TypeInfo type)
			: base (TestPathType.Fixture, null,
			        attr.Prefix != null ? attr.Prefix + "." + type.Name : type.Name,
			        TestSerializer.GetStringParameter (type.FullName))
		{
			AssemblyBuilder = assembly;
			Type = type;
			Attribute = attr;

			filter = ReflectionHelper.CreateTestFilter (this, null, ReflectionHelper.GetTypeInfo (type));
		}

		public override TestFilter Filter {
			get { return filter; }
		}

		protected override IEnumerable<TestBuilder> CreateChildren ()
		{
			var seenAnyStatic = false;
			var seenAnyInstance = false;

			var list = new List<TestBuilder> ();
			ResolveMembers (Type);
			return list;

			void ResolveMembers (TypeInfo type)
			{
				foreach (var method in type.DeclaredMethods) {
					ResolveMethod (method);
				}

				var baseType = type.BaseType?.GetTypeInfo ();
				if (baseType != null)
					ResolveMembers (baseType);
			}

			void ResolveMethod (MethodInfo method)
			{
				if (!method.IsPublic || method.IsAbstract)
					return;
				if (Type.IsAbstract && !method.IsStatic)
					return;
				var attr = method.GetCustomAttribute<AsyncTestAttribute> (true);
				if (attr == null)
					return;

				if (method.IsStatic) {
					if (seenAnyInstance)
						CannotMixStaticAndInstance ();
					seenAnyStatic = true;
				} else {
					if (seenAnyStatic)
						CannotMixStaticAndInstance ();
					seenAnyInstance = true;
				}

				list.Add (new ReflectionTestCaseBuilder (this, attr, method));
			}

			void CannotMixStaticAndInstance ()
			{
				throw new InternalErrorException (
					$"Cannot mix static and instance methods in fixture `{Type}'.");
			}
		}

		protected override IEnumerable<TestHost> CreateParameterHosts (bool needFixtureInstance)
		{
			var list = new List<TestHost> ();
			var fixedParameters = Type.GetCustomAttributes<FixedTestParameterAttribute> ();
			foreach (var fixedParameter in fixedParameters) {
				list.Add (ReflectionHelper.CreateFixedParameterHost (Type, fixedParameter));
			}

			if (needFixtureInstance)
				list.AddRange (ReflectionHelper.ResolveFixtureParameterHosts (this));

			return list;
		}

		internal override bool NeedFixtureInstance => true;

		internal override bool SkipThisTest {
			get {
				if (!ResolvedTree)
					throw new InternalErrorException ();
				return Children.Count == 0;
			}
		}

		internal override bool RunFilter (TestContext ctx, TestInstance instance)
		{
			if (SkipThisTest || Children.Count == 0)
				return false;
			if (!Filter.Filter (ctx, instance))
				return false;
			return Children.Any (c => c.RunFilter (ctx, instance));
		}

		internal override TestInvoker CreateInnerInvoker (TestPathTreeNode node)
		{
			return new TestCollectionInvoker (this, node);
		}
	}
}

