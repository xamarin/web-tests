//
// HttpRequest.cs
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
using System.IO;
using System.Text;
using System.Threading;
using System.Globalization;
using System.Collections.Generic;

namespace Xamarin.WebTests.Framework
{
	public class HttpRequest : HttpMessage
	{
		public HttpRequest (Connection connection, StreamReader reader)
			: base (connection, reader)
		{
		}

		public string Method {
			get; private set;
		}

		public string Path {
			get; private set;
		}

		protected override void Read ()
		{
			var header = reader.ReadLine ();
			var fields = header.Split (new char[] { ' ' }, StringSplitOptions.None);
			if (fields.Length != 3)
				throw new InvalidOperationException ();

			Method = fields [0];
			Protocol = ProtocolFromString (fields [2]);
			if (Method.Equals ("CONNECT"))
				Path = fields [1];
			else
				Path = fields [1].StartsWith ("/") ? fields [1] : new Uri (fields [1]).AbsolutePath;

			ReadHeaders ();
		}

		public void Write (StreamWriter writer)
		{
			writer.Write ("{0} {1} {2}\r\n", Method, Path, ProtocolToString (Protocol));
			WriteHeaders (writer);
		}

		public override string ToString ()
		{
			return string.Format ("[HttpRequest: Method={0}, Path={1}]", Method, Path);
		}
	}
}

