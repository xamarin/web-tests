//
// ParameterSourceInstance.cs
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
	class ParameterSourceInstance<T> : ParameterizedTestInstance
	{
		ITestParameterSource<T> source;
		List<T> parameters;
		T current;
		int index;
		bool hasSource;

		public Type SourceType {
			get;
			private set;
		}

		public string Filter {
			get;
			private set;
		}

		public override object Current {
			get { return current; }
		}

		public ParameterSourceInstance (ParameterizedTestHost host, TestInstance parent, Type sourceType, string filter)
			: base (host, parent)
		{
			SourceType = sourceType;
			Filter = filter;
		}

		ParameterSourceInstance (ParameterizedTestHost host, TestInstance parent, ITestParameterSource<T> source, string filter)
			: base (host, parent)
		{
			this.source = source;
			this.hasSource = true;
			Filter = filter;
		}

		public static ParameterizedTestInstance CreateFromSource (
			ParameterizedTestHost host, TestInstance parent, ITestParameterSource<T> source, string filter)
		{
			return new ParameterSourceInstance<T> (host, parent, source, filter);
		}

		ITestParameterSource<T> CreateSource (TestContext context)
		{
			if (SourceType != null)
				return (ITestParameterSource<T>)Activator.CreateInstance (SourceType);
			else if (typeof(T).Equals (typeof(bool)))
				return (ITestParameterSource<T>)ParameterizedTestHost.CreateBooleanSource ();
			else if (typeof(T).GetTypeInfo ().IsEnum)
				return (ITestParameterSource<T>)ParameterizedTestHost.CreateEnumSource<T> ();

			return (ITestParameterSource<T>)GetFixtureInstance ().Instance;
		}

		public override Task Initialize (TestContext context, CancellationToken cancellationToken)
		{
			return Task.Run (() => {
				if (!hasSource)
					source = CreateSource (context);
				parameters = new List<T> (source.GetParameters (context, Filter));
				index = 0;
			});
		}

		public override Task ReuseInstance (TestContext context, CancellationToken cancellationToken)
		{
			return Task.FromResult<object> (null);
		}

		public override bool HasNext ()
		{
			return index < parameters.Count;
		}

		public override Task MoveNext (TestContext context, CancellationToken cancellationToken)
		{
			return Task.Run (() => {
				current = parameters [index];
				index++;
			});
		}

		public override Task Destroy (TestContext context, CancellationToken cancellationToken)
		{
			return Task.Run (() => {
				if (!hasSource)
					source = null;
				parameters = null;
				index = -1;
			});
		}
	}
}

