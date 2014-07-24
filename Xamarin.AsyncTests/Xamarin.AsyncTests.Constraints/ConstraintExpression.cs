//
// AggregatedConstraint.cs
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

namespace Xamarin.AsyncTests.Constraints
{
	public class ConstraintExpression : Constraint
	{
		public string Name {
			get;
			private set;
		}

		public Constraint Inner {
			get;
			private set;
		}

		public Func<Func<object,bool>,object,bool> Operator {
			get;
			private set;
		}

		public ConstraintExpression (string name, Func<Func<object,bool>,object,bool> op, Constraint inner)
		{
			Name = name;
			Operator = op;
			Inner = inner;
		}

		public ConstraintExpression (ConstraintOperator op, Constraint inner)
		{
			Name = op.Name;
			Operator = (f,a) => op.Evaluate (f,a);
			Inner = inner;
		}

		public override bool Evaluate (object actual)
		{
			return Operator (f => Inner.Evaluate (actual), actual);
		}

		public override string Print ()
		{
			return Name + "." + Inner.Print ();
		}
	}
}

