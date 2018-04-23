﻿//
// ReflectionTestAssemblyBuilder.cs
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
using System.Reflection;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Xamarin.AsyncTests.Framework.Reflection
{
	class ReflectionTestAssemblyBuilder : TestBuilder
	{
		public ReflectionTestSuiteBuilder SuiteBuilder {
			get;
			private set;
		}

		public ReflectionTestAssembly Assembly {
			get;
			private set;
		}

		public override TestBuilder Parent {
			get { return SuiteBuilder; }
		}

		public ReflectionTestAssemblyBuilder (ReflectionTestSuiteBuilder suite, ReflectionTestAssembly assembly)
			: base (TestPathType.Assembly, null, assembly.Name,
			        TestSerializer.GetStringParameter (assembly.Assembly.FullName))
		{
			SuiteBuilder = suite;
			Assembly = assembly;
		}

		public override TestFilter Filter {
			get { return null; }
		}

		public IAsyncTestAssembly AssemblyInstance {
			get;
			private set;
		}

		protected override bool SkipThisTest => false;

		protected override IList<TestBuilder> CreateChildren ()
		{
			var instance = DependencyInjector.Get (Assembly.Attribute.Type);
			AssemblyInstance = instance as IAsyncTestAssembly;

			var seenTypes = new Dictionary<TypeInfo, TestBuilder> ();

			var children = new List<TestBuilder> ();

			foreach (var type in Assembly.Assembly.ExportedTypes) {
				var tinfo = type.GetTypeInfo ();
				var attr = ResolveType (tinfo);
				if (attr == null)
					continue;

				children.Add (new ReflectionTestFixtureBuilder (this, attr, tinfo));
			}

			return children;

			AsyncTestFixtureAttribute ResolveType (TypeInfo type)
			{
				var attr = type.GetCustomAttribute<AsyncTestFixtureAttribute> (true);
				if (attr != null)
					return attr;

				var baseType = type.BaseType?.GetTypeInfo ();
				if (baseType != null)
					return ResolveType (baseType);

				return null;
			}
		}

		internal override TestInvoker CreateInnerInvoker (TestPathTreeNode node)
		{
			return new ReflectionTestAssemblyInvoker (this, node);
		}
	}
}

