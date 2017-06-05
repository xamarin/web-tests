//
// ConstraintExpression.cs
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
	public abstract class ConstraintOperator
	{
		public abstract ConstraintExpression Combine (Constraint right);

		public Constraint False {
			get { return Combine (new FalseConstraint ()); }
		}

		public Constraint True {
			get { return Combine (new TrueConstraint ()); }
		}

		public Constraint Null {
			get { return Combine (new NullConstraint ()); }
		}

		public Constraint Empty {
			get { return Combine (new EmptyConstraint ()); }
		}

		public Constraint NullOrEmpty {
			get { return Combine (new NullOrEmptyConstraint ()); }
		}

		public Constraint EqualTo (object expected)
		{
			return Combine (new EqualConstraint (expected));
		}

		public Constraint InstanceOfType (Type type, bool allowSubclasses = false)
		{
			return Combine (new InstanceOfTypeConstraint (type, allowSubclasses));
		}

		public Constraint InstanceOfType<T> (bool allowSubclasses = false)
		{
			return Combine (new InstanceOfTypeConstraint (typeof (T) , allowSubclasses));
		}
	}
}

