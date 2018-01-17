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
using System.Text;
using System.Globalization;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Xamarin.AsyncTests;
using Xamarin.AsyncTests.Constraints;

namespace Xamarin.WebTests.HttpFramework
{
	using TestFramework;

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
				var chunk = await ReadChunk (ctx, reader, cancellationToken).ConfigureAwait (false);
				if (chunk == null)
					break;

				chunks.Add (Encoding.UTF8.GetString (chunk, 0, chunk.Length));
			} while (true);

			return new ChunkedContent (chunks);
		}

		public static async Task<byte[]> ReadChunk (TestContext ctx, HttpStreamReader reader, CancellationToken cancellationToken)
		{
			cancellationToken.ThrowIfCancellationRequested ();
			var header = await reader.ReadLineAsync (cancellationToken);
			var length = int.Parse (header, NumberStyles.HexNumber);
			if (length == 0)
				return null;

			cancellationToken.ThrowIfCancellationRequested ();

			var buffer = new byte[length];
			int pos = 0;
			while (pos < length) {
				var ret = await reader.ReadAsync (buffer, pos, length - pos, cancellationToken);
				if (ret < 0)
					throw new IOException ();
				if (ret == 0)
					break;
				pos += ret;
			}

			ctx.Assert (pos, Is.EqualTo (length), "read entire chunk");

			cancellationToken.ThrowIfCancellationRequested ();

			var empty = await reader.ReadLineAsync (cancellationToken);
			if (!string.IsNullOrEmpty (empty))
				throw new InvalidOperationException ();

			return buffer;
		}

		public static async Task WriteChunk (Stream stream, byte[] chunk, CancellationToken cancellationToken)
		{
			await stream.WriteAsync ($"{chunk.Length:x}\r\n", cancellationToken).ConfigureAwait (false);
			await stream.WriteAsync (chunk, cancellationToken);
			await stream.WriteAsync ("\r\n", cancellationToken);
		}

		public static async Task WriteChunkAsBlob (Stream stream, byte[] chunk, CancellationToken cancellationToken)
		{
			using (var ms = new MemoryStream ()) {
				var header = Encoding.UTF8.GetBytes ($"{chunk.Length:x}\r\n");
				var newline = Encoding.UTF8.GetBytes ("\n\r");
				ms.Write (header, 0, header.Length);
				ms.Write (chunk, 0, chunk.Length);
				ms.Write (newline, 0, newline.Length);
				await stream.WriteAsync (ms.ToArray (), cancellationToken).ConfigureAwait (false);
			}
		}

		public static Task WriteChunkTrailer (Stream stream, CancellationToken cancellationToken)
		{
			return stream.WriteAsync ("0\r\n\r\n", cancellationToken);
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

		public override async Task WriteToAsync (TestContext ctx, Stream stream, CancellationToken cancellationToken)
		{
			foreach (var chunk in chunks) {
				var bytes = Encoding.UTF8.GetBytes (chunk);
				await WriteChunk (stream, bytes, cancellationToken).ConfigureAwait (false);
			}
			await WriteChunkTrailer (stream, cancellationToken).ConfigureAwait (false);
		}
	}
}

