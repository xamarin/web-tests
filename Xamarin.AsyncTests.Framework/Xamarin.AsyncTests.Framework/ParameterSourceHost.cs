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
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace Xamarin.AsyncTests.Framework
{
	class ParameterSourceHost<T> : ParameterizedTestHost
	{
		public ITestParameterSource<T> SourceInstance {
			get;
			private set;
		}

		public string Filter {
			get;
			private set;
		}

		public ParameterSourceHost (
			string name, ITestParameterSource<T> sourceInstance, IParameterSerializer serializer,
			string filter, TestFlags flags = TestFlags.None)
			: base (name, typeof (T).GetTypeInfo (), serializer, flags)
		{
			SourceInstance = sourceInstance;
			Filter = filter;
		}

		internal override IEnumerable<ITestParameter> GetParameters (TestContext ctx)
		{
			var parameters = SourceInstance.GetParameters (ctx, Filter);
			foreach (var value in parameters) {
				yield return Serialize (value);
			}
		}

		internal ITestParameter Serialize (T value)
		{
			var testParameter = value as ITestParameter;
			if (testParameter != null)
				return testParameter;

			return Serializer.ObjectToParameter (value);
		}

		internal T Deserialize (ITestParameter parameter)
		{
			if (Serializer != null)
				return (T)Serializer.ParameterToObject (parameter);

			return (T)parameter;
		}

		internal override TestInstance CreateInstance (TestPath path, TestInstance parent)
		{
			return new ParameterSourceInstance<T> (this, path, parent, SourceInstance, Filter);
		}
	}
}

