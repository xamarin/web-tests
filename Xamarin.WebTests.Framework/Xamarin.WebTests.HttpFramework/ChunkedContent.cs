//
// ChunkedContent.cs
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
using System.Linq;
using System.Globalization;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Xamarin.AsyncTests;

namespace Xamarin.WebTests.HttpFramework
{
	public class ChunkedContent : HttpContent
	{
		List<string> chunks;

		public ChunkedContent (params string[] chunks)
		{
			this.chunks = new List<string> (chunks);
		}

		public ChunkedContent (IEnumerable<string> chunks)
		{
			this.chunks = new List<string> (chunks);
		}

		public static async Task<ChunkedContent> ReadNonChunked (HttpStreamReader reader, CancellationToken cancellationToken)
		{
			var chunks = new List<string> ();

			while (true) {
				cancellationToken.ThrowIfCancellationRequested ();

				var line = await reader.ReadLineAsync (cancellationToken).ConfigureAwait (false);
				if (string.IsNullOrEmpty (line))
					break;

				chunks.Add (line);
			}

			return new ChunkedContent (chunks);
		}

		public static async Task<ChunkedContent> Read (TestContext ctx, HttpStreamReader reader, CancellationToken cancellationToken)
		{
			var chunks = new List<string> ();

			do {
				cancellationToken.ThrowIfCancellationRequested ();
				var header = await reader.ReadLineAsync (cancellationToken);
				var length = int.Parse (header, NumberStyles.HexNumber);
				if (length == 0)
					break;

				cancellationToken.ThrowIfCancellationRequested ();

				var buffer = new char [length];
				var ret = await reader.ReadAsync (buffer, 0, length, cancellationToken);
				if (ret != length)
					throw new InvalidOperationException ();

				chunks.Add (new string (buffer));

				cancellationToken.ThrowIfCancellationRequested ();

				var empty =  await reader.ReadLineAsync (cancellationToken);
				if (!string.IsNullOrEmpty (empty))
					throw new InvalidOperationException ();
			} while (true);

			return new ChunkedContent (chunks);
		}

		public override bool HasLength {
			get { return true; }
		}

		public override int Length {
			get { return chunks.Sum (c => c.Length); }
		}

		public override string AsString ()
		{
			return string.Join (string.Empty, chunks);
		}

		public override byte[] AsByteArray ()
		{
			throw new NotSupportedException ();
		}

		public override void AddHeadersTo (HttpMessage message)
		{
			message.TransferEncoding = "chunked";
		}

		public override async Task WriteToAsync (TestContext ctx, Stream stream)
		{
			foreach (var chunk in chunks) {
				var bytes = StringContent.GetBytes ("{0:x}\r\n{1}\r\n", chunk.Length, chunk);
				await stream.WriteAsync (bytes, 0, bytes.Length).ConfigureAwait (false);
			}
			var trailer = StringContent.GetBytes ("0\r\n\r\n\r\n");
			await stream.WriteAsync (trailer, 0, trailer.Length);
		}

		public override async Task WriteToAsync (TestContext ctx, StreamWriter writer)
		{
			foreach (var chunk in chunks)
				await writer.WriteAsync (string.Format ("{0:x}\r\n{1}\r\n", chunk.Length, chunk));
			await writer.WriteAsync ("0\r\n\r\n\r\n");
		}
	}
}

