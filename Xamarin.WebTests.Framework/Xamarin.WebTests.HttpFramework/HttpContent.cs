//
// HttpContent.cs
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
using System.Threading.Tasks;

using Xamarin.AsyncTests;
using Xamarin.AsyncTests.Framework;
using Xamarin.AsyncTests.Constraints;

namespace Xamarin.WebTests.HttpFramework
{
	public abstract class HttpContent
	{
		public static readonly HttpContent HelloWorld = new StringContent ("Hello World!");
		public static readonly HttpContent HelloChunked = new ChunkedContent ("Hello Chunked World!");

		public abstract bool HasLength {
			get;
		}

		public abstract int Length {
			get;
		}

		public abstract string AsString ();

		public abstract byte[] AsByteArray ();

		public virtual HttpContent RemoveTransferEncoding ()
		{
			return new StringContent (AsString ());
		}

		public abstract void AddHeadersTo (HttpMessage message);

		public abstract Task WriteToAsync (StreamWriter writer);

		public static bool IsNullOrEmpty (HttpContent content)
		{
			if (content == null)
				return true;
			return content.IsNullOrEmpty ();
		}

		protected virtual bool IsNullOrEmpty ()
		{
			return string.IsNullOrEmpty (AsString ());
		}

		protected virtual bool Compare (TestContext ctx, HttpContent actual)
		{
			var actualString = actual.AsString ();
			var expectedString = AsString ();
			return ctx.Expect (actualString, Is.EqualTo (expectedString));
		}

		public static bool Compare (TestContext ctx, HttpContent actual, HttpContent expected,
			bool ignoreType, string message = null)
		{
			if (expected == null)
				return ctx.Expect (IsNullOrEmpty (actual), Is.True, message);
			if (!ctx.Expect (actual, Is.Not.Null))
				return false;

			if (!ignoreType && !ctx.Expect (actual, Is.InstanceOfType (expected.GetType ())))
				return false;

			return expected.Compare (ctx, actual);
		}
	}
}

