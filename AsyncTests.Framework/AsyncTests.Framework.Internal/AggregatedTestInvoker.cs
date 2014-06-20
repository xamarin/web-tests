//
// AggregatedTestRunner2.cs
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

namespace AsyncTests.Framework.Internal
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

		public bool ContinueOnError {
			get { return (Flags & TestFlags.ContinueOnError) != 0; }
		}

		public AggregatedTestInvoker (string name, TestFlags flags, TestHost host, params TestInvoker[] invokers)
			: base (name)
		{
			Flags = flags;
			Host = host;
			innerTestInvokers = new List<TestInvoker> ();
			innerTestInvokers.AddRange (invokers);
		}

		public IList<TestInvoker> InnerTestInvokers {
			get { return innerTestInvokers; }
		}

		List<TestInvoker> innerTestInvokers;

		static string Print (object obj)
		{
			return obj != null ? obj.ToString () : "<null>";
		}

		async Task<bool> SetUp (TestContext context, TestResultCollection result, CancellationToken cancellationToken)
		{
			context.Log ("SetUp {0}: {1} {2} {3}", Name, Print (Host), Flags, Print (context.Instance));

			try {
				if (Host != null)
					await Host.CreateInstance (context, cancellationToken);
				return true;
			} catch (Exception ex) {
				var error = new TestError (Name, "SetUp failed", ex);
				context.LogError (error);
				result.AddChild (error);
				return false;
			}
		}

		async Task<bool> ReuseInstance (TestContext context, TestResultCollection result, CancellationToken cancellationToken)
		{
			context.Log ("ReuseInstance {0}: {1} {2} {3}", Name, Print (Host), Flags, Print (context.Instance));

			try {
				var parameterizedHost = Host as ParameterizedTestHost;
				if (parameterizedHost != null)
					await parameterizedHost.ReuseInstance (context, cancellationToken);
				return true;
			} catch (Exception ex) {
				var error = new TestError (Name, "ReuseInstance failed", ex);
				context.LogError (error);
				result.AddChild (error);
				return false;
			}
		}

		async Task<bool> InvokeInner (TestContext context, TestResultCollection result, TestInvoker invoker, CancellationToken cancellationToken)
		{
			var name = string.Format ("{0} / {1}", Name, invoker.Name);
			context.Log ("Running {0}: {1} {2}", name, Print (Host), invoker);

			try {
				cancellationToken.ThrowIfCancellationRequested ();
				var inner = await invoker.Invoke (context, cancellationToken);
				result.AddChild (inner);
				return ContinueOnError || inner.Status != TestStatus.Error;
			} catch (Exception ex) {
				var error = new TestError (name, "Test failed", ex);
				context.LogError (error);
				result.AddChild (error);
				return ContinueOnError;
			}
		}

		async Task<bool> TearDown (TestContext context, TestResultCollection result, CancellationToken cancellationToken)
		{
			context.Log ("TearDown {0}: {1} {2} {3}", Name, Print (Host), Flags, Print (context.Instance));

			try {
				if (Host != null)
					await Host.DestroyInstance (context, cancellationToken);
				return true;
			} catch (Exception ex) {
				var error = new TestError (Name, "TearDown failed", ex);
				context.LogError (error);
				result.AddChild (error);
				return false;
			}
		}
			
		public sealed override async Task<TestResult> Invoke (TestContext context, CancellationToken cancellationToken)
		{
			context.Log ("THE INVOKE: {0} {1}", this, InnerTestInvokers.Count);

			if (InnerTestInvokers.Count == 0)
				return new TestSuccess (Name, true);

			var result = new TestResultCollection (Name);

			if (!await SetUp (context, result, cancellationToken)) {
				context.Log ("THE INVOKE FAILED: {0}", this);
				return result;
			}

			bool success = true;
			var innerRunners = new LinkedList<TestInvoker> (InnerTestInvokers);
			var current = innerRunners.First;

			while (success && current != null) {
				context.Log ("THE INVOKE LOOP: {0}", current.Value);

				if (cancellationToken.IsCancellationRequested)
					break;

				success = await ReuseInstance (context, result, cancellationToken);
				if (!success)
					break;

				var invoker = current.Value;
				success = await InvokeInner (context, result, invoker, cancellationToken);

				var parameterizedHost = Host as ParameterizedTestHost;
				if (parameterizedHost == null || !parameterizedHost.CanReuseInstance (context))
					current = current.Next;
			}

			success &= await TearDown (context, result, cancellationToken);

			context.Log ("THE INVOKE DONE: {0} {1}", this, result);

			cancellationToken.ThrowIfCancellationRequested ();
			return result;
		}

		public override string ToString ()
		{
			return string.Format ("[{0}: Flags={1}, Host={2}]", GetType ().Name, Flags, Host);
		}
	}
}

