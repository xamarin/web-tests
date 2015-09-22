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
using System.Linq;
using System.Text;
using System.Collections.Generic;

namespace Xamarin.AsyncTests
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

		public Parameter[] Parameters {
			get;
			private set;
		}

		public IEnumerable<Parameter> VisibleParameters {
			get { return Parameters.Where (p => !p.IsHidden); }
		}

		static int nextId;
		public readonly int ID = ++nextId;

		public static TestName Empty = new TestName (string.Empty);

		public static bool IsNullOrEmpty (TestName name)
		{
			return name == null || name.IsEmpty;
		}

		public bool IsEmpty {
			get;
			private set;
		}

		public TestName (string name, params Parameter[] parameters)
		{
			Name = name;
			Parameters = parameters;
			IsEmpty = string.IsNullOrEmpty (name) && parameters.Length == 0;
		}

		internal TestName (string localName, string fullName, params Parameter[] parameters)
			: this (fullName, parameters)
		{
			this.localName = localName;
		}

		string fullName;
		string fullLocalName;
		string localName;

		internal string InternalLocalName {
			get { return localName; }
		}

		public string FullName {
			get {
				if (fullName == null)
					fullName = GetFullName ();
				return fullName;
			}
		}

		public string LocalName {
			get {
				if (fullLocalName == null)
					fullLocalName = GetLocalName ();
				return fullLocalName;
			}
		}

		string GetFullName ()
		{
			if (Parameters == null || Parameters.Length == 0)
				return Name;
			var sb = new StringBuilder (Name);
			AppendParameters (sb);
			return sb.ToString ();
		}

		string GetLocalName ()
		{
			if (Parameters == null || Parameters.Length == 0)
				return localName;
			var sb = new StringBuilder (localName);
			AppendParameters (sb);
			return sb.ToString ();
		}

		void AppendParameters (StringBuilder sb)
		{
			sb.Append ("(");
			bool first = true;
			for (int i = 0; i < Parameters.Length; i++) {
				if (Parameters [i].IsHidden)
					continue;
				if (first)
					first = false;
				else
					sb.Append (",");
				sb.Append (Parameters [i].Value);
			}
			sb.Append (")");
		}

		public class Parameter
		{
			public string Name {
				get;
				private set;
			}

			public string Value {
				get;
				private set;
			}

			public bool IsHidden {
				get;
				private set;
			}

			public Parameter (string name, string value, bool isHidden = false)
			{
				Name = name;
				Value = value;
				IsHidden = isHidden;
			}

			public override string ToString ()
			{
				return string.Format ("[Parameter: Name={0}, Value={1}, IsHidden={2}]", Name, Value, IsHidden);
			}
		}

		public override string ToString ()
		{
			return string.Format ("[{0}]", FullName);
		}
	}
}

