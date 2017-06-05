//
// Is.cs
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
	public static class Is
	{
		public static Constraint False {
			get { return new FalseConstraint (); }
		}

		public static Constraint True {
			get { return new TrueConstraint (); }
		}

		public static Constraint Null {
			get { return new NullConstraint (); }
		}

		public static Constraint Empty {
			get { return new EmptyConstraint (); }
		}

		public static Constraint NullOrEmpty {
			get { return new NullOrEmptyConstraint (); }
		}

		public static Constraint EqualTo (object expected)
		{
			return new EqualConstraint (expected);
		}

		public static Constraint InstanceOfType (Type type, bool allowSubclasses = false)
		{
			return new InstanceOfTypeConstraint (type, allowSubclasses);
		}

		public static Constraint InstanceOf<T> (bool allowSubclasses = false)
		{
			return new InstanceOfTypeConstraint (typeof (T), allowSubclasses);
		}

		public static Constraint GreaterThanOrEqualTo (object expected)
		{
			return new GreaterThanOrEqualConstraint (expected);
		}

		public static Constraint LessThanOrEqualTo (object expected)
		{
			return new LessThanOrEqualConstraint (expected);
		}

		public static Constraint GreaterThan (object expected)
		{
			return new GreaterThanConstraint (expected);
		}

		public static Constraint LessThan (object expected)
		{
			return new LessThanConstraint (expected);
		}

		public static ConstraintOperator Not {
			get { return new NotOperator (); }
		}
	}
}

