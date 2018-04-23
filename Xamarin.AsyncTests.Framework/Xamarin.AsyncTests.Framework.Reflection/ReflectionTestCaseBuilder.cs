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
	sealed class ReflectionTestCaseBuilder : ReflectionTestBuilder
	{
		public override TestFilter Filter {
			get;
		}

		public AsyncTestAttribute Attribute {
			get;
		}

		public MethodInfo Method {
			get;
		}

		public override string FullName => Parameter.Value;

		public TypeInfo ExpectedExceptionType {
			get { return expectedExceptionType; }
		}

		ExpectedExceptionAttribute expectedException;
		TypeInfo expectedExceptionType;

		public ReflectionTestCaseBuilder (
			TestBuilder parent, ReflectionMethodEntry method)
			: base (parent, TestPathType.Test, null, method.Method.Name,
			        GetParameter (method.Method), TestFlags.Browsable)
		{
			Attribute = method.Attribute;
			Method = method.Method;
			Filter = ReflectionHelper.CreateTestFilter (this, Fixture.Filter, true, method.MemberInfo);
		}

		static ITestParameter GetParameter (MethodInfo method)
		{
			var fullName = ReflectionHelper.GetMethodSignatureFullName (method);
			return TestSerializer.GetStringParameter (fullName);
		}

		protected override void ResolveMembers ()
		{
			base.ResolveMembers ();

			expectedException = Method.GetCustomAttribute<ExpectedExceptionAttribute> ();
			if (expectedException != null)
				expectedExceptionType = expectedException.ExceptionType.GetTypeInfo ();

			if (!CheckReturnType ()) {
				var declaringType = ReflectionHelper.GetTypeFullName (Method.DeclaringType.GetTypeInfo ());
				var returnType = ReflectionHelper.GetTypeFullName (Method.ReturnType.GetTypeInfo ());
				throw new InternalErrorException ($"Method '{declaringType}.{FullName}' has invalid return type '{returnType}'.");
			}

			ResolveChildren (Method, Attribute, true);
		}

		bool CheckReturnType ()
		{
			var returnType = Method.ReturnType;
			if (returnType.Equals (typeof (void)))
				return true;
			if (returnType.Equals (typeof (Task)))
				return true;

			return false;
		}
	}
}

