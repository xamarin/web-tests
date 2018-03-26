//
// TraditionalResponse.cs
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
using System.Net;
using System.Threading;
using System.Threading.Tasks;

using Xamarin.AsyncTests;

namespace Xamarin.WebTests.HttpHandlers
{
	using ConnectionFramework;
	using HttpFramework;

	public class TraditionalResponse : Response
	{
		public HttpWebResponse Response {
			get;
		}

		public TraditionalResponse (
			TraditionalRequest request, HttpWebResponse response,
			HttpContent content, Exception error = null)
			: base (request)
		{
			Response = response;
			Status = response.StatusCode;
			Content = content;
			Error = error;

			// FIXME: .NET does not allow accessing StatusCode after the
			// response has been disposed.
			var contentType = response.ContentType;
			if (string.Equals (contentType, "SHOULD-NEVER-HAPPEN"))
				throw new InvalidOperationException ();
		}

		public TraditionalResponse (
			TraditionalRequest request, HttpStatusCode status,
			Exception error)
			: base (request)
		{
			Status = status;
			Error = error;

			if (error is WebException webException)
				Response = (HttpWebResponse)webException.Response;
		}

		public sealed override HttpStatusCode Status {
			get;
		}

		public override bool IsSuccess => Error == null;

		public sealed override Exception Error {
			get;
		}

		public sealed override HttpContent Content {
			get;
		}

		protected override void Close ()
		{
			Response?.Dispose ();
			base.Close ();
		}
	}
}
