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
using System.Reflection;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Xamarin.AsyncTests.Framework.Reflection
{
	class ReflectionPropertyHost : ParameterizedTestHost
	{
		public ReflectionTestFixture Fixture {
			get;
			private set;
		}

		public PropertyInfo Property {
			get;
			private set;
		}

		public TestHost Host {
			get;
			private set;
		}

		public ReflectionPropertyHost (ReflectionTestFixture fixture, PropertyInfo prop, TestHost host)
			: base (prop.Name, prop.PropertyType.GetTypeInfo (), host.Flags)
		{
			Fixture = fixture;
			Property = prop;
			Host = host;
		}

		internal override TestInstance CreateInstance (TestInstance parent)
		{
			var instance = (ParameterizedTestInstance)Host.CreateInstance (parent);
			return new ReflectionPropertyInstance (this, instance, parent);
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

			public ReflectionPropertyInstance (ReflectionPropertyHost host, ParameterizedTestInstance instance, TestInstance parent)
				: base (host, parent)
			{
				Instance = instance;
			}

			public override void Initialize (TestContext context)
			{
				Instance.Initialize (context);
			}

			public override bool HasNext ()
			{
				return Instance.HasNext ();
			}

			public override bool MoveNext (TestContext context)
			{
				if (!Instance.MoveNext (context))
					return false;

				Host.Property.SetValue (GetFixtureInstance ().Instance, Instance.Current);
				return true;
			}

			public override object Current {
				get { return Instance.Current; }
			}
		}

	}
}

