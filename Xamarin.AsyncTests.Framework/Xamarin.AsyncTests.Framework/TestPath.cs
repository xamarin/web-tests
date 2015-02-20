//
// TestPathNode.cs
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
using System.Xml.Linq;

namespace Xamarin.AsyncTests.Framework
{
	abstract class TestPath
	{
		public string Type {
			get;
			private set;
		}

		public TestPath Parent {
			get;
			private set;
		}

		protected TestPath (string type, TestPath parent)
		{
			Type = type;
			Parent = parent;
		}

		internal abstract bool Serialize (XElement node);

		public static TestPath CreateFromInstance (TestInstance instance)
		{
			TestPath parent = null;
			if (instance.Parent != null)
				parent = CreateFromInstance (instance.Parent);
			return instance.CreatePath (parent);
		}

		protected virtual void GetTestName (TestNameBuilder builder)
		{
			if (Parent != null)
				Parent.GetTestName (builder);
		}

		public static TestName GetTestName (TestPath path)
		{
			var builder = new TestNameBuilder ();
			if (path != null)
				path.GetTestName (builder);
			return builder.GetName ();
		}

		public override string ToString ()
		{
			return string.Format ("[TestPath: Type={0}, Parent={1}]", Type, Parent);
		}
	}
}

