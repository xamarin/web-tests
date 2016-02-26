//
// ReflectionTestCaseBuilder.cs
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
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace Xamarin.AsyncTests.Framework.Reflection
{
	class ReflectionTestCaseBuilder : TestBuilder
	{
		public ReflectionTestFixtureBuilder Fixture {
			get;
			private set;
		}

		public override TestBuilder Parent {
			get { return Fixture; }
		}

		public override TestFilter Filter {
			get { return filter; }
		}

		public AsyncTestAttribute Attribute {
			get;
			private set;
		}

		public MethodInfo Method {
			get;
			private set;
		}

		public override string FullName {
			get { return Parameter.Value; }
		}

		public TypeInfo ExpectedExceptionType {
			get { return expectedExceptionType; }
		}

		TestFilter filter;
		ExpectedExceptionAttribute expectedException;
		TypeInfo expectedExceptionType;

		public ReflectionTestCaseBuilder (ReflectionTestFixtureBuilder fixture, AsyncTestAttribute attr, MethodInfo method)
			: base (TestSerializer.TestCaseIdentifier, method.Name, GetParameter (method))
		{
			Fixture = fixture;
			Attribute = attr;
			Method = method;
			filter = ReflectionHelper.CreateTestFilter (fixture.Filter, ReflectionHelper.GetMethodInfo (method));
		}

		static ITestParameter GetParameter (MethodInfo method)
		{
			var fullName = ReflectionHelper.GetMethodSignatureFullName (method);
			return TestSerializer.GetStringParameter (fullName);
		}

		protected override void ResolveMembers ()
		{
			expectedException = Method.GetCustomAttribute<ExpectedExceptionAttribute> ();
			if (expectedException != null)
				expectedExceptionType = expectedException.ExceptionType.GetTypeInfo ();

			if (!CheckReturnType ()) {
				var declaringType = ReflectionHelper.GetTypeFullName (Method.DeclaringType.GetTypeInfo ());
				var returnType = ReflectionHelper.GetTypeFullName (Method.ReturnType.GetTypeInfo ());
				throw new InternalErrorException ("Method '{0}.{1}' has invalid return type '{2}'.", declaringType, FullName, returnType);
			}

			base.ResolveMembers ();
		}

		bool CheckReturnType ()
		{
			var returnType = Method.ReturnType;
			if (returnType.Equals (typeof(void)))
				return true;
			else if (returnType.Equals (typeof(Task)))
				return true;

			return false;
		}

		protected override IEnumerable<TestBuilder> CreateChildren ()
		{
			yield break;
		}

		protected override IEnumerable<TestHost> CreateParameterHosts ()
		{
			bool seenCtx = false;
			bool seenToken = false;

			var fixedParameters = Method.GetCustomAttributes<FixedTestParameterAttribute> ();
			foreach (var fixedParameter in fixedParameters) {
				yield return ReflectionHelper.CreateFixedParameterHost (Fixture.Type, fixedParameter);
			}

			var parameters = Method.GetParameters ();
			for (int i = 0; i < parameters.Length; i++) {
				var paramType = parameters [i].ParameterType;
				var paramName = parameters [i].Name;

				var fork = parameters [i].GetCustomAttribute<ForkAttribute> ();
				if (fork != null) {
					if (!paramType.Equals (typeof(IFork)))
						throw new InternalErrorException ();
					yield return new ForkedTestHost (paramName, fork);
					continue;
				}

				if (paramType.Equals (typeof(CancellationToken))) {
					if (seenToken)
						throw new InternalErrorException ();
					seenToken = true;
					continue;
				} else if (paramType.Equals (typeof(TestContext))) {
					if (seenCtx)
						throw new InternalErrorException ();
					seenCtx = true;
					continue;
				} else if (paramType.Equals (typeof(IFork))) {
					throw new InternalErrorException ();
				}

				yield return ReflectionHelper.ResolveParameter (Fixture, parameters [i]);
			}

			if (Attribute.Repeat != 0)
				yield return ReflectionHelper.CreateRepeatHost (Attribute.Repeat);
		}

		internal override TestInvoker CreateInnerInvoker (TestPathNode node)
		{
			TestInvoker invoker = new ReflectionTestCaseInvoker (this);

			invoker = new PrePostRunTestInvoker (invoker);

			invoker = new ResultGroupTestInvoker (node.Path, invoker);

			return invoker;
		}
	}
}

