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

namespace Xamarin.AsyncTests.Framework
{
	class ParameterSourceInstance<T> : ParameterizedTestInstance
	{
		ITestParameterSource<T> source;
		List<T> parameters;
		bool hasNext;
		T current;
		int index;

		public ParameterSourceHost<T> SourceHost {
			get;
			private set;
		}

		public Type SourceType {
			get;
			private set;
		}

		public ITestParameterSource<T> SourceInstance {
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

		public string CapturedIdentifier {
			get; set;
		}

		public object CapturedValue {
			get; set;
		}

		public override object Current {
			get { return current; }
		}

		public ParameterSourceInstance (ParameterSourceHost<T> host, TestInstance parent,
			Type sourceType, ITestParameterSource<T> sourceInstance, bool useFixtureInstance, string filter)
			: base (host, parent)
		{
			SourceHost = host;
			SourceType = sourceType;
			SourceInstance = sourceInstance;
			UseFixtureInstance = useFixtureInstance;
			Filter = filter;
		}

		ITestParameterSource<T> CreateSource (TestContext ctx)
		{
			if (SourceInstance != null)
				return SourceInstance;
			else if (SourceType != null)
				return (ITestParameterSource<T>)Activator.CreateInstance (SourceType);
			else if (UseFixtureInstance)
				return (ITestParameterSource<T>)GetFixtureInstance ().Instance;
			else
				throw new InternalErrorException ();
		}

		public override void Initialize (TestContext ctx)
		{
			base.Initialize (ctx);

			if (CapturedValue != null) {
				current = (T)CapturedValue;
				var cloneable = current as ICloneable;
				if (cloneable != null)
					current = (T)cloneable.Clone ();
				hasNext = true;
				return;
			}

			source = CreateSource (ctx);
			parameters = new List<T> (source.GetParameters (ctx, Filter));
			if (CapturedIdentifier != null) {
				if (parameters.Count == 0)
					throw new InternalErrorException ();
				else if (parameters.Count > 1)
					parameters.RemoveAll (p => !((ITestParameter)p).Identifier.Equals (CapturedIdentifier));
				if (parameters.Count != 1)
					throw new InternalErrorException ();
			}
			index = 0;
		}

		public override void Destroy (TestContext ctx)
		{
			source = null;
			parameters = null;
			current = default(T);
			index = -1;
			base.Destroy (ctx);
		}

		public override bool HasNext ()
		{
			return parameters != null ? index < parameters.Count : hasNext;
		}

		public override bool MoveNext (TestContext ctx)
		{
			if (!HasNext ())
				return false;

			if (parameters != null) {
				current = parameters [index];
				index++;
				return true;
			} else {
				if (!hasNext)
					return false;
				hasNext = false;
				return true;
			}
		}
	}
}

