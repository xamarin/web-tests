//
// ReflectionTestCase.cs
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
using System.Reflection;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Xamarin.AsyncTests.Framework.Reflection
{
	class ReflectionTestCase : ReflectionTest
	{
		public ReflectionTestFixture Fixture {
			get;
			private set;
		}

		public MethodInfo Method {
			get;
			private set;
		}

		public TypeInfo ExpectedExceptionType {
			get { return expectedExceptionType; }
		}

		internal override TestInvoker Invoker {
			get { return invoker; }
		}

		protected override bool IsFixture {
			get { return false; }
		}

		readonly ExpectedExceptionAttribute expectedException;
		readonly TypeInfo expectedExceptionType;
		readonly TestInvoker invoker;

		public ReflectionTestCase (ReflectionTestFixture fixture, AsyncTestAttribute attr, MethodInfo method)
			: base (fixture.Suite, new TestName (method.Name), attr, ReflectionHelper.GetMethodInfo (method))
		{
			Fixture = fixture;
			Method = method;

			expectedException = method.GetCustomAttribute<ExpectedExceptionAttribute> ();
			if (expectedException != null)
				expectedExceptionType = expectedException.ExceptionType.GetTypeInfo ();

			invoker = Resolve ();
		}

		public override bool Filter (TestContext ctx, out bool enabled)
		{
			if (Fixture.Filter (ctx, out enabled))
				return enabled;
			return base.Filter (ctx, out enabled);
		}

		TestInvoker Resolve ()
		{
			var parameterHosts = new List<TestHost> ();

			if (Attribute.Repeat != 0)
				parameterHosts.Add (new RepeatedTestHost (Attribute.Repeat, TestFlags.Browsable));

			bool seenCtx = false;
			bool seenToken = false;

			var parameters = Method.GetParameters ();
			for (int i = parameters.Length - 1; i >= 0; i--) {
				var paramType = parameters [i].ParameterType;

				var fork = parameters [i].GetCustomAttribute<ForkAttribute> ();
				if (fork != null) {
					if (!paramType.Equals (typeof(IFork)))
						throw new InvalidOperationException ();
					parameterHosts.Add (new ForkedTestHost (fork));
					continue;
				}

				if (paramType.Equals (typeof(CancellationToken))) {
					if (seenToken)
						throw new InvalidOperationException ();
					seenToken = true;
					continue;
				} else if (paramType.Equals (typeof(TestContext))) {
					if (seenCtx)
						throw new InvalidOperationException ();
					seenCtx = true;
					continue;
				} else if (paramType.Equals (typeof(IFork))) {
					throw new InvalidOperationException ();
				}

				var member = ReflectionHelper.GetParameterInfo (parameters [i]);
				parameterHosts.AddRange (ResolveParameter (Fixture.Type, member));
			}

			TestInvoker invoker = new ReflectionTestCaseInvoker (this);

			invoker = new ResultGroupTestInvoker (invoker);

			invoker = new PrePostRunTestInvoker (invoker);

			return CreateInvoker (invoker, parameterHosts);
		}

		public Task<bool> Invoke (
			TestContext ctx, TestInstance instance, CancellationToken cancellationToken)
		{
			ctx.OnTestRunning ();

			if (ExpectedExceptionType != null)
				return ExpectingException (ctx, instance, ExpectedExceptionType, cancellationToken);
			else
				return ExpectingSuccess (ctx, instance, cancellationToken);
		}

		int GetTimeout ()
		{
			if (Attribute.Timeout != 0)
				return Attribute.Timeout;
			else if (Fixture.Attribute.Timeout != 0)
				return Fixture.Attribute.Timeout;
			else
				return 30000;
		}

		object InvokeInner (TestContext ctx, TestInstance instance, CancellationToken cancellationToken)
		{
			var args = new LinkedList<object> ();

			int timeout = GetTimeout ();
			CancellationTokenSource timeoutCts = null;
			CancellationTokenSource methodCts = null;
			CancellationToken methodToken;

			if (timeout > 0) {
				timeoutCts = new CancellationTokenSource (timeout);
				methodCts = CancellationTokenSource.CreateLinkedTokenSource (cancellationToken, timeoutCts.Token);
				methodToken = methodCts.Token;
			} else {
				methodToken = cancellationToken;
			}

			var parameters = Method.GetParameters ();

			ctx.LogDebug (10, "INVOKE: {0} {1} {2}", Name, Method, instance);

			for (int index = parameters.Length - 1; index >= 0; index--) {
				var param = parameters [index];
				var paramType = param.ParameterType;

				if (paramType.Equals (typeof(CancellationToken))) {
					args.AddFirst (methodToken);
					continue;
				} else if (paramType.Equals (typeof(TestContext))) {
					args.AddFirst (ctx);
					continue;
				}

				var forkedInstance = instance as ForkedTestInstance;
				if (forkedInstance != null) {
					args.AddFirst (forkedInstance);
					instance = instance.Parent;
					continue;
				}

				var heavyInstance = instance as HeavyTestInstance;
				if (heavyInstance != null) {
					args.AddFirst (heavyInstance.Current);
					instance = instance.Parent;
					continue;
				}

				var namedInstance = instance as NamedTestInstance;
				if (namedInstance != null) {
					instance = instance.Parent;
					continue;
				}

				var parameterizedInstance = instance as ParameterizedTestInstance;
				if (parameterizedInstance == null)
					throw new InvalidOperationException ();

				if (!paramType.GetTypeInfo ().IsAssignableFrom (parameterizedInstance.ParameterType))
					throw new InvalidOperationException ();

				args.AddFirst (parameterizedInstance.Current);
				instance = instance.Parent;
			}

			while (instance != null) {
				if (instance is FixtureTestInstance)
					break;
				var host = instance.Host;
				var capturedHost = host as CapturedTestHost;
				if (capturedHost != null)
					host = capturedHost.Parent;
				if (!(host is RepeatedTestHost || host is ReflectionPropertyHost || host is NamedTestHost))
					throw new InvalidOperationException ();
				instance = instance.Parent;
			}

			object thisInstance = null;
			if (!Method.IsStatic) {
				var fixtureInstance = instance as FixtureTestInstance;
				if (fixtureInstance == null)
					throw new InvalidOperationException ();
				thisInstance = fixtureInstance.Instance;
				instance = null;
			}

			if (instance != null)
				throw new InvalidOperationException ();

			try {
				return DoInvokeInner (thisInstance, args.ToArray ());
			} finally {
				if (timeoutCts != null)
					timeoutCts.Dispose ();
			}
		}

		[StackTraceEntryPoint]
		object DoInvokeInner (object instance, object[] args)
		{
			return Method.Invoke (instance, args);
		}

		bool CheckFinalStatus (TestContext ctx, bool ok)
		{
			if (ctx.HasPendingException) {
				ctx.OnTestFinished (TestStatus.Error);
				return false;
			} else if (!ok) {
				ctx.OnError (new AssertionException ("Test failed", ctx.GetStackTrace ()));
				return false;
			} else {
				ctx.OnTestFinished (TestStatus.Success);
				return true;
			}
		}

		async Task<bool> ExpectingSuccess (TestContext ctx, TestInstance instance, CancellationToken cancellationToken)
		{
			object retval;
			try {
				retval = InvokeInner (ctx, instance, cancellationToken);
			} catch (Exception ex) {
				ctx.OnError (ex);
				return false;
			}

			var task = retval as Task;
			if (task == null)
				return CheckFinalStatus (ctx, true);

			try {
				bool ok;
				var btask = retval as Task<bool>;
				if (btask != null)
					ok = await btask;
				else {
					await task;
					ok = true;
				}

				return CheckFinalStatus (ctx, ok);
			} catch (OperationCanceledException) {
				ctx.OnTestFinished (TestStatus.Canceled);
				return false;
			} catch (Exception ex) {
				ctx.OnError (ex);
				return false;
			}
		}

		async Task<bool> ExpectingException (
			TestContext ctx, TestInstance instance,
			TypeInfo expectedException, CancellationToken cancellationToken)
		{
			try {
				var retval = InvokeInner (ctx, instance, cancellationToken);
				var task = retval as Task;
				if (task != null)
					await task;

				var message = string.Format ("Expected an exception of type {0}", expectedException);
				ctx.OnError (new AssertionException (message, ctx.GetStackTrace ()));
				return false;
			} catch (Exception ex) {
				if (ex is TargetInvocationException)
					ex = ((TargetInvocationException)ex).InnerException;
				if (expectedException.IsAssignableFrom (ex.GetType ().GetTypeInfo ())) {
					ctx.OnTestFinished (TestStatus.Success);
					return true;
				}
				var message = string.Format ("Expected an exception of type {0}, but got {1}",
					expectedException, ex.GetType ());
				ctx.OnError (new AssertionException (message, ex, ctx.GetStackTrace ()));
				return false;
			}
		}

		class ReflectionTestCaseInvoker : TestInvoker
		{
			public ReflectionTestCase Test {
				get;
				private set;
			}

			public ReflectionTestCaseInvoker (ReflectionTestCase test)
			{
				Test = test;
			}

			public override Task<bool> Invoke (TestContext ctx, TestInstance instance, CancellationToken cancellationToken)
			{
				MartinTest (ctx, instance);
				return Test.Invoke (ctx, instance, cancellationToken);
			}

			static void MartinTest (TestContext ctx, TestInstance instance)
			{
				var name = TestInstance.GetTestName (instance);
				var node = TestInstance.Serialize (instance);
				ctx.LogMessage ("MARTIN TEST: {0} {1}", name, node);
				TestInstance.Deserialize (ctx, node);
			}

		}
	}
}

