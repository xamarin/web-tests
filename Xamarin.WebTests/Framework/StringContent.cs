//
// StringContent.cs
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
using System.Threading;
using System.Threading.Tasks;

namespace Xamarin.WebTests.Framework
{
	public class StringContent : HttpContent
	{
		string content;

		public StringContent (string content)
		{
			this.content = content;
		}

		public async static Task<StringContent> Read (StreamReader reader, int length)
		{
			var buffer = new char [length];
			int offset = 0;
			while (offset < length) {
				var len = Math.Min (4096, length - offset);
				var ret = await reader.ReadAsync (buffer, offset, len);
				if (ret <= 0)
					throw new InvalidOperationException ();

				offset += ret;
			}

			return new StringContent (new string (buffer));
		}

		public override string AsString ()
		{
			return content;
		}

		public override void AddHeadersTo (HttpMessage message)
		{
			if (!message.Headers.ContainsKey ("Content-Length"))
				message.AddHeader ("Content-Length", content.Length + 2);
			if (!message.Headers.ContainsKey ("Content-Type"))
				message.AddHeader ("Content-Type", "text/plain");
		}

		public override void WriteTo (StreamWriter writer)
		{
			writer.Write (content + "\r\n");
		}
	}
}

