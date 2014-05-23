//
// HttpResponse.cs
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

namespace Xamarin.WebTests.Server
{
	public class HttpResponse : HttpMessage
	{
		public HttpStatusCode StatusCode {
			get; private set;
		}

		public string StatusMessage {
			get; private set;
		}

		public bool IsSuccess {
			get; private set;
		}

		public HttpResponse (HttpStatusCode status, string body = null)
		{
			Protocol = "HTTP/1.1";
			StatusCode = status;
			StatusMessage = status.ToString ();
			IsSuccess = status == HttpStatusCode.OK;

			if (body != null) {
				AddHeader ("Content-Length", body.Length);
				AddHeader ("Content-Type", "text/plain");
				Body = body;
			}

			AddHeader ("Connection", "close");
		}

		public HttpResponse (Connection connection, StreamReader reader)
			: base (connection, reader)
		{
		}

		protected override void Read ()
		{
			var header = reader.ReadLine ();
			Console.WriteLine ("READ RESPONSE: {0}", header);
			var fields = header.Split (new char[] { ' ' }, StringSplitOptions.None);
			if (fields.Length < 2 || fields.Length > 3) {
				Console.Error.WriteLine ("GOT INVALID HTTP REQUEST: {0}", header);
				throw new InvalidOperationException ();
			}

			Protocol = fields [0];
			StatusCode = (HttpStatusCode)int.Parse (fields [1]);
			StatusMessage = fields.Length == 3 ? fields [2] : string.Empty;

			if (!Protocol.Equals ("HTTP/1.1") && !Protocol.Equals ("HTTP/1.0"))
				throw new InvalidOperationException ();

			ReadHeaders ();

			Body = ReadBody ();
		}

		public void Write (StreamWriter writer)
		{
			var message = StatusMessage ?? ((HttpStatusCode)StatusCode).ToString ();
			writer.WriteLine ("{0} {1} {2}", Protocol, (int)StatusCode, message);
			WriteHeaders (writer);

			if (Body != null)
				writer.WriteLine (Body);
		}

		public static HttpResponse CreateSimple (HttpStatusCode status, string body = null)
		{
			return new HttpResponse (status, body);
		}

		public static HttpResponse CreateRedirect (HttpStatusCode code, Uri uri)
		{
			var response = new HttpResponse (code);
			response.AddHeader ("Location", uri);
			return response;
		}

		public static HttpResponse CreateSuccess (string body = null)
		{
			return new HttpResponse (HttpStatusCode.OK, body);
		}

		public static HttpResponse CreateError (string message, params object[] args)
		{
			return new HttpResponse (HttpStatusCode.InternalServerError, string.Format (message, args));
		}

		public override string ToString ()
		{
			return string.Format ("[HttpResponse: StatusCode={0}, StatusMessage={1}, Body={2}]", StatusCode, StatusMessage, Body);
		}
	}
}

