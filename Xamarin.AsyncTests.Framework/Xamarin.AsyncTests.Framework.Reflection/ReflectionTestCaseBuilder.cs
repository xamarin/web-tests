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
		bool skipThisTest;

		public ReflectionTestCaseBuilder (ReflectionTestFixtureBuilder fixture, AsyncTestAttribute attr, MethodInfo method)
			: base (TestPathType.Test, null, method.Name, GetParameter (method))
		{
			Fixture = fixture;
			Attribute = attr;
			Method = method;
			filter = ReflectionHelper.CreateTestFilter (this, fixture.Filter, ReflectionHelper.GetMethodInfo (method));
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
			if (returnType.Equals (typeof(Task)))
				return true;

			return false;
		}

		internal override bool NeedFixtureInstance => !Method.IsStatic;

		internal override bool SkipThisTest {
			get {
				if (!ResolvedTree)
					throw new InternalErrorException ();
				return skipThisTest;
			}
		}

		protected override IEnumerable<TestBuilder> CreateChildren ()
		{
			yield break;
		}

		protected override IEnumerable<TestHost> CreateParameterHosts (bool needFixtureInstance)
		{
			var list = ReflectionHelper.ResolveParameters (
				Fixture, Attribute, Method);
			if (list != null)
				return list;

			skipThisTest = true;
			return new TestHost[0];
		}

		internal override bool RunFilter (TestContext ctx, TestInstance instance)
		{
			if (SkipThisTest)
				return false;
			return Filter.Filter (ctx, instance);
		}

		internal override TestInvoker CreateInnerInvoker (TestPathTreeNode node)
		{
			if (skipThisTest)
				throw new InternalErrorException ();

			TestInvoker invoker = new ReflectionTestCaseInvoker (this);

			invoker = new PrePostRunTestInvoker (invoker);

			invoker = new ResultGroupTestInvoker (node.Path.Node.Flags | TestFlags.PathHidden, invoker);

			return invoker;
		}
	}
}

