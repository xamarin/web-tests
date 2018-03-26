//
// ReflectionFixtureHost.cs
//
// Author:
//       Martin Baulig <mabaul@microsoft.com>
//
// Copyright (c) 2018 Xamarin Inc. (http://www.xamarin.com)
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

namespace Xamarin.AsyncTests.Framework.Reflection
{
	class ReflectionFixtureHost : HeavyTestHost
	{
		public ReflectionTestFixtureBuilder Builder {
			get;
		}

		public ConstructorInfo Constructor {
			get;
		}

		public override ITestParameter Parameter => null;

		internal IReadOnlyList<TestHost> ParameterHosts {
			get;
		}

		public ReflectionFixtureHost (
			ReflectionTestFixtureBuilder builder, ConstructorInfo ctor,
			ICollection<TestHost> parameterHosts)
			: base (TestPathType.Instance, null, TestPath.GetFriendlyName (builder.Type.AsType ()),
				builder.Type.AsType (), builder.Type.AsType (),
				TestFlags.ContinueOnError | TestFlags.Hidden | TestFlags.PathHidden)
		{
			Builder = builder;
			Constructor = ctor;
			ParameterHosts = parameterHosts?.ToArray () ?? new TestHost[0];
		}

		internal override TestInstance CreateInstance (TestNode node, TestInstance parent)
		{
			return new ReflectionFixtureInstance (this, node, parent);
		}

		internal void Unwind (ref TestInstance instance)
		{
			if (!(instance is ReflectionFixtureInstance))
				throw new InternalErrorException ();
			instance = instance.Parent;

			for (int i = 0; i < ParameterHosts.Count; i++) {
				if (instance == null)
					throw new InternalErrorException ();
				if (instance.Host != ParameterHosts[i])
					throw new InternalErrorException ();
				instance = instance.Parent;
			}
		}
	}
}
