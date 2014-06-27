//
// ParameterizedTestHost.cs
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

namespace Xamarin.AsyncTests.Framework.Internal
{
	abstract class ParameterizedTestHost : TestHost
	{
		public string ParameterName {
			get;
			private set;
		}

		public TypeInfo ParameterType {
			get;
			private set;
		}

		protected ParameterizedTestHost (string name, TypeInfo type, TestFlags flags = TestFlags.None)
		{
			ParameterName = name;
			ParameterType = type;
			Flags = flags;
		}

		public static ParameterizedTestHost CreateBoolean (string name)
		{
			var source = CreateBooleanSource ();
			return new ParameterSourceHost<bool> (name, source, null);
		}

		internal static ITestParameterSource<bool> CreateBooleanSource ()
		{
			return new BooleanTestSource ();
		}

		class BooleanTestSource : ITestParameterSource<bool>
		{
			public IEnumerable<bool> GetParameters (TestContext context, string filter)
			{
				yield return false;
				yield return true;
			}
		}

		public static ParameterizedTestHost CreateEnum (TypeInfo typeInfo, string name)
		{
			if (!typeInfo.IsEnum || typeInfo.GetCustomAttribute<FlagsAttribute> () != null)
				throw new InvalidOperationException ();

			var type = typeInfo.AsType ();
			var sourceType = typeof(EnumTestSource<>).MakeGenericType (type);
			var hostType = typeof(ParameterSourceHost<>).MakeGenericType (type);

			var source = Activator.CreateInstance (sourceType);
			return (ParameterizedTestHost)Activator.CreateInstance (hostType, name, source, null, TestFlags.None);
		}

		internal static ITestParameterSource<T> CreateEnumSource<T> ()
		{
			return new EnumTestSource<T> ();
		}

		class EnumTestSource<T> : ITestParameterSource<T>
		{
			public IEnumerable<T> GetParameters (TestContext context, string filter)
			{
				foreach (var value in Enum.GetValues (typeof (T)))
					yield return (T)value;
			}
		}

		class ParameterSourceHost<T> : ParameterizedTestHost
		{
			public string Name {
				get;
				private set;
			}

			public ITestParameterSource<T> Source {
				get;
				private set;
			}

			public string Filter {
				get;
				private set;
			}

			public ParameterSourceHost (string name, ITestParameterSource<T> source, string filter, TestFlags flags = TestFlags.None)
				: base (name, typeof (T).GetTypeInfo (), flags)
			{
				Source = source;
				Filter = filter;
			}

			internal override TestInstance CreateInstance (TestContext context, TestInstance parent)
			{
				return ParameterSourceInstance<T>.CreateFromSource (this, parent, Source, Filter);
			}
		}
	}
}

