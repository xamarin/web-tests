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

namespace Xamarin.AsyncTests.Framework {
	class ParameterSourceInstance<T> : ParameterizedTestInstance {
		List<Tuple<ITestParameter, T>> parameters;
		bool hasNext;
		ParameterizedTestValue current;
		int index;

		new public ParameterSourceHost<T> Host {
			get { return (ParameterSourceHost<T>)base.Host; }
		}

		public ITestParameterSource<T> SourceInstance {
			get;
		}

		public bool UseFixtureInstance {
			get;
		}

		public string Filter {
			get;
		}

		public override ParameterizedTestValue Current {
			get { return current; }
		}

		public ParameterSourceInstance (
			ParameterSourceHost<T> host, TestNode node, TestInstance parent,
			ITestParameterSource<T> sourceInstance, bool useFixtureInstance, string filter)
			: base (host, node, parent)
		{
			SourceInstance = sourceInstance;
			UseFixtureInstance = useFixtureInstance;
			Filter = filter;
		}

		public override void Initialize (TestContext ctx)
		{
			base.Initialize (ctx);

			if (Node.Parameter != null) {
				var value = Clone (Host.Deserialize (Node.Parameter));
				current = new ParameterSourceValue (this, Node.Parameter, value);
				hasNext = true;
				return;
			}

			ITestParameterSource<T> instance;
			if (UseFixtureInstance)
				instance = (ITestParameterSource<T>)GetFixtureInstance ().Instance;
			else if (SourceInstance != null)
				instance = SourceInstance;
			else
				throw new InternalErrorException ();

			parameters = new List<Tuple<ITestParameter, T>> ();
			foreach (var value in instance.GetParameters (ctx, Filter)) {
				var parameter = Host.Serialize (value);
				parameters.Add (new Tuple<ITestParameter,T> (parameter, value));
			}

			index = 0;
		}

		static T Clone (T value)
		{
			var cloneable = value as ICloneable;
			if (cloneable != null)
				value = (T)cloneable.Clone ();
			return value;
		}

		public override void Destroy (TestContext ctx)
		{
			parameters = null;
			current = null;
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
				var value = Clone (parameters [index].Item2);
				current = new ParameterSourceValue (this, parameters [index].Item1, value);
				index++;
				return true;
			} else {
				if (!hasNext)
					return false;
				hasNext = false;
				return true;
			}
		}

		class ParameterSourceValue : ParameterizedTestValue
		{
			readonly ITestParameter parameter;

			public ParameterSourceValue (ParameterizedTestInstance instance, ITestParameter parameter, object value)
				: base (instance, value)
			{
				this.parameter = parameter;
			}

			public override ITestParameter Parameter {
				get {
					return parameter;
				}
			}
		}

	}
}

