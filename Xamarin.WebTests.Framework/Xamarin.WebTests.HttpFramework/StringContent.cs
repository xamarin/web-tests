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
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xamarin.AsyncTests;

namespace Xamarin.WebTests.HttpFramework
{
	using TestFramework;

	public class StringContent : HttpContent
	{
		string content;

		public StringContent (string content)
		{
			this.content = content;
		}

		public static readonly StringContent Empty = new StringContent (string.Empty);

		public static HttpContent CreateMaybeNull (string content)
		{
			return content != null ? new StringContent (content) : null;
		}

		public async static Task<StringContent> Read (TestContext ctx, HttpStreamReader reader, int length, CancellationToken cancellationToken)
		{
			var buffer = new char [length];
			int offset = 0;
			while (offset < length) {
				cancellationToken.ThrowIfCancellationRequested ();
				var len = Math.Min (16384, length - offset);
				var ret = await reader.ReadAsync (buffer, offset, len, cancellationToken);
				if (ret <= 0) {
					if (offset == 0)
						throw new IOException ("Client didn't send any body.");
					throw new InvalidOperationException ();
				}

				offset += ret;
			}

			return new StringContent (new string (buffer));
		}

		public override bool HasLength {
			get { return true; }
		}

		public override int Length {
			get { return content.Length; }
		}

		public override string AsString ()
		{
			return content;
		}

		public override byte[] AsByteArray ()
		{
			throw new NotSupportedException ();
		}

		public override void AddHeadersTo (HttpMessage message)
		{
			if (message.ContentLength == null)
				message.ContentLength = content.Length;
			else if (message.ContentLength.Value != content.Length)
				throw new InvalidOperationException ();
			if (message.ContentType == null)
				message.ContentType = "text/plain";
		}

		public override async Task WriteToAsync (TestContext ctx, Stream stream, CancellationToken cancellationToken)
		{
			if (string.IsNullOrEmpty (content))
				return;
			await stream.WriteAsync (content, cancellationToken).ConfigureAwait (false);
		}
	}
}

