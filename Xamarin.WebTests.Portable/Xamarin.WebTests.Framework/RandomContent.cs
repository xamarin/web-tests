//
// RandomContent.cs
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

using Xamarin.AsyncTests;
using Xamarin.AsyncTests.Constraints;

namespace Xamarin.WebTests.Framework
{
	public class RandomContent : HttpContent
	{
		public bool SendChunked {
			get;
			private set;
		}

		public int MaxChunks {
			get;
			private set;
		}

		public int MinChunkSize {
			get;
			private set;
		}

		public int MaxChunkSize {
			get;
			private set;
		}

		public RandomContent (int minChunkSize, int maxChunkSize)
		{
			MinChunkSize = MinChunkSize;
			MaxChunkSize = maxChunkSize;

			Initialize ();
		}

		public RandomContent (int maxChunks, int minChunkSize, int maxChunkSize)
		{
			SendChunked = true;
			MaxChunks = maxChunks;
			MinChunkSize = minChunkSize;
			MaxChunkSize = maxChunkSize;

			Initialize ();
		}

		Random random;
		int[] chunkSizes;
		int totalSize;

		void Initialize ()
		{
			random = new Random ();

			int countChunks = 1;
			if (SendChunked)
				countChunks += random.Next (MaxChunks);

			chunkSizes = new int [countChunks];
			for (int i = 0; i < countChunks; i++) {
				chunkSizes [i] = MinChunkSize + random.Next (MaxChunkSize - MinChunkSize);
				
				totalSize += chunkSizes [i] * 2;
			}
		}

		public override int Length {
			get { return totalSize; }
		}

		public override string AsString ()
		{
			throw new NotImplementedException ();
		}

		public override void AddHeadersTo (HttpMessage message)
		{
			if (SendChunked)
				message.SetHeader ("Transfer-Encoding", "chunked");
		}

		static readonly char[] hexchars = new char[] {
			'0', '1', '2', '3', '4', '5', '6', '7', '8', '9', 'A', 'B', 'C', 'D', 'E', 'F'
		};

		string GetChunk (int size)
		{
			var bytes = new byte [size];
			random.NextBytes (bytes);

			var chars = new char [size * 2];
			for (int i = 0; i < size; i++) {
				int hi = bytes [i] >> 8;
				int lo = bytes [i] & 15;
				chars [i << 1] = hexchars [hi];
				chars [(i << 1) + 1] = hexchars [lo];
			}
			return new string (chars);
		}

		public override HttpContent RemoveTransferEncoding ()
		{
			if (SendChunked)
				throw new InvalidOperationException ();

			return this;
		}

		public override async Task WriteToAsync (StreamWriter writer)
		{
			if (!SendChunked) {
				await writer.WriteAsync (GetChunk (chunkSizes [0]));
				return;
			}

			for (int i = 0; i < chunkSizes.Length; i++) {
				var chunk = GetChunk (chunkSizes [i]);
				await writer.WriteAsync (string.Format ("{0:x}\r\n{1}\r\n", chunk.Length, chunk));
			}
			await writer.WriteAsync ("0\r\n\r\n\r\n");
		}

		protected override bool IsNullOrEmpty ()
		{
			return false;
		}

		protected override bool Compare (TestContext ctx, HttpContent actual)
		{
			return ctx.Expect (actual.Length, Is.EqualTo (Length));
		}
	}
}

