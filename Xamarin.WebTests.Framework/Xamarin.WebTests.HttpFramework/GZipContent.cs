//
// GZipContent.cs
//
// Author:
//       Martin Baulig <martin.baulig@xamarin.com>
//
// Copyright (c) 2018 Xamarin Inc. (http://www.xamarin.com)
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

namespace Xamarin.WebTests.HttpFramework {
	using TestFramework;
	using ConnectionFramework;

	public class GZipContent : HttpContent
	{
		List<byte[]> chunks;
		List<byte []> compressed;

		public GZipContent (params byte[][] chunks)
		{
			this.chunks = new List<byte[]> (chunks);
			Compress ();
		}

		void Compress ()
		{
			if (!DependencyInjector.IsAvailable<IGZipProvider> ())
				throw new NotSupportedException ();
			var provider = DependencyInjector.Get<IGZipProvider> ();
			compressed = new List<byte []> ();
			for (int i = 0; i < chunks.Count; i++) {
				using (var ms = new MemoryStream ()) {
					using (var gzip = provider.Compress (ms, true)) {
						gzip.Write (chunks [i], 0, chunks [i].Length);
					}
					compressed.Add (ms.ToArray ());
				}
			}
		}

		public override bool HasLength => false;

		public override int Length => throw new NotSupportedException ();

		public override string AsString ()
		{
			throw new NotSupportedException ();
		}

		public override byte [] AsByteArray ()
		{
			throw new NotSupportedException ();
		}

		public override void AddHeadersTo (HttpMessage message)
		{
			message.TransferEncoding = "chunked";
			message.AddHeader ("Content-Encoding", "gzip");
		}

		public override async Task WriteToAsync (TestContext ctx, Stream stream, CancellationToken cancellationToken)
		{
			for (int i = 0; i < chunks.Count; i++) {
				await ChunkedContent.WriteChunk (stream, compressed [i], cancellationToken).ConfigureAwait (false);
			}
			await ChunkedContent.WriteChunkTrailer (stream, cancellationToken).ConfigureAwait (false);
		}
	}
}

