//
// AggregatedTestInvoker.cs
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
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Xamarin.AsyncTests.Framework
{
	class AggregatedTestInvoker : TestInvoker
	{
		public TestFlags Flags {
			get;
			private set;
		}

		public TestHost Host {
			get;
			private set;
		}

		public ParameterizedTestHost ParameterizedHost {
			get { return Host as ParameterizedTestHost; }
		}

		public bool ContinueOnError {
			get { return (Flags & TestFlags.ContinueOnError) != 0; }
		}

		public bool IsHidden {
			get { return (Flags & TestFlags.Hidden) != 0; }
		}

		AggregatedTestInvoker (TestFlags flags, TestHost host, IEnumerable<TestInvoker> invokers)
		{
			Flags = flags;
			Host = host;

			innerTestInvokers = new List<TestInvoker> ();
			innerTestInvokers.AddRange (invokers);
		}

		public static TestInvoker Create (TestFlags flags, params TestInvoker[] invokers)
		{
			return Create (flags, null, invokers);
		}

		public static TestInvoker Create (TestHost host, params TestInvoker[] invokers)
		{
			return Create (host.Flags, host, invokers);
		}

		static TestInvoker CreateInnerInvoker (TestHost host, TestInvoker invoker)
		{
			invoker = new CaptureContextTestInvoker (host, invoker);
			var parameterizedHost = host as ParameterizedTestHost;
			if (parameterizedHost != null)
				invoker = new ParameterizedTestInvoker (parameterizedHost, invoker);
			return invoker;
		}

		static TestInvoker Create (TestFlags flags, TestHost host, params TestInvoker[] invokers)
		{
			var innerInvokers = invokers.Select (i => CreateInnerInvoker (host, i));
			TestInvoker invoker = new AggregatedTestInvoker (flags, host, innerInvokers);
			if (host != null)
				invoker = new HostInstanceTestInvoker (host, invoker);
			if ((flags & (TestFlags.Browsable | TestFlags.FlattenHierarchy)) == TestFlags.Browsable)
				invoker = new ResultGroupTestInvoker (invoker);
			return invoker;
		}

		List<TestInvoker> innerTestInvokers;

		static string Print (object obj)
		{
			return obj != null ? obj.ToString () : "<null>";
		}

		async Task<bool> InvokeInner (
			TestContext ctx, TestInstance instance, TestResult result, TestInvoker invoker,
			CancellationToken cancellationToken)
		{
			ctx.Debug (3, "Running({0}): {1} {2}", ctx.CurrentTestName.GetFullName (), Print (Host), invoker);

			try {
				cancellationToken.ThrowIfCancellationRequested ();
				var success = await invoker.Invoke (ctx, instance, result, cancellationToken);
				return success || ContinueOnError;
			} catch (OperationCanceledException) {
				result.Status = TestStatus.Canceled;
				return false;
			} catch (Exception ex) {
				var error = ctx.CreateTestResult (ex);
				result.AddChild (error);
				return ContinueOnError;
			}
		}

		public sealed override async Task<bool> Invoke (
			TestContext ctx, TestInstance instance, TestResult result, CancellationToken cancellationToken)
		{
			if (innerTestInvokers.Count == 0)
				return true;
			if (cancellationToken.IsCancellationRequested)
				return false;

			ctx.Debug (3, "Invoke({0}): {1} {2} {3} {4}", ctx.GetCurrentTestName ().FullName,
				Print (Host), Flags, Print (instance), innerTestInvokers.Count);

			bool success = true;
			var innerRunners = new LinkedList<TestInvoker> (innerTestInvokers);
			var current = innerRunners.First;

			while (success && current != null) {
				var parameterizedInstance = instance as ParameterizedTestInstance;
				var invoker = current.Value;

				if (cancellationToken.IsCancellationRequested)
					break;

				ctx.Debug (5, "InnerInvoke({0}): {1} {2} {3} {4}", ctx.GetCurrentTestName ().FullName,
					Print (Host), Print (instance), invoker, innerTestInvokers.Count);

				success = await InvokeInner (ctx, instance, result, invoker, cancellationToken);

				ctx.Debug (5, "InnerInvoke({0}) done: {1} {2} {3}", ctx.GetCurrentTestName ().FullName,
					Print (Host), Print (instance), success);

				if (!success)
					break;

				current = current.Next;
			}

			return success;
		}

		public override string ToString ()
		{
			return string.Format ("[{0}: Flags={1}, Host={2}]", GetType ().Name, Flags, Host);
		}
	}
}

