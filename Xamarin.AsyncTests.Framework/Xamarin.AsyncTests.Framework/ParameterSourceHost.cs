//
// ParameterSourceHost.cs
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
using System.Threading;
using System.Threading.Tasks;

namespace Xamarin.AsyncTests.Framework
{
	class ParameterSourceHost<T> : ParameterizedTestHost
	{
		public Type SourceType {
			get;
			private set;
		}

		public bool UseFixtureInstance {
			get;
			private set;
		}

		public string Filter {
			get;
			private set;
		}

		public bool IsCaptured {
			get;
			private set;
		}

		public ParameterSourceHost (
			TestHost parent, string name, Type sourceType, bool useFixtureInstance,
			IParameterSerializer serializer, string filter, TestFlags flags = TestFlags.None)
			: base (parent, name, typeof (T).GetTypeInfo (), serializer, flags)
		{
			SourceType = sourceType;
			UseFixtureInstance = useFixtureInstance;
			Filter = filter;
		}

		internal override bool Serialize (XElement node, TestInstance instance)
		{
			if (Serializer != null)
				return base.Serialize (node, instance);

			var parameterizedInstance = (ParameterizedTestInstance)instance;
			var testParameter = parameterizedInstance.Current as ITestParameter;
			if (testParameter == null)
				return false;

			node.Add (new XAttribute ("Identifier", testParameter.Identifier));
			return true;
		}

		internal override TestHost Deserialize (XElement node, TestHost parent)
		{
			if (Serializer != null && node != null)
				return base.Deserialize (node, parent);

			string identifier;
			if (node != null) {
				var attr = node.Attribute ("Identifier");
				if (attr == null)
					return null;

				identifier = attr.Value;
			} else {
				identifier = Filter;
			}

			var host = new ParameterSourceHost<T> (
				parent, ParameterName, SourceType, UseFixtureInstance, null,
				identifier, Flags);

			if (node != null)
				host.IsCaptured = true;

			return host;
		}

		internal override TestInstance CreateInstance (TestInstance parent)
		{
			return new ParameterSourceInstance<T> (this, parent, SourceType, UseFixtureInstance, Filter);
		}
	}
}

