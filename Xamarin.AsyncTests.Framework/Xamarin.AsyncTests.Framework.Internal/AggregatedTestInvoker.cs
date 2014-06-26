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

namespace Xamarin.AsyncTests.Framework.Internal
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

		public bool IsBrowsable {
			get { return (Flags & (TestFlags.Browsable | TestFlags.FlattenHierarchy)) == TestFlags.Browsable; }
		}

		public AggregatedTestInvoker (TestFlags flags, params TestInvoker[] invokers)
			: this (flags, null, invokers)
		{
		}

		public AggregatedTestInvoker (TestHost host, params TestInvoker[] invokers)
			: this (host.Flags, host, invokers)
		{
		}

		AggregatedTestInvoker (TestFlags flags, TestHost host, TestInvoker[] invokers)
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

		async Task<bool> SetUp (TestContext context, TestResult result, CancellationToken cancellationToken)
		{
			context.Debug (3, "SetUp({0}): {1} {2} {3}", context.GetCurrentTestName ().FullName,
				Print (Host), Flags, Print (context.Instance));

			if (Host == null)
				return true;

			try {
				context.CurrentTestName.PushName ("SetUp");
				await Host.CreateInstance (context, cancellationToken);
				return true;
			} catch (Exception ex) {
				var error = context.CreateTestResult (ex);
				result.AddChild (error);
				return false;
			} finally {
				context.CurrentTestName.PopName ();
			}
		}

		async Task<bool> ReuseInstance (TestContext context, TestResult result, CancellationToken cancellationToken)
		{
			context.Debug (3, "ReuseInstance({0}): {1} {2} {3}", context.GetCurrentTestName ().FullName,
				Print (Host), Flags, Print (context.Instance));

			try {
				context.CurrentTestName.PushName ("ReuseInstance");
				for (var instance = context.Instance; instance != null; instance = instance.Parent) {
					await instance.ReuseInstance (context, cancellationToken);
				}
				return true;
			} catch (Exception ex) {
				var error = context.CreateTestResult (ex);
				result.AddChild (error);
				return false;
			} finally {
				context.CurrentTestName.PopName ();
			}
		}

		async Task<bool> MoveNext (TestContext context, TestResult result, CancellationToken cancellationToken)
		{
			context.Debug (3, "MoveNext({0}): {1} {2} {3}", context.GetCurrentTestName ().FullName,
				Print (Host), Flags, Print (context.Instance));

			if (ParameterizedHost == null)
				return true;

			try {
				context.CurrentTestName.PushName ("MoveNext");
				await ParameterizedHost.MoveNext (context, cancellationToken);
				return true;
			} catch (Exception ex) {
				var error = context.CreateTestResult (ex);
				result.AddChild (error);
				return false;
			} finally {
				context.CurrentTestName.PopName ();
			}
		}


		async Task<bool> InvokeInner (TestContext context, TestResult result, TestInvoker invoker, CancellationToken cancellationToken)
		{
			context.Debug (3, "Running({0}): {1} {2}", context.CurrentTestName.GetFullName (), Print (Host), invoker);

			try {
				cancellationToken.ThrowIfCancellationRequested ();
				var success = await invoker.Invoke (context, result, cancellationToken);
				return success || ContinueOnError;
			} catch (Exception ex) {
				var error = context.CreateTestResult (ex);
				result.AddChild (error);
				return ContinueOnError;
			}
		}

		async Task<bool> TearDown (TestContext context, TestResult result, CancellationToken cancellationToken)
		{
			context.Debug (3, "TearDown({0}): {1} {2} {3}", context.GetCurrentTestName ().FullName,
				Print (Host), Flags, Print (context.Instance));

			if (Host == null)
				return true;

			try {
				context.CurrentTestName.PushName ("TearDown");
				await Host.DestroyInstance (context, cancellationToken);
				return true;
			} catch (Exception ex) {
				var error = context.CreateTestResult (ex);
				result.AddChild (error);
				return false;
			} finally {
				context.CurrentTestName.PopName ();
			}
		}
			
		public sealed override async Task<bool> Invoke (
			TestContext context, TestResult result, CancellationToken cancellationToken)
		{
			if (InnerTestInvokers.Count == 0)
				return true;

			context.Debug (3, "Invoke({0}): {1} {2} {3}", context.GetCurrentTestName ().FullName,
				Print (Host), Flags, Print (context.Instance));

			var oldResult = context.CurrentResult;

			var innerResult = result;
			if (IsBrowsable) {
				innerResult = new TestResult (context.GetCurrentTestName ());
				result.AddChild (innerResult);
				context.CurrentResult = innerResult;
			}

			if (!await SetUp (context, innerResult, cancellationToken)) {
				context.CurrentResult = oldResult;
				return false;
			}

			bool success = true;
			var innerRunners = new LinkedList<TestInvoker> (InnerTestInvokers);
			var current = innerRunners.First;

			while (success && current != null) {
				var invoker = current.Value;

				if (cancellationToken.IsCancellationRequested)
					break;

				success = await MoveNext (context, innerResult, cancellationToken);
				if (!success)
					break;

				if (ParameterizedHost != null) {
					var parameterizedInstance = (ParameterizedTestInstance)context.Instance;
					context.CurrentTestName.PushParameter (ParameterizedHost.ParameterName, parameterizedInstance.Current);
				}
				var capturedTest = CaptureContext (context, invoker);
				if (capturedTest != null)
					context.CurrentTestName.PushCapture (capturedTest);

				context.Debug (5, "InnerInvoke({0}): {1} {2}", context.GetCurrentTestName ().FullName,
					IsBrowsable, Host != null);

				success = await InvokeInner (context, innerResult, invoker, cancellationToken);

				context.Debug (5, "InnerInvoke({0}) done: {1} {2} {3}", context.GetCurrentTestName ().FullName,
					IsBrowsable, Host != null, success);

				if (capturedTest != null)
					context.CurrentTestName.PopCapture ();
				if (ParameterizedHost != null)
					context.CurrentTestName.PopParameter ();

				if (!success)
					break;

				if (ParameterizedHost == null || !ParameterizedHost.HasNext (context))
					current = current.Next;
			}

			if (!await TearDown (context, innerResult, cancellationToken))
				success = false;

			context.CurrentResult = oldResult;

			cancellationToken.ThrowIfCancellationRequested ();
			return success;
		}

		TestCase CaptureContext (TestContext context, TestInvoker invoker)
		{
			if (context.CurrentTestName.IsCaptured || Host is CapturedTestHost || context.Instance == null)
				return null;

			var capture = CaptureContext (context.GetCurrentTestName (), context.Instance, invoker);
			if (capture == null)
				return null;

			return new CapturedTestCase (new CapturedTestInvoker (context.GetCurrentTestName (), capture));
		}

		static TestInvoker CaptureContext (TestName name, TestInstance instance, TestInvoker invoker)
		{
			if (instance.Parent != null)
				invoker = CaptureContext (name, instance.Parent, invoker);

			var parameterizedInstance = instance as ParameterizedTestInstance;
			if (parameterizedInstance != null) {
				var host = new CapturedTestHost (name, parameterizedInstance.Host, parameterizedInstance.Current);
				return new AggregatedTestInvoker (host, invoker);
			}

			return new AggregatedTestInvoker (instance.Host, invoker);
		}

		public override string ToString ()
		{
			return string.Format ("[{0}: Flags={1}, Host={2}]", GetType ().Name, Flags, Host);
		}
	}
}

