//
// MultipleBinaryContent.cs
//
// Author:
//       Martin Baulig <mabaul@microsoft.com>
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
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;

using Xamarin.AsyncTests;
using Xamarin.AsyncTests.Constraints;

namespace Xamarin.WebTests.HttpFramework
{
	using TestFramework;

	public class MultipleBinaryContent : HttpContent
	{
		public byte[][] Data {
			get;
		}

		public MultipleBinaryContent (ICollection<byte[]> chunks)
		{
			Data = chunks.ToArray ();
			Length = Data.Sum (c => c.Length);
		}

		public override bool HasLength => true;

		public sealed override int Length {
			get;
		}

		public override string AsString ()
		{
			throw new NotSupportedException ();
		}

		public override byte[] AsByteArray ()
		{
			throw new NotSupportedException ();
		}

		public override void AddHeadersTo (HttpMessage message)
		{
			message.ContentType = "application/octet-stream";
			message.ContentLength = Length;
		}

		public override HttpContent RemoveTransferEncoding ()
		{
			return this;
		}

		public override async Task WriteToAsync (TestContext ctx, Stream stream, CancellationToken cancellationToken)
		{
			foreach (var chunk in Data) {
				cancellationToken.ThrowIfCancellationRequested ();
				await stream.WriteAsync (chunk, cancellationToken).ConfigureAwait (false);
			}
		}

		protected override bool IsNullOrEmpty ()
		{
			return false;
		}

		protected override bool Compare (TestContext ctx, HttpContent actual)
		{
			throw new NotSupportedException ();
		}
	}
}
