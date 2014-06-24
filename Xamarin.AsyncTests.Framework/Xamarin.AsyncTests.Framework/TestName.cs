//
// TestName.cs
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
using System.Text;
using System.Collections.Generic;

namespace Xamarin.AsyncTests.Framework
{
	public class TestName
	{
		public string Name {
			get;
			private set;
		}

		public bool HasParameters {
			get { return Parameters.Length > 0; }
		}

		public KeyValuePair<string,string>[] Parameters {
			get;
			private set;
		}

		public TestName (string name, params KeyValuePair<string,string>[] parameters)
		{
			Name = name;
			Parameters = parameters;
		}

		string fullName;
		public string FullName {
			get {
				if (fullName == null)
					fullName = GetFullName ();
				return fullName;
			}
		}

		string GetFullName ()
		{
			if (Parameters == null || Parameters.Length == 0)
				return Name;
			var sb = new StringBuilder (Name);
			sb.Append ("(");
			for (int i = 0; i < Parameters.Length; i++) {
				if (i > 0)
					sb.Append (",");
				sb.Append (Parameters [i].Value);
			}
			sb.Append (")");
			return sb.ToString ();
		}

		public override string ToString ()
		{
			return string.Format ("[{0}]", FullName);
		}
	}
}

