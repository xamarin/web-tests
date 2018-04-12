//
// FrameworkTestFeatures.cs
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
using System.Collections.Generic;
using Xamarin.AsyncTests;
using Xamarin.AsyncTests.Constraints;
using Xamarin.AsyncTests.TestSuite;

[assembly: AsyncTestSuite (typeof (FrameworkTestFeatures), "Tests")]
[assembly: DependencyProvider (typeof (FrameworkTestFeatures.Provider))]

namespace Xamarin.AsyncTests.TestSuite
{
	using FrameworkTests;

	public class FrameworkTestFeatures : IAsyncTestAssembly, ISingletonInstance
	{
		public static FrameworkTestFeatures Instance {
			get => DependencyInjector.Get<FrameworkTestFeatures> ();
		}

		static Stack<InvocationCounters> invocationCounterStack = new Stack<InvocationCounters> ();

		public static InvocationCounters Counters => invocationCounterStack.Peek ();

		public string Name => "FrameworkTestFeatures";

		public IEnumerable<TestFeature> Features => new[] {
			IgnoreAttribute.Instance,
			TestFeature.ForkedSupport
		};

		public IEnumerable<TestCategory> Categories => new[] {
			ConditionalAttribute.Instance,
			SingleCategoryAttribute.Instance,
			SingleWithParametersAttribute.Instance
		};

		public void GlobalSetUp (TestContext ctx)
		{
			ctx.LogMessage ($"Global setup: {ctx.CurrentCategory}");
			invocationCounterStack.Push (new InvocationCounters ());
		}

		public void GlobalTearDown (TestContext ctx)
		{
			ctx.LogMessage ($"Global tear down: {ctx.CurrentCategory}");
			var counters = invocationCounterStack.Pop ();
			if (ctx.CurrentCategory == TestCategory.All)
				counters.CheckInvocationCounts (ctx);
			else if (ctx.CurrentCategory == ConditionalAttribute.Instance)
				counters.CheckConditionalInvocationCounts (ctx);
		}

		internal class Provider : IDependencyProvider
		{
			public void Initialize ()
			{
				DependencyInjector.RegisterDependency (() => new FrameworkTestFeatures ());
			}
		}
	}
}
