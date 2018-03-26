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
		byte[][] chunkArray;
		string contentAsString;
		Dictionary<string, string> headers;

		public bool WriteAsBlob {
			get; set;
		}

		internal IReadOnlyDictionary<string, string> ExtraHeaders {
			get { return headers; }
		}

		public ChunkedContent (params string[] chunks)
		{
			chunkArray = CreateChunkArray (chunks);
			Length = chunkArray.Sum (c => c.Length);
			contentAsString = string.Concat (chunks);
		}

		public ChunkedContent (IReadOnlyList<string> chunks)
		{
			chunkArray = CreateChunkArray (chunks);
			Length = chunkArray.Sum (c => c.Length);
			contentAsString = string.Concat (chunks);
		}

		static byte[][] CreateChunkArray (IReadOnlyList<string> chunks)
		{
			var array = new byte[chunks.Count][];
			for (int i = 0; i < chunks.Count; i++) {
				array[i] = Encoding.UTF8.GetBytes (chunks[i]);
			}
			return array;
		}

		public ChunkedContent (ICollection<byte[]> chunks)
		{
			chunkArray = chunks.ToArray ();
			Length = chunkArray.Sum (c => c.Length);
		}

		public void AddExtraHeader (string header, string value)
		{
			if (headers == null)
				headers = new Dictionary<string, string> ();
			headers.Add (header, value);
		}

		internal IReadOnlyList<byte[]> CopyChunks ()
		{
			return chunkArray;
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

		static async Task WriteChunk (Stream stream, byte[] chunk, CancellationToken cancellationToken)
		{
			await stream.WriteAsync ($"{chunk.Length:x}\r\n", cancellationToken).ConfigureAwait (false);
			await stream.WriteAsync (chunk, cancellationToken);
			await stream.WriteAsync ("\r\n", cancellationToken);
		}

		static async Task WriteChunkAsBlob (Stream stream, byte[] chunk, CancellationToken cancellationToken)
		{
			using (var ms = new MemoryStream ()) {
				var header = Encoding.UTF8.GetBytes ($"{chunk.Length:x}\r\n");
				var newline = Encoding.UTF8.GetBytes ("\r\n");
				ms.Write (header, 0, header.Length);
				ms.Write (chunk, 0, chunk.Length);
				ms.Write (newline, 0, newline.Length);
				await stream.WriteAsync (ms.ToArray (), cancellationToken).ConfigureAwait (false);
				await stream.FlushAsync ().ConfigureAwait (false);
			}
		}

		async Task WriteChunkTrailer (Stream stream, CancellationToken cancellationToken)
		{
			if (ExtraHeaders == null) {
				await stream.WriteAsync ("0\r\n\r\n", cancellationToken).ConfigureAwait (false);
				return;
			}

			await stream.WriteAsync ("0\r\n").ConfigureAwait (false);
			foreach (var entry in ExtraHeaders) {
				await stream.WriteAsync ($"{entry.Key}: {entry.Value}\r\n", cancellationToken);
			}

			await stream.WriteAsync ("\r\n");
		}

		public override bool HasLength {
			get { return true; }
		}

		public sealed override int Length {
			get;
		}

		public override string AsString ()
		{
			if (contentAsString == null)
				throw new NotSupportedException ();
			return contentAsString;
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
			foreach (var chunk in chunkArray) {
				cancellationToken.ThrowIfCancellationRequested ();
				if (WriteAsBlob)
					await WriteChunkAsBlob (stream, chunk, cancellationToken).ConfigureAwait (false);
				else
					await WriteChunk (stream, chunk, cancellationToken).ConfigureAwait (false);
			}
			cancellationToken.ThrowIfCancellationRequested ();
			await WriteChunkTrailer (stream, cancellationToken).ConfigureAwait (false);
		}
	}
}

