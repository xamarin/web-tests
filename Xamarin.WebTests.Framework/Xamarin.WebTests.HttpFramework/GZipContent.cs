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
		HttpContent output;

		public GZipContent (params byte[][] chunks)
		{
			Compress (chunks, true);
		}

		public GZipContent (ChunkedContent chunked)
		{
			Compress (chunked.CopyChunks (), true);
		}

		public GZipContent (StringContent content)
		{
			var bytes = Encoding.UTF8.GetBytes (content.AsString ());
			Compress (new byte[][] { bytes }, false);
		}

		void Compress (IReadOnlyList<byte[]> chunks, bool chunked)
		{
			if (!DependencyInjector.IsAvailable<IGZipProvider> ())
				throw new NotSupportedException ();
			var gzipProvider = DependencyInjector.Get<IGZipProvider> ();
			var streamProvider = DependencyInjector.Get<IStreamProvider> ();
			var compressed = new List<byte []> ();
			int position = 0;
			using (var ms = new MemoryStream ()) {
				using (var gzip = gzipProvider.Compress (ms, true)) {
					for (int i = 0; i < chunks.Count; i++) {
						gzip.Write (chunks[i], 0, chunks[i].Length);
						gzip.Flush ();
						CopyBuffer ();
					}
				}

				CopyBuffer ();

				if (chunked)
					output = new ChunkedContent (compressed);
				else
					output = new MultipleBinaryContent (compressed);

				void CopyBuffer ()
				{
					if (ms.Position == position)
						return;
					var msBuffer = streamProvider.GetBuffer (ms);
					var newLength = (int)ms.Position - position;
					var buffer = new byte[newLength];
					Buffer.BlockCopy (msBuffer, position, buffer, 0, newLength);
					position += newLength;
					compressed.Add (buffer);
				}
			}
		}

		public override bool HasLength => output.HasLength;

		public override int Length => output.Length;

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
			message.AddHeader ("Content-Encoding", "gzip");
			output.AddHeadersTo (message);
		}

		public override Task WriteToAsync (TestContext ctx, Stream stream, CancellationToken cancellationToken)
		{
			return output.WriteToAsync (ctx, stream, cancellationToken);
		}
	}
}

