//
// ReflectionPropertyHost.cs
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

namespace Xamarin.AsyncTests.Framework.Reflection
{
	class ReflectionPropertyHost : ParameterizedTestHost
	{
		public ReflectionTestFixtureBuilder Fixture {
			get;
			private set;
		}

		public PropertyInfo Property {
			get;
			private set;
		}

		public ParameterizedTestHost Host {
			get;
			private set;
		}

		public ReflectionPropertyHost (ReflectionTestFixtureBuilder fixture,
			PropertyInfo prop, ParameterizedTestHost host)
			: base (prop.Name, prop.PropertyType.GetTypeInfo (), host.Serializer, host.Flags)
		{
			Fixture = fixture;
			Property = prop;
			Host = host;
		}

		internal override TestInvoker Deserialize (XElement node, TestInvoker invoker)
		{
			if (Serializer == null)
				return null;

			var value = Serializer.Deserialize (node);
			if (value == null)
				throw new InternalErrorException ();

			return new CapturedInvoker (this, value, invoker);
		}

		internal override TestInstance CreateInstance (TestInstance parent)
		{
			var instance = (ParameterizedTestInstance)Host.CreateInstance (parent);
			return new ReflectionPropertyInstance (this, instance, parent);
		}

		class CapturedInvoker : ParameterizedTestInvoker
		{
			public object Captured {
				get;
				private set;
			}

			new public ReflectionPropertyHost Host {
				get { return (ReflectionPropertyHost)base.Host; }
			}

			public CapturedInvoker (ReflectionPropertyHost host, object captured, TestInvoker inner)
				: base (host, inner)
			{
				Captured = captured;
			}

			protected override ParameterizedTestInstance CreateInstance (TestInstance parent)
			{
				return new ReflectionPropertyInstance (Host, Captured, parent);
			}
		}

		class ReflectionPropertyInstance : ParameterizedTestInstance
		{
			new public ReflectionPropertyHost Host {
				get { return (ReflectionPropertyHost)base.Host; }
			}

			public ParameterizedTestInstance Instance {
				get;
				private set;
			}

			public object CapturedValue {
				get;
				private set;
			}

			object current;
			bool hasNext;

			public ReflectionPropertyInstance (ReflectionPropertyHost host, ParameterizedTestInstance instance, TestInstance parent)
				: base (host, parent)
			{
				Instance = instance;
			}

			public ReflectionPropertyInstance (ReflectionPropertyHost host, object captured, TestInstance parent)
				: base (host, parent)
			{
				CapturedValue = captured;
			}

			public override void Initialize (TestContext ctx)
			{
				if (Instance != null)
					Instance.Initialize (ctx);
				else {
					current = CapturedValue;
					var cloneable = current as ICloneable;
					if (cloneable != null)
						current = cloneable.Clone ();
					hasNext = true;
				}
			}

			public override bool HasNext ()
			{
				return Instance != null ? Instance.HasNext () : hasNext;
			}

			public override bool MoveNext (TestContext ctx)
			{
				if (Instance != null) {
					if (!Instance.MoveNext (ctx))
						return false;
					current = Instance.Current;
				} else {
					if (!hasNext)
						return false;
					hasNext = false;
				}

				Host.Property.SetValue (GetFixtureInstance ().Instance, current);
				return true;
			}

			public override object Current {
				get { return current; }
			}
		}
	}
}

