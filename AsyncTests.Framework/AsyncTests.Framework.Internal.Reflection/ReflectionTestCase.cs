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

namespace AsyncTests.Framework.Internal.Reflection
{
	class ReflectionTestCase : TestCase
	{
		public ReflectionTestFixture Fixture {
			get;
			private set;
		}

		public AsyncTestAttribute Attribute {
			get;
			private set;
		}

		public MethodInfo Method {
			get;
			private set;
		}

		public override IEnumerable<string> Categories {
			get { return categories; }
		}

		public override IEnumerable<TestWarning> Warnings {
			get { return warnings; }
		}

		public TypeInfo ExpectedExceptionType {
			get { return expectedExceptionType; }
		}

		IList<string> categories;
		IList<TestWarning> warnings;
		ExpectedExceptionAttribute expectedException;
		TypeInfo expectedExceptionType;

		public ReflectionTestCase (ReflectionTestFixture fixture, AsyncTestAttribute attr, MethodInfo method)
			: base (method.DeclaringType.Name + "." + method.Name)
		{
			Fixture = fixture;
			Attribute = attr;
			Method = method;

			ReflectionTestFixture.Resolve (
				fixture.Suite, fixture, method, out categories, out warnings);

			expectedException = method.GetCustomAttribute<ExpectedExceptionAttribute> ();
			if (expectedException != null)
				expectedExceptionType = expectedException.ExceptionType.GetTypeInfo ();
		}

		internal override TestInvoker Resolve (TestContext context)
		{
			context.Debug (5, "RESOLVE: {0}", Name);

			TestInvoker invoker = new TestCaseInvoker (this);

			var parameterHosts = new List<TestHost> ();
			if (Attribute.Repeat != 0)
				parameterHosts.Add (new RepeatedTestHost (Attribute.Repeat));

			var parameters = Method.GetParameters ();
			for (int i = parameters.Length - 1; i >= 0; i--) {
				var paramType = parameters [i].ParameterType;
				var paramTypeInfo = paramType.GetTypeInfo ();

				if (paramType.Equals (typeof(TestContext)))
					continue;
				else if (paramType.Equals (typeof(CancellationToken)))
					continue;

				if (typeof(ITestInstance).GetTypeInfo ().IsAssignableFrom (paramTypeInfo)) {
					var hostAttr = parameters [i].GetCustomAttribute<TestHostAttribute> ();
					if (hostAttr == null)
						hostAttr = paramTypeInfo.GetCustomAttribute<TestHostAttribute> ();
					if (hostAttr == null)
						throw new InvalidOperationException ();

					parameterHosts.Add (new CustomHostAttributeTestHost (paramTypeInfo, hostAttr));
					continue;
				}

				bool found = false;
				var paramAttrs = parameters [i].GetCustomAttributes<TestParameterSourceAttribute> ();
				foreach (var paramAttr in paramAttrs) {
					parameterHosts.Add (new ParameterAttributeTestHost (paramTypeInfo, paramAttr));
					found = true;
				}

				if (found)
					continue;

				paramAttrs = paramTypeInfo.GetCustomAttributes<TestParameterSourceAttribute> ();
				foreach (var paramAttr in paramAttrs) {
					parameterHosts.Add (new ParameterAttributeTestHost (paramTypeInfo, paramAttr));
					found = true;
				}

				if (found)
					continue;

				throw new InvalidOperationException ();
			}

			foreach (var parameter in parameterHosts) {
				invoker = parameter.CreateInvoker (invoker);
			}

			context.Debug (5, "RESOLVE: {0} {1}", Name, invoker);

			return invoker;
		}

		public override Task<TestResult> Run (TestContext context, CancellationToken cancellationToken)
		{
			if (ExpectedExceptionType != null)
				return ExpectingException (context, ExpectedExceptionType, cancellationToken);
			else
				return ExpectingSuccess (context, cancellationToken);
		}

		object InvokeInner (TestContext context, CancellationToken cancellationToken)
		{
			var args = new LinkedList<object> ();

			var instance = context.Instance;
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

			if (instance != null && instance.Host is RepeatedTestHost)
				instance = instance.Parent;

			object thisInstance = null;
			if (!Method.IsStatic) {
				var fixtureInstance = instance as TestFixtureInstance;
				if (fixtureInstance == null)
					throw new InvalidOperationException ();
				thisInstance = fixtureInstance.Instance;
				instance = null;
			}

			if (instance != null)
				throw new InvalidOperationException ();

			return Method.Invoke (thisInstance, args.ToArray ());
		}

		Task<TestResult> ExpectingSuccess (TestContext context, CancellationToken cancellationToken)
		{
			object retval;
			try {
				retval = InvokeInner (context, cancellationToken);
			} catch (Exception ex) {
				return Task.FromResult<TestResult> (new TestError (ex));
			}

			var tresult = retval as Task<TestResult>;
			if (tresult != null)
				return tresult;

			var task = retval as Task;
			if (task == null)
				return Task.FromResult<TestResult> (new TestSuccess ());

			return Task.Factory.ContinueWhenAny<TestResult> (new Task[] { task }, t => {
				if (t.IsFaulted)
					return new TestError ("Test failed", t.Exception);
				else if (t.IsCanceled)
					return new TestError ("Test cancelled", t.Exception);
				else if (t.IsCompleted)
					return new TestSuccess ();
				throw new InvalidOperationException ();
			});
		}

		async Task<TestResult> ExpectingException (TestContext context, TypeInfo expectedException,
			CancellationToken cancellationToken)
		{
			try {
				var retval = InvokeInner (context, cancellationToken);
				var rtask = retval as Task<TestResult>;
				if (rtask != null) {
					var result = await rtask;
					var terror = result as TestError;
					if (terror != null)
						throw terror.Error;
				} else {
					var task = retval as Task;
					if (task != null)
						await task;
				}

				var message = string.Format ("Expected an exception of type {0}", expectedException);
				return new TestError (message, new AssertionException (message));
			} catch (Exception ex) {
				if (ex is TargetInvocationException)
					ex = ((TargetInvocationException)ex).InnerException;
				if (expectedException.IsAssignableFrom (ex.GetType ().GetTypeInfo ()))
					return new TestSuccess ();
				var message = string.Format ("Expected an exception of type {0}, but got {1}",
					expectedException, ex.GetType ());
				return new TestError (message, new AssertionException (message, ex));
			}
		}
	}
}

