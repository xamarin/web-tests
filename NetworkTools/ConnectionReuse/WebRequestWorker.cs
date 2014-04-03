//
// WebRequestWorker.cs
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
using System.Net;
using System.Threading;

namespace ConnectionReuse
{
	public class WebRequestWorker : BaseWorker
	{
		Uri uri;

		public WebRequestWorker (Uri uri)
		{
			this.uri = uri;
		}

		public Uri Uri {
			get { lock (this) return uri; }
			set {
				lock (this) {
					uri = new Uri (value.AbsoluteUri);
				}
			}
		}

		protected override string DoRequest (CancellationToken token)
		{
			Uri requestUri;
			lock (this) {
				requestUri = new Uri (uri.AbsoluteUri);
			}

			var request = WebRequest.Create (requestUri) as HttpWebRequest;
			var ar = request.BeginGetResponse (null, null);
			ar.AsyncWaitHandle.WaitOne ();

			using (var response = (HttpWebResponse)request.EndGetResponse (ar)) {
				if (response.StatusCode != HttpStatusCode.OK)
					throw new InvalidOperationException ();
				using (var stream = new StreamReader (response.GetResponseStream ()))
					return stream.ReadToEnd ();
			}
		}
	}
}

