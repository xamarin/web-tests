﻿//
// ReflectionTestAssemblyInvoker.cs
//
// Author:
//       Martin Baulig <mabaul@microsoft.com>
//
// Copyright (c) 2018 Xamarin Inc. (http://www.xamarin.com)
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

namespace Xamarin.AsyncTests.Framework.Reflection
{
	class ReflectionTestAssemblyInvoker : AggregatedTestInvoker
	{
		public ReflectionTestAssemblyBuilder Builder {
			get;
		}

		public TestPathTreeNode Node {
			get;
		}

		public TestInvoker Inner {
			get;
		}

		public ReflectionTestAssemblyInvoker (
			ReflectionTestAssemblyBuilder builder, TestPathTreeNode node,
			TestFlags flags = TestFlags.ContinueOnError)
			: base (flags)
		{
			Builder = builder;
			Node = node;

			Inner = new TestCollectionInvoker (Builder, node);
		}

		public override async Task<bool> Invoke (
			TestContext ctx, TestInstance instance,
			CancellationToken cancellationToken)
		{
			if (Builder.AssemblyInstance != null &&
			    !ReflectionMethodInvoker.InvokeGlobalMethod (ctx, Builder.AssemblyInstance.GlobalSetUp))
				return false;

			try {
				return await InvokeInner (
					ctx, instance, Inner, cancellationToken).ConfigureAwait (false);
			} finally {
				if (Builder.AssemblyInstance != null)
					ReflectionMethodInvoker.InvokeGlobalMethod (ctx, Builder.AssemblyInstance.GlobalTearDown);
			}
		}
	}
}
