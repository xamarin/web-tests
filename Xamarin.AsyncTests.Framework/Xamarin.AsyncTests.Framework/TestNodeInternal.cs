//
// TestNodeInternal.cs
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
	sealed class TestNodeInternal : TestNode
	{
		TestHost Host {
			get;
		}

		public override TestFlags Flags {
			get;
		}

		public override string Name {
			get { return Host.Name; }
		}

		public override bool HasParameter {
			get { return Parameter != null; }
		}

		public override ITestParameter Parameter {
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

		internal TestNodeInternal (TestHost host, ITestParameter parameter = null, TestFlags? flags = null)
		{
			Host = host;
			Flags = flags ?? host.Flags;

			Parameter = host.HasFixedParameter ? host.GetFixedParameter () : parameter;
		}

		internal override TestNode Clone ()
		{
			return new TestNodeInternal (Host, Parameter, Flags);
		}

		public override TestNode Parameterize (ITestParameter parameter)
		{
			if (!IsParameterized)
				throw new InternalErrorException ();
			return new TestNodeInternal (Host, parameter, Flags);
		}

		public static bool Matches (TestNode first, TestNode second)
		{
			if (first.PathType != second.PathType)
				return false;
			if (!string.Equals (first.Identifier, second.Identifier, StringComparison.Ordinal))
				return false;
			if ((first.Name != null) != (second.Name != null))
				return false;
			if ((first.ParameterType != null) != (second.ParameterType != null))
				return false;
			if (first.ParameterType != null && !first.ParameterType.Equals (second.ParameterType))
				return false;

			return true;
		}

		public static bool Matches (TestHost host, TestNode second)
		{
			var hostNode = new TestNodeInternal (host);
			return Matches (hostNode, second); 
		}

		public override T GetParameter<T> ()
		{
			if (Parameter == null)
				return default (T);

			var typeInfo = typeof (T).GetTypeInfo ();
			if (typeInfo.IsEnum)
				return (T)Enum.Parse (typeof (T), Parameter.Value);

			var wrapper = Parameter as ITestParameterWrapper;
			if (wrapper != null)
				return (T)wrapper.Value;

			return (T)Parameter;
		}

		public override bool HasParameters {
			get { return Host.HasParameters; }
		}

		internal TestInvoker CreateInvoker (TestInvoker invoker, TestFlags flags)
		{
			return Host.CreateInvoker (this, invoker, flags);
		}
	}
}

