//
// TestNameBuilder.cs
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
using System.Collections.Generic;

namespace Xamarin.AsyncTests
{
	public class TestNameBuilder
	{
		Stack<string> parts = new Stack<string> ();
		Stack<KeyValuePair<string,string>> parameters = new Stack<KeyValuePair<string, string>> ();

		public TestName GetName ()
		{
			var name = string.Join (".", parts.Reverse ());
			return new TestName (name, parameters.Reverse ().ToArray ());
		}

		public static TestNameBuilder CreateFromName (TestName name)
		{
			var builder = new TestNameBuilder ();
			if (!string.IsNullOrEmpty (name.Name))
				builder.PushName (name.Name);
			if (name.HasParameters) {
				foreach (var entry in name.Parameters.Reverse ()) {
					builder.PushParameter (entry.Key, entry.Value);
				}
			}

			return builder;
		}

		public void PushName (string part)
		{
			parts.Push (part);
		}

		public void PopName ()
		{
			parts.Pop ();
		}

		public void PushParameter (string key, object value)
		{
			parameters.Push (new KeyValuePair<string, string> (key, Print (value)));
		}

		public void PopParameter ()
		{
			parameters.Pop ();
		}

		string Print (object value)
		{
			return value != null ? value.ToString () : "<null>";
		}

		public string GetFullName ()
		{
			return GetName ().FullName;
		}
	}
}

