﻿//
// FixedParameterHost.cs
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

namespace Xamarin.AsyncTests.Framework
{
	class FixedParameterHost<T> : ParameterizedTestHost
	{
		public FixedTestParameterAttribute Attribute {
			get;
		}

		internal override bool HasFixedParameter {
			get { return true; }
		}

		internal override ITestParameter GetFixedParameter ()
		{
			return fixedParameter;
		}

		ITestParameter fixedParameter;

		public FixedParameterHost (string name, TypeInfo type, IParameterSerializer serializer, FixedTestParameterAttribute attr, TestFlags flags)
			: base (name, type, serializer, flags)
		{
			Attribute = attr;

			if (Serializer != null)
				fixedParameter = Serializer.ObjectToParameter (Attribute.Value);
			else
				fixedParameter = (ITestParameter)Attribute.Value;
		}

		internal override TestInstance CreateInstance (TestContext ctx, TestNode node, TestInstance parent)
		{
			return new FixedParameterInstance<T> (this, node, parent);
		}
	}
}

