//
// TestFeature.cs
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
	public class TestFeature
	{
		public string Name {
			get;
			private set;
		}

		public string Description {
			get;
			private set;
		}

		public bool? Constant {
			get;
			private set;
		}

		public bool? DefaultValue {
			get;
			private set;
		}

		public bool CanModify {
			get { return Constant == null; }
		}

		public TestFeature (string name, string description, bool? defaultValue = null)
			: this (name, description, defaultValue, null)
		{
		}

		public TestFeature (string name, string description, Func<bool> func)
			: this (name, description, null, GetConstantValue (func))
		{
		}

		static bool? GetConstantValue (Func<bool> func)
		{
			if (func != null)
				return func ();
			return null;
		}

		internal TestFeature (string name, string description, bool? defaultValue, bool? constant)
		{
			Name = name;
			Description = description;
			DefaultValue = defaultValue;
			Constant = constant;
		}

		public override string ToString ()
		{
			var constant = string.Empty;
			if (Constant != null)
				constant = string.Format (", Constant={0}", Constant.Value);
			return string.Format ("[TestFeature: Name={0}, Description={1}{2}]", Name, Description, constant);
		}
	}
}

