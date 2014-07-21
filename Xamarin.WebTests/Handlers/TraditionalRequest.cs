//
// TraditionalRequest.cs
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
using System.Threading.Tasks;

namespace Xamarin.WebTests.Handlers
{
	public class TraditionalRequest : Request
	{
		public readonly HttpWebRequest Request;

		public string Body {
			get; set;
		}

		public TraditionalRequest (Uri uri)
		{
			Request = (HttpWebRequest)HttpWebRequest.Create (uri);
			Request.KeepAlive = true;
		}

		#region implemented abstract members of Request

		public override void SetCredentials (ICredentials credentials)
		{
			Request.Credentials = credentials;
		}

		public override void SetProxy (IWebProxy proxy)
		{
			Request.Proxy = proxy;
		}

		public override async Task<Response> Send (CancellationToken cancellationToken)
		{
			var cts = CancellationTokenSource.CreateLinkedTokenSource (cancellationToken);
			cts.Token.Register (() => Request.Abort ());

			var task = Task.Factory.StartNew (() => {
				return Send ();
			});

			try {
				return await task;
			} finally {
				cts.Dispose ();
			}
		}

		Response FromHttpResponse (HttpWebResponse response, WebException error = null)
		{
			string content;
			var status = response.StatusCode;
			using (var reader = new StreamReader (response.GetResponseStream ())) {
				content = reader.ReadToEnd ();
			}
			response.Close ();

			return new SimpleResponse (this, status, content, error);
		}

		Response Send ()
		{
			try {
				if (Body != null) {
					using (var writer = new StreamWriter (Request.GetRequestStream ())) {
						if (!string.IsNullOrEmpty (Body))
							writer.Write (Body);
					}
				}

				var response = (HttpWebResponse)Request.GetResponse ();
				return FromHttpResponse (response);
			} catch (WebException wexc) {
				var response = (HttpWebResponse)wexc.Response;
				if (response == null)
					return new SimpleResponse (this, HttpStatusCode.InternalServerError, null, wexc);

				return FromHttpResponse (response, wexc);
			} catch (Exception ex) {
				return new SimpleResponse (this, HttpStatusCode.InternalServerError, null, ex);
			}
		}

		#endregion
	}
}

