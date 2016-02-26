//
// ReflectionTestSuite.cs
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
using System.Threading;
using System.Threading.Tasks;

namespace Xamarin.AsyncTests.Framework.Reflection
{
	class ReflectionTestSuite : TestSuite, IPathResolver
	{
		public TestFramework Framework {
			get;
			private set;
		}

		public List<ReflectionTestAssembly> Assemblies {
			get;
			private set;
		}

		public ReflectionTestSuiteBuilder Builder {
			get;
			private set;
		}

		public TestPathNode RootPath {
			get;
			private set;
		}

		public ReflectionTestCase RootTestCase {
			get;
			private set;
		}

		internal ReflectionTestSuite (ReflectionTestFramework framework)
		{
			Framework = framework;
			Assemblies = framework.Assemblies;
			Builder = new ReflectionTestSuiteBuilder (this);

			var rootPath = new TestPath (Builder.Host, null, Builder.Parameter);
			RootPath = new TestPathNode (Builder.TreeRoot, rootPath);
			RootTestCase = new ReflectionTestCase (RootPath);
		}

		TestCase TestSuite.RootTestCase {
			get { return RootTestCase; }
		}

		TestPathNode IPathResolver.Node {
			get { return RootPath; }
		}

		IPathResolver IPathResolver.Resolve (TestContext ctx, IPathNode node, string parameter)
		{
			if (!node.Identifier.Equals (TestSerializer.TestSuiteIdentifier))
				throw new InternalErrorException ();
			if (!node.ParameterType.Equals (TestSerializer.TestSuiteIdentifier))
				throw new InternalErrorException ();
			return RootPath;
		}


	}
}

