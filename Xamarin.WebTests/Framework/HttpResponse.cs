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

namespace Xamarin.WebTests.Framework
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
			get { return StatusCode == HttpStatusCode.OK; }
		}

		public bool? KeepAlive {
			get { return keepAlive; }
			set {
				if (responseWritten)
					throw new InvalidOperationException ();
				if (!value.HasValue)
					throw new InvalidOperationException ();
				keepAlive = value.Value;
			}
		}

		bool? keepAlive;
		bool responseWritten;

		public HttpResponse (HttpStatusCode status, HttpContent content = null)
		{
			Protocol = HttpProtocol.Http11;
			StatusCode = status;
			StatusMessage = status.ToString ();
			Body = content;
		}

		public HttpResponse (HttpStatusCode status, HttpProtocol protocol, string statusMessage, HttpContent content = null)
		{
			Protocol = protocol;
			StatusCode = status;
			StatusMessage = statusMessage;
			Body = content;
		}

		public HttpResponse (Connection connection, StreamReader reader)
			: base (connection, reader)
		{
		}

		void CheckHeaders ()
		{
			if (Body != null)
				Body.AddHeadersTo (this);

			if (Protocol == HttpProtocol.Http11 && !Headers.ContainsKey ("Connection"))
				AddHeader ("Connection", (KeepAlive ?? false) ? "keep-alive" : "close");
		}

		protected override void Read ()
		{
			var header = reader.ReadLine ();
			var fields = header.Split (new char[] { ' ' }, StringSplitOptions.None);
			if (fields.Length < 2 || fields.Length > 3)
				throw new InvalidOperationException ();

			Protocol = ProtocolFromString (fields [0]);
			StatusCode = (HttpStatusCode)int.Parse (fields [1]);
			StatusMessage = fields.Length == 3 ? fields [2] : string.Empty;

			ReadHeaders ();

			Body = ReadBody ();
		}

		public void Write (StreamWriter writer)
		{
			CheckHeaders ();
			responseWritten = true;

			var message = StatusMessage ?? ((HttpStatusCode)StatusCode).ToString ();
			writer.Write ("{0} {1} {2}\r\n", ProtocolToString (Protocol), (int)StatusCode, message);
			WriteHeaders (writer);

			if (Body != null)
				Body.WriteToAsync (writer).Wait ();
			writer.Flush ();
		}

		public static HttpResponse CreateSimple (HttpStatusCode status, string body = null)
		{
			return new HttpResponse (status, body != null ? new StringContent (body) : null);
		}

		public static HttpResponse CreateRedirect (HttpStatusCode code, Uri uri)
		{
			var response = new HttpResponse (code);
			response.AddHeader ("Location", uri);
			return response;
		}

		public static HttpResponse CreateSuccess (string body = null)
		{
			return new HttpResponse (HttpStatusCode.OK, body != null ? new StringContent (body) : null);
		}

		// [Obsolete]
		public static HttpResponse CreateError (string message, params object[] args)
		{
			return new HttpResponse (HttpStatusCode.InternalServerError, new StringContent (string.Format (message, args)));
		}

		public override string ToString ()
		{
			return string.Format ("[HttpResponse: StatusCode={0}, StatusMessage={1}, Body={2}]", StatusCode, StatusMessage, Body);
		}
	}
}

