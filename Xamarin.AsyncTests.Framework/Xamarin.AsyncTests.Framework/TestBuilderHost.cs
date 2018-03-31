//
// TestBuilderHost.cs
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
using System.Collections.Generic;

namespace Xamarin.AsyncTests.Framework
{
	sealed class TestBuilderHost : TestHost
	{
		public TestBuilder Builder {
			get;
		}

		public TestBuilderHost (TestBuilder builder)
			: base (builder.PathType, builder.Identifier, builder.Name, builder.Identifier, builder.Flags)
		{
			Builder = builder;
		}

		internal override ITestParameter GetParameter (TestInstance instance)
		{
			return Builder.Parameter;
		}

		internal override TestInstance CreateInstance (TestContext ctx, TestNode node, TestInstance parent)
		{
			return new TestBuilderInstance (this, node, parent);
		}

		TestInvoker CreateResultGroup (TestInvoker invoker, TestFlags flags)
		{
			if ((flags & (TestFlags.Hidden | TestFlags.FlattenHierarchy)) != 0)
				return invoker;

			return new ResultGroupTestInvoker (flags, invoker);
		}

		internal override TestInvoker CreateInvoker (TestNode node, TestInvoker invoker, TestFlags flags)
		{
			invoker = CreateResultGroup (invoker, flags);

			invoker = new TestBuilderInvoker (this, node, invoker);

			return invoker;
		}

		public override string ToString ()
		{
			return $"[TestBuilderHost: Name={Builder.Name}, Builder={DebugHelper.FormatType (Builder)}]";
		}
	}
}

