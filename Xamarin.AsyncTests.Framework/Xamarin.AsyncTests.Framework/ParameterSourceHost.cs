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

		public ParameterSourceHost (
			string name, Type sourceType, bool useFixtureInstance,
			IParameterSerializer serializer, string filter, TestFlags flags = TestFlags.None)
			: base (name, typeof (T).GetTypeInfo (), serializer, flags)
		{
			SourceType = sourceType;
			UseFixtureInstance = useFixtureInstance;
			Filter = filter;
		}

		internal override bool Serialize (XElement node, TestInstance instance)
		{
			var parameterizedInstance = (ParameterizedTestInstance)instance;

			if (Serializer != null)
				return Serializer.Serialize (node, parameterizedInstance.Current);

			var testParameter = parameterizedInstance.Current as ITestParameter;
			if (testParameter == null)
				return false;

			node.Add (new XAttribute ("Identifier", testParameter.Identifier));
			return true;
		}

		internal override TestInvoker Deserialize (XElement node, TestInvoker invoker)
		{
			if (Serializer != null) {
				var value = Serializer.Deserialize (node);
				if (value == null)
					throw new InternalErrorException ();

				return new CapturedInvoker (this, value, invoker);
			}

			var attr = node.Attribute ("Identifier");
			if (attr == null)
				throw new InternalErrorException ();

			return new CapturedInvoker (this, attr.Value, invoker);
		}

		internal override TestInstance CreateInstance (TestInstance parent)
		{
			return new ParameterSourceInstance<T> (this, parent, SourceType, UseFixtureInstance, Filter);
		}

		class CapturedInvoker : ParameterizedTestInvoker
		{
			public object CapturedValue {
				get;
				private set;
			}

			public string CapturedIdentifier {
				get;
				private set;
			}

			new public ParameterSourceHost<T> Host {
				get { return (ParameterSourceHost<T>)base.Host; }
			}

			public CapturedInvoker (ParameterSourceHost<T> host, object captured, TestInvoker inner)
				: base (host, inner)
			{
				CapturedValue = captured;
			}

			public CapturedInvoker (ParameterSourceHost<T> host, string identifier, TestInvoker inner)
				: base (host, inner)
			{
				CapturedIdentifier = identifier;
			}

			protected override ParameterizedTestInstance CreateInstance (TestInstance parent)
			{
				var instance = new ParameterSourceInstance<T> (
					Host, parent, Host.SourceType, Host.UseFixtureInstance, Host.Filter);
				instance.CapturedValue = CapturedValue;
				instance.CapturedIdentifier = CapturedIdentifier;
				return instance;
			}
		}
	}
}

