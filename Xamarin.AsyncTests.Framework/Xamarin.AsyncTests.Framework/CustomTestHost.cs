//
// CustomTestHost.cs
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
using System.Xml.Linq;
using System.Reflection;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Xamarin.AsyncTests.Framework
{
	class CustomTestHost : HeavyTestHost, ITestParameter
	{
		public Type HostType {
			get;
		}

		public bool UseFixtureInstance {
			get;
		}

		public override ITestParameter Parameter {
			get { return this; }
		}

		public string Value {
			get { return TestPath.GetFriendlyName (Type); }
		}

		public string FriendlyValue => Value;

		public TestHostAttribute Attribute {
			get;
		}

		public ITestHost<ITestInstance> StaticHost {
			get;
		}

		public CustomTestHost (string name, Type type, Type hostType, TestFlags flags,
		                       TestHostAttribute attr, ITestHost<ITestInstance> staticHost, bool useFixtureInstance)
			: base (TestPathType.Parameter, name, name, type, hostType, flags)
		{
			HostType = hostType;
			Attribute = attr;
			StaticHost = staticHost;
			UseFixtureInstance = useFixtureInstance;
		}

		internal override TestInstance CreateInstance (TestNode node, TestInstance parent)
		{
			return new CustomTestInstance (this, node, parent);
		}

		public override string ToString ()
		{
			var hostType = HostType?.Name ?? "<null>";
			return $"[CustomTestHost({DebugHelper.FormatType (this)}): HostType={hostType}, UseFixtureInstance={UseFixtureInstance}]";
		}
	}
}

