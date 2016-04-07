//
// ValidationTestCategoryAttribute.cs
//
// Author:
//       Martin Baulig <martin.baulig@xamarin.com>
//
// Copyright (c) 2016 Xamarin Inc. (http://www.xamarin.com)

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
using Xamarin.AsyncTests;

namespace Xamarin.WebTests.MonoTestFeatures
{
	using MonoTestFramework;

	[AttributeUsage (AttributeTargets.Method, AllowMultiple = false)]
	public class ValidationTestCategoryAttribute : FixedTestParameterAttribute
	{
		public override Type Type {
			get { return typeof (ValidationTestCategory); }
		}

		public override object Value {
			get { return category; }
		}

		public override string Identifier {
			get { return identifier; }
		}

		public ValidationTestCategory Category {
			get { return category; }
		}

		readonly string identifier;
		readonly ValidationTestCategory category;

		public ValidationTestCategoryAttribute (ValidationTestCategory category)
		{
			this.category = category;
			this.identifier = Type.Name;
		}
	}
}

