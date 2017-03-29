//
// TestPathInternal.cs
//
// Author:
//       Martin Baulig <martin.baulig@xamarin.com>
//
// Copyright (c) 2015 Xamarin, Inc.
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
using System.Xml.Linq;
using System.Collections.Generic;

namespace Xamarin.AsyncTests.Framework
{
	sealed class TestPathInternal : TestPath, ITestPathInternal
	{
		public TestHost Host {
			get;
		}

		public override TestFlags Flags {
			get;
		}

		public TestPathInternal InternalParent {
			get;
		}

		public override TestPath Parent {
			get { return InternalParent; }
		}

		public override string Name {
			get { return Host.Name; }
		}

		TestPath ITestPathInternal.Path {
			get { return this; }
		}

		ITestPathInternal ITestPathInternal.Parent {
			get { return InternalParent; }
		}

		public bool HasParameter {
			get { return Parameter != null; }
		}

		public ITestParameter Parameter {
			get;
		}

		public override string ParameterValue {
			get { return Parameter?.Value; }
		}

		public override TestPathType PathType {
			get { return Host.PathType; }
		}

		public override string Identifier {
			get { return Host.Identifier; }
		}

		public override string ParameterType {
			get { return Host.ParameterType; }
		}

		internal TestPathInternal (TestHost host, TestPathInternal parent, ITestParameter parameter = null, TestFlags? flags = null)
		{
			Host = host;
			Flags = flags ?? host.Flags;
			InternalParent = parent;

			Parameter = host.HasFixedParameter ? host.GetFixedParameter () : parameter;
		}

		TestPath ITestPathInternal.GetCurrentPath ()
		{
			return Clone ();
		}

		internal TestPathInternal Clone ()
		{
			return new TestPathInternal (Host, InternalParent, Parameter);
		}

		public TestPathInternal Parameterize (ITestParameter parameter)
		{
			if (!IsParameterized)
				throw new InternalErrorException ();
			return new TestPathInternal (Host, InternalParent, parameter);
		}

		internal TestBuilder GetTestBuilder ()
		{
			var builder = Host as TestBuilderHost;
			if (builder != null)
				return builder.Builder;
			return InternalParent.GetTestBuilder ();
		}

		public static bool Matches (TestHost host, PathNode second)
		{
			if (host.PathType != second.PathType)
				return false;
			if (!string.Equals (host.Identifier, second.Identifier, StringComparison.Ordinal))
				return false;
			if ((host.Name != null) != (second.Name != null))
				return false;
			if ((host.ParameterType != null) != (second.ParameterType != null))
				return false;
			if (host.ParameterType != null && !host.ParameterType.Equals (second.ParameterType))
				return false;

			return true;
		}

		public T GetParameter<T> ()
		{
			if (Parameter == null)
				return default (T);

			var typeInfo = typeof(T).GetTypeInfo ();
			if (typeInfo.IsEnum)
				return (T)Enum.Parse (typeof (T), Parameter.Value);

			var wrapper = Parameter as ITestParameterWrapper;
			if (wrapper != null)
				return (T)wrapper.Value;

			return (T)Parameter;
		}

		internal static bool TestEquals (TestContext ctx, TestPath first, TestPath second)
		{
			var serializedFirst = first.SerializePath ().ToString ();
			var serializedSecond = second.SerializePath ().ToString ();
			if (string.Equals (serializedFirst, serializedSecond))
				return true;

			ctx.LogMessage ("NOT EQUAL:\n{0}\n{1}\n\n", serializedFirst, serializedSecond);
			return false;
		}
	}
}

