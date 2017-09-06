//
// TestCategory.cs
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

namespace Xamarin.AsyncTests
{
	public class TestCategory
	{
		public string Name {
			get;
			private set;
		}

		public bool IsBuiltin {
			get;
			private set;
		}

		public bool IsExplicit {
			get; set;
		}

		public TestCategory (string name)
		{
			Name = name;
		}

		public static readonly TestCategory All = new TestCategory ("All") { IsBuiltin = true };

		public static readonly TestCategory Global = new TestCategory ("Global") { IsBuiltin = true };

		public static readonly TestCategory Martin = new TestCategory ("Martin") { IsExplicit = true, IsBuiltin = true };

		public override string ToString ()
		{
			return string.Format ("[TestCategory: Name={0}]", Name);
		}
	}
}

