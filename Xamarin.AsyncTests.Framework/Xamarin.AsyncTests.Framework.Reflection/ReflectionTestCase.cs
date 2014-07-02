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

		readonly ExpectedExceptionAttribute expectedException;
		readonly TypeInfo expectedExceptionType;
		readonly TestInvoker invoker;

		public ReflectionTestCase (ReflectionTestFixture fixture, AsyncTestAttribute attr, MethodInfo method)
			: base (new TestName (method.Name), attr, ReflectionHelper.GetMethodInfo (method))
		{
			Fixture = fixture;
			Method = method;

			expectedException = method.GetCustomAttribute<ExpectedExceptionAttribute> ();
			if (expectedException != null)
				expectedExceptionType = expectedException.ExceptionType.GetTypeInfo ();

			invoker = Resolve ();
		}

		internal override bool RunFilter (TestContext ctx, out bool enabled)
		{
			if (Fixture.RunFilter (ctx, out enabled))
				return enabled;
			return base.RunFilter (ctx, out enabled);
		}

		TestInvoker Resolve ()
		{
			var parameterHosts = new List<TestHost> ();

			if (Attribute.Repeat != 0)
				parameterHosts.Add (new RepeatedTestHost (Attribute.Repeat, TestFlags.Browsable));

			var parameters = Method.GetParameters ();
			for (int i = parameters.Length - 1; i >= 0; i--) {
				var paramType = parameters [i].ParameterType;

				if (paramType.Equals (typeof(TestContext)))
					continue;
				else if (paramType.Equals (typeof(CancellationToken)))
					continue;

				var member = ReflectionHelper.GetParameterInfo (parameters [i]);
				parameterHosts.AddRange (ResolveParameter (Fixture.Type, member));
			}

			TestInvoker invoker = new ReflectionTestCaseInvoker (this);

			invoker = new ResultGroupTestInvoker (invoker);

			invoker = new PrePostRunTestInvoker (invoker);

			return CreateInvoker (invoker, parameterHosts);
		}

		public Task<bool> Invoke (
			TestContext ctx, TestInstance instance, TestResult result, CancellationToken cancellationToken)
		{
			ctx.OnTestRunning (ctx.GetCurrentTestName ());

			if (ExpectedExceptionType != null)
				return ExpectingException (ctx, instance, result, ExpectedExceptionType, cancellationToken);
			else
				return ExpectingSuccess (ctx, instance, result, cancellationToken);
		}

		object InvokeInner (
			TestContext ctx, TestInstance instance, TestResult result, CancellationToken cancellationToken)
		{
			var args = new LinkedList<object> ();

			var parameters = Method.GetParameters ();

			ctx.Debug (5, "INVOKE: {0} {1} {2}", Name, Method, instance);

			for (int index = parameters.Length - 1; index >= 0; index--) {
				var param = parameters [index];
				var paramType = param.ParameterType;

				if (paramType.Equals (typeof(CancellationToken))) {
					args.AddFirst (cancellationToken);
					continue;
				} else if (paramType.Equals (typeof(TestContext))) {
					args.AddFirst (ctx);
					continue;
				}

				var heavyInstance = instance as HeavyTestInstance;
				if (heavyInstance != null) {
					args.AddFirst (heavyInstance.Current);
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
				if (!(host is RepeatedTestHost || host is ReflectionPropertyHost))
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

			return Method.Invoke (thisInstance, args.ToArray ());
		}

		async Task<bool> ExpectingSuccess (
			TestContext ctx, TestInstance instance, TestResult result, CancellationToken cancellationToken)
		{
			object retval;
			try {
				retval = InvokeInner (ctx, instance, result, cancellationToken);
			} catch (Exception ex) {
				result.Error = ex;
				ctx.OnTestError (result);
				return false;
			}

			var task = retval as Task;
			if (task == null) {
				result.Status = TestStatus.Success;
				ctx.OnTestPassed (result);
				return true;
			}

			try {
				await task;
				result.Status = TestStatus.Success;
				ctx.OnTestPassed (result);
				return true;
			} catch (OperationCanceledException) {
				result.Status = TestStatus.Canceled;
				return false;
			} catch (Exception ex) {
				result.Error = ex;
				ctx.OnTestError (result);
				return false;
			}
		}

		async Task<bool> ExpectingException (
			TestContext ctx, TestInstance instance, TestResult result,
			TypeInfo expectedException, CancellationToken cancellationToken)
		{
			try {
				var retval = InvokeInner (ctx, instance, result, cancellationToken);
				var task = retval as Task;
				if (task != null)
					await task;

				var message = string.Format ("Expected an exception of type {0}", expectedException);
				result.Error = new AssertionException (message);
				ctx.OnTestError (result);
				return false;
			} catch (Exception ex) {
				if (ex is TargetInvocationException)
					ex = ((TargetInvocationException)ex).InnerException;
				if (expectedException.IsAssignableFrom (ex.GetType ().GetTypeInfo ())) {
					result.Status = TestStatus.Success;
					ctx.OnTestPassed (result);
					return true;
				}
				var message = string.Format ("Expected an exception of type {0}, but got {1}",
					expectedException, ex.GetType ());
				result.Error = new AssertionException (message, ex);
				ctx.OnTestError (result);
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

			public override Task<bool> Invoke (
				TestContext ctx, TestInstance instance, TestResult result, CancellationToken cancellationToken)
			{
				return Test.Invoke (ctx, instance, result, cancellationToken);
			}
		}
	}
}

