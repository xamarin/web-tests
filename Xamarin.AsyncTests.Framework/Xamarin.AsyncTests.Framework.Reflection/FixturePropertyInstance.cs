//
// FixturePropertyInstance.cs
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
namespace Xamarin.AsyncTests.Framework.Reflection
{
	class FixturePropertyInstance : ParameterizedTestInstance
	{
		new public FixturePropertyHost Host => (FixturePropertyHost)base.Host;

		bool hasNext;
		ParameterizedTestValue current;

		public FixturePropertyInstance (
			FixturePropertyHost host, TestNode node,
			TestInstance parent)
			: base (host, node, parent)
		{
		}

		public override void Initialize (TestContext ctx)
		{
			hasNext = true;
			current = null;
		}

		public override bool HasNext ()
		{
			return hasNext;
		}

		public override bool MoveNext (TestContext ctx)
		{
			if (!hasNext)
				return false;
			hasNext = false;

			var fixtureInstance = GetFixtureInstance ();
			var value = Host.Property.GetValue (fixtureInstance.Instance);
			var serialized = Host.Serializer.ObjectToParameter (value);

			current = new FixturePropertyValue (this, serialized, value);

			return true;
		}

		public override ParameterizedTestValue Current {
			get { return current; }
		}

		sealed class FixturePropertyValue : ParameterizedTestValue
		{
			public FixturePropertyValue (
				FixturePropertyInstance instance,
				ITestParameter parameter, object value)
				: base (instance, value)
			{
				Parameter = parameter;
			}

			public override ITestParameter Parameter {
				get;
			}
		}
	}
}
