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
				parameterHosts.AddRange (ResolveParameter (member));
			}

			TestInvoker invoker = new ReflectionTestCaseInvoker (this);

			foreach (var parameter in parameterHosts) {
				invoker = parameter.CreateInvoker (invoker);
			}

			return new ProxyTestInvoker (Name.Name, invoker);
		}

		public async Task<bool> Invoke (
			TestContext context, TestInstance instance, TestResult result, CancellationToken cancellationToken)
		{
			try {
				var inner = await Run (context, instance, cancellationToken);
				result.AddChild (inner);
				return inner.Status != TestStatus.Error;
			} catch (Exception ex) {
				var error = context.CreateTestResult (ex);
				result.AddChild (error);
				return false;
			}
		}

		Task<TestResult> Run (TestContext context, TestInstance instance, CancellationToken cancellationToken)
		{
			if (ExpectedExceptionType != null)
				return ExpectingException (context, instance, ExpectedExceptionType, cancellationToken);
			else
				return ExpectingSuccess (context, instance, cancellationToken);
		}

		object InvokeInner (TestContext context, TestInstance instance, CancellationToken cancellationToken)
		{
			var args = new LinkedList<object> ();

			var parameters = Method.GetParameters ();

			context.Debug (5, "INVOKE: {0} {1} {2}", Name, Method, instance);

			for (int index = parameters.Length - 1; index >= 0; index--) {
				var param = parameters [index];
				var paramType = param.ParameterType;

				if (paramType.Equals (typeof(CancellationToken))) {
					args.AddFirst (cancellationToken);
					continue;
				} else if (paramType.Equals (typeof(TestContext))) {
					args.AddFirst (context);
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

		Task<TestResult> ExpectingSuccess (TestContext context, TestInstance instance, CancellationToken cancellationToken)
		{
			object retval;
			try {
				retval = InvokeInner (context, instance, cancellationToken);
			} catch (Exception ex) {
				return Task.FromResult<TestResult> (context.CreateTestResult (ex));
			}

			var tresult = retval as Task<TestResult>;
			if (tresult != null)
				return tresult;

			var task = retval as Task;
			if (task == null)
				return Task.FromResult<TestResult> (context.CreateTestResult (TestStatus.Success));

			return Task.Factory.ContinueWhenAny<TestResult> (new Task[] { task }, t => {
				if (t.IsFaulted)
					return context.CreateTestResult (t.Exception, "Test failed");
				else if (t.IsCanceled)
					return context.CreateTestResult (t.Exception, "Test cancelled");
				else if (t.IsCompleted)
					return context.CreateTestResult (TestStatus.Success);
				throw new InvalidOperationException ();
			});
		}

		async Task<TestResult> ExpectingException (TestContext context, TestInstance instance,
			TypeInfo expectedException, CancellationToken cancellationToken)
		{
			try {
				var retval = InvokeInner (context, instance, cancellationToken);
				var rtask = retval as Task<TestResult>;
				if (rtask != null) {
					var result = await rtask;
					if (result.Error != null)
						throw result.Error;
				} else {
					var task = retval as Task;
					if (task != null)
						await task;
				}

				var message = string.Format ("Expected an exception of type {0}", expectedException);
				return context.CreateTestResult (new AssertionException (message), message);
			} catch (Exception ex) {
				if (ex is TargetInvocationException)
					ex = ((TargetInvocationException)ex).InnerException;
				if (expectedException.IsAssignableFrom (ex.GetType ().GetTypeInfo ()))
					return context.CreateTestResult (TestStatus.Success);
				var message = string.Format ("Expected an exception of type {0}, but got {1}",
					expectedException, ex.GetType ());
				return context.CreateTestResult (new AssertionException (message, ex), message);
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

