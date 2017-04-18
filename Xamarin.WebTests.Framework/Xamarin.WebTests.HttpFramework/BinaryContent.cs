//
// BinaryContent.cs
//
// Author:
//       Martin Baulig <martin.baulig@xamarin.com>
//
// Copyright (c) 2016 Xamarin Inc. (http://www.xamarin.com)
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

namespace Xamarin.WebTests.HttpFramework
{
	public class BinaryContent : HttpContent
	{
		public byte[] Data {
			get;
			private set;
		}

		public BinaryContent (byte[] data)
		{
			Data = data;
		}

		public static BinaryContent CreateRandom (int size)
		{
			var random = new Random ();
			var data = new byte [size];
			random.NextBytes (data);
			return new BinaryContent (data);
		}

		public override bool HasLength {
			get { return true; }
		}

		public override int Length {
			get { return Data.Length; }
		}

		public override string AsString ()
		{
			throw new NotSupportedException ();
		}

		public override byte[] AsByteArray ()
		{
			return Data;
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

		public override Task WriteToAsync (StreamWriter writer)
		{
			throw new NotSupportedException ();
		}

		protected override bool IsNullOrEmpty ()
		{
			return false;
		}

		internal static async Task<HttpContent> Read (StreamReader reader, int length)
		{
			var buffer = new byte[length];
			reader.DiscardBufferedData ();

			int offset = 0;
			while (offset < length) {
				var len = Math.Min (16384, length - offset);
				var ret = await reader.BaseStream.ReadAsync (buffer, offset, len);
				if (ret <= 0)
					throw new InvalidOperationException ();

				offset += ret;
			}

			return new BinaryContent (buffer);
		}

		protected override bool Compare (TestContext ctx, HttpContent actual)
		{
			if (!ctx.Expect (actual.Length, Is.EqualTo (Length), "length"))
				return false;
			if (!ctx.Expect (actual, Is.InstanceOfType (typeof (BinaryContent))))
				return false;

			var actualData = ((BinaryContent)actual).Data;
			return ctx.Expect (actualData, Is.EqualTo (Data), "content");
		}
	}
}

