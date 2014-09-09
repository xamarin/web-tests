//
// ReflectionTestBuilder.cs
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
using System.Threading;
using System.Threading.Tasks;
using System.Reflection;
using System.Collections.Generic;

namespace Xamarin.AsyncTests.Framework.Reflection
{
	abstract class ReflectionTestBuilder : TestBuilder
	{
		public IMemberInfo Member {
			get;
			private set;
		}

		public AsyncTestAttribute Attribute {
			get;
			private set;
		}

		protected ReflectionTestBuilder (TestSuite suite, TestName name, AsyncTestAttribute attr, IMemberInfo member)
			: base (suite, name)
		{
			Attribute = attr;
			Member = member;
		}

		protected override TestFilter GetTestFilter ()
		{
			TestFilter parent = null;
			if (Parent != null)
				parent = Parent.Filter;

			var categories = ReflectionHelper.GetCategories (Member);
			var features = ReflectionHelper.GetFeatures (Member);

			return new TestFilter (parent, categories, features);
		}

		protected override TestCase CreateTestCase ()
		{
			return new ReflectionTestCase (this);
		}

		protected static TestHost CreateRepeatHost (TestHost parent, int repeat)
		{
			return new RepeatedTestHost (null, repeat, TestFlags.Browsable);
		}

		class ReflectionTestCase : TestCase
		{
			public ReflectionTestBuilder Builder {
				get;
				private set;
			}

			public ReflectionTestCase (ReflectionTestBuilder builder)
				: base (builder.Suite, builder.Name)
			{
				Builder = builder;
			}

			internal override Task<bool> Run (TestContext ctx, CancellationToken cancellationToken)
			{
				return Builder.Invoker.Invoke (ctx, null, cancellationToken);
			}
		}
	}
}

