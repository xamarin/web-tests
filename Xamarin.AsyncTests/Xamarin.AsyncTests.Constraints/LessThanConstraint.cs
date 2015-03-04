//
// GreaterThanOrEqualConstraint.cs
//
// Author:
//       Martin Baulig <martin.baulig@xamarin.com>
//
// Copyright (c) 2015 Xamarin, Inc.
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
	public class LessThanConstraint : Constraint
	{
		public object Expected {
			get;
			private set;
		}

		public LessThanConstraint (object expected)
		{
			Expected = expected;
		}

		public override bool Evaluate (object actual, out string message)
		{
			message = null;
			if (actual is int)
				return (int)actual < (int)Expected;
			else if (actual is short)
				return (short)actual < (short)Expected;
			else if (actual is long)
				return (long)actual < (long)Expected;
			else
				return false;
		}

		public override string Print ()
		{
			return string.Format ("LessThan({0})", Expected);
		}
	}
}

