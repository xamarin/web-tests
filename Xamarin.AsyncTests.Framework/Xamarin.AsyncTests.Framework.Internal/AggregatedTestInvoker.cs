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

		async Task<TestInstance> SetUp (
			TestContext ctx, TestInstance instance, TestResult result, CancellationToken cancellationToken)
		{
			ctx.Debug (3, "SetUp({0}): {1} {2} {3}", ctx.GetCurrentTestName ().FullName,
				Print (Host), Flags, Print (instance));

			if (Host == null)
				return instance;

			try {
				ctx.CurrentTestName.PushName ("SetUp");
				return await Host.CreateInstance (ctx, instance, cancellationToken);
			} catch (Exception ex) {
				var error = ctx.CreateTestResult (ex);
				result.AddChild (error);
				return null;
			} finally {
				ctx.CurrentTestName.PopName ();
			}
		}

		async Task<bool> ReuseInstance (
			TestContext ctx, TestInstance instance, TestResult result, CancellationToken cancellationToken)
		{
			ctx.Debug (3, "ReuseInstance({0}): {1} {2} {3}", ctx.GetCurrentTestName ().FullName,
				Print (Host), Flags, Print (instance));

			try {
				ctx.CurrentTestName.PushName ("ReuseInstance");
				for (var current = instance; current != null; current = current.Parent) {
					await current.ReuseInstance (ctx, cancellationToken);
				}
				return true;
			} catch (Exception ex) {
				var error = ctx.CreateTestResult (ex);
				result.AddChild (error);
				return false;
			} finally {
				ctx.CurrentTestName.PopName ();
			}
		}

		async Task<bool> MoveNext (
			TestContext ctx, TestInstance instance, TestResult result, CancellationToken cancellationToken)
		{
			ctx.Debug (3, "MoveNext({0}): {1} {2} {3}", ctx.GetCurrentTestName ().FullName,
				Print (Host), Flags, Print (instance));

			var parameterizedInstance = instance as ParameterizedTestInstance;
			if (parameterizedInstance == null)
				return true;

			try {
				ctx.CurrentTestName.PushName ("MoveNext");
				await parameterizedInstance.MoveNext (ctx, cancellationToken);
				return true;
			} catch (Exception ex) {
				var error = ctx.CreateTestResult (ex);
				result.AddChild (error);
				return false;
			} finally {
				ctx.CurrentTestName.PopName ();
			}
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
			} catch (Exception ex) {
				var error = ctx.CreateTestResult (ex);
				result.AddChild (error);
				return ContinueOnError;
			}
		}

		async Task<bool> TearDown (
			TestContext ctx, TestInstance instance, TestResult result, CancellationToken cancellationToken)
		{
			ctx.Debug (3, "TearDown({0}): {1} {2} {3}", ctx.GetCurrentTestName ().FullName,
				Print (Host), Flags, Print (instance));

			try {
				ctx.CurrentTestName.PushName ("TearDown");
				await instance.Destroy (ctx, cancellationToken);
				return true;
			} catch (Exception ex) {
				var error = ctx.CreateTestResult (ex);
				result.AddChild (error);
				return false;
			} finally {
				ctx.CurrentTestName.PopName ();
			}
		}
			
		public sealed override async Task<bool> Invoke (
			TestContext ctx, TestInstance instance, TestResult result, CancellationToken cancellationToken)
		{
			if (InnerTestInvokers.Count == 0)
				return true;

			ctx.Debug (3, "Invoke({0}): {1} {2} {3} {4}", ctx.GetCurrentTestName ().FullName,
				Print (Host), Flags, Print (instance), InnerTestInvokers.Count);

			var oldResult = ctx.CurrentResult;

			var innerResult = result;
			if (IsBrowsable) {
				innerResult = new TestResult (ctx.GetCurrentTestName ());
				result.AddChild (innerResult);
				ctx.CurrentResult = innerResult;
			}

			var innerInstance = instance;
			if (Host != null) {
				innerInstance = await SetUp (ctx, instance, innerResult, cancellationToken);
				if (innerInstance == null) {
					ctx.CurrentResult = oldResult;
					return false;
				}
			}

			bool success = true;
			var innerRunners = new LinkedList<TestInvoker> (InnerTestInvokers);
			var current = innerRunners.First;

			while (success && current != null) {
				var invoker = current.Value;

				if (cancellationToken.IsCancellationRequested)
					break;

				success = await MoveNext (ctx, innerInstance, innerResult, cancellationToken);
				if (!success)
					break;

				var parameterizedInstance = innerInstance as ParameterizedTestInstance;
				if (ParameterizedHost != null)
					ctx.CurrentTestName.PushParameter (ParameterizedHost.ParameterName, parameterizedInstance.Current);
				var capturedTest = CaptureContext (ctx, innerInstance, invoker);
				if (capturedTest != null)
					ctx.CurrentTestName.PushCapture (capturedTest);

				ctx.Debug (5, "InnerInvoke({0}): {1} {2} {3} {4}", ctx.GetCurrentTestName ().FullName,
					Print (Host), Print (innerInstance), invoker, InnerTestInvokers.Count);

				success = await InvokeInner (ctx, innerInstance, innerResult, invoker, cancellationToken);

				ctx.Debug (5, "InnerInvoke({0}) done: {1} {2} {3} {4}", ctx.GetCurrentTestName ().FullName,
					IsBrowsable, Print (Host), Print (innerInstance), success);

				if (capturedTest != null)
					ctx.CurrentTestName.PopCapture ();
				if (ParameterizedHost != null)
					ctx.CurrentTestName.PopParameter ();

				if (!success)
					break;

				if (parameterizedInstance == null || !parameterizedInstance.HasNext ())
					current = current.Next;
			}

			if (Host != null) {
				if (!await TearDown (ctx, innerInstance, innerResult, cancellationToken))
					success = false;
			}

			ctx.CurrentResult = oldResult;

			cancellationToken.ThrowIfCancellationRequested ();
			return success;
		}

		TestCase CaptureContext (TestContext context, TestInstance instance, TestInvoker invoker)
		{
			if (context.CurrentTestName.IsCaptured || Host is CapturedTestHost || instance == null)
				return null;

			var capture = CaptureContext (context.GetCurrentTestName (), instance, invoker);
			context.Debug (5, "CaptureContext({0}): {1} {2} {3} -> {4}", context.GetCurrentTestName (),
				Print (instance), Print (Host), invoker, Print (capture));
			if (capture == null)
				return null;

			return new CapturedTestCase (new CapturedTestInvoker (context.GetCurrentTestName (), capture));
		}

		static TestInvoker CaptureContext (TestName name, TestInstance instance, TestInvoker invoker)
		{
			var parameterizedInstance = instance as ParameterizedTestInstance;
			if (parameterizedInstance != null) {
				var host = new CapturedTestHost (name, parameterizedInstance.Host, parameterizedInstance.Current);
				invoker = new AggregatedTestInvoker (host, invoker);
			}

			if (instance.Parent != null)
				return CaptureContext (name, instance.Parent, invoker);

			return new AggregatedTestInvoker (instance.Host, invoker);
		}

		public override string ToString ()
		{
			return string.Format ("[{0}: Flags={1}, Host={2}]", GetType ().Name, Flags, Host);
		}
	}
}

