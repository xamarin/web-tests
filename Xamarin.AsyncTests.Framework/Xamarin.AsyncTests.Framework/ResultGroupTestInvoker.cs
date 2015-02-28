//
// ResultGroupTestInvoker.cs
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

namespace Xamarin.AsyncTests.Framework
{
	class ResultGroupTestInvoker : AggregatedTestInvoker
	{
		public TestInvoker Inner {
			get;
			private set;
		}

		public TestPath Path {
			get;
			private set;
		}

		public ResultGroupTestInvoker (TestPath path, TestInvoker inner)
		{
			Path = path;
			Inner = inner;
		}

		public override async Task<bool> Invoke (
			TestContext ctx, TestInstance instance, CancellationToken cancellationToken)
		{
			if ((Path.Flags & TestFlags.FlattenHierarchy) != 0)
				return await InvokeInner (ctx, instance, Inner, cancellationToken);

			var currentPath = TestInstance.GetCurrentPath (instance);

			var innerName = currentPath.Name;
			var innerResult = new TestResult (innerName);
			innerResult.Path = currentPath;

			var serialized = currentPath.SerializePath ();
			var deserialized = TestSerializer.DeserializePath (ctx, serialized);
			innerResult.Test = new PathBasedTestCase (deserialized);

			var innerCtx = ctx.CreateChild (innerResult.Name, innerResult);

			bool success;
			try {
				success = await InvokeInner (innerCtx, instance, Inner, cancellationToken);
			} finally {
				ctx.Result.AddChild (innerResult);
			}

			return success;
		}
	}
}

