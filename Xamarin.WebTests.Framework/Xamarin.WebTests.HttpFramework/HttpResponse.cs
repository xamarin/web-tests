﻿//
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
using System.Threading;
using System.Threading.Tasks;

namespace Xamarin.WebTests.HttpFramework
{
	public class HttpResponse : HttpMessage
	{
		public HttpStatusCode StatusCode {
			get; private set;
		}

		public string StatusMessage {
			get; private set;
		}

		public HttpContent Body {
			get;
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

		HttpResponse ()
			: base ()
		{
		}

		internal static async Task<HttpResponse> Read (StreamReader reader, CancellationToken cancellationToken)
		{
			cancellationToken.ThrowIfCancellationRequested ();
			try {
				var response = new HttpResponse ();
				await response.InternalRead (reader, cancellationToken).ConfigureAwait (false);
				return response;
			} catch (Exception ex) {
				return CreateError (ex);
			}
		}

		void CheckHeaders ()
		{
			if (Body != null)
				Body.AddHeadersTo (this);

			if (Protocol == HttpProtocol.Http11 && !Headers.ContainsKey ("Connection"))
				AddHeader ("Connection", (KeepAlive ?? false) ? "keep-alive" : "close");
		}

		public static bool ParseResponseHeader (string header, out HttpProtocol protocol, out HttpStatusCode status)
		{
			var fields = header.Split (new char[] { ' ' }, StringSplitOptions.None);
			if (fields.Length < 2) {
				protocol = HttpProtocol.Http10;
				status = HttpStatusCode.InternalServerError;
				return false;
			}

			try {
				protocol = ProtocolFromString (fields [0]);
				status = (HttpStatusCode)int.Parse (fields [1]);
				return true;
			} catch {
				protocol = HttpProtocol.Http10;
				status = HttpStatusCode.InternalServerError;
				return false;
			}
		}

		async Task InternalRead (StreamReader reader, CancellationToken cancellationToken)
		{
			cancellationToken.ThrowIfCancellationRequested ();
			var header = await reader.ReadLineAsync ();
			var fields = header.Split (new char[] { ' ' }, StringSplitOptions.None);
			if (fields.Length < 2 || fields.Length > 3)
				throw new InvalidOperationException ();

			Protocol = ProtocolFromString (fields [0]);
			StatusCode = (HttpStatusCode)int.Parse (fields [1]);
			StatusMessage = fields.Length == 3 ? fields [2] : string.Empty;

			cancellationToken.ThrowIfCancellationRequested ();
			await ReadHeaders (reader, cancellationToken);
		}

		public async Task Write (StreamWriter writer, CancellationToken cancellationToken)
		{
			CheckHeaders ();
			responseWritten = true;

			cancellationToken.ThrowIfCancellationRequested ();

			var message = StatusMessage ?? ((HttpStatusCode)StatusCode).ToString ();
			await writer.WriteAsync (string.Format ("{0} {1} {2}\r\n", ProtocolToString (Protocol), (int)StatusCode, message));
			await WriteHeaders (writer, cancellationToken);

			if (Body != null)
				await Body.WriteToAsync (writer);
			await writer.FlushAsync ();
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

		public static HttpResponse CreateError (string message, params object[] args)
		{
			return new HttpResponse (HttpStatusCode.InternalServerError, new StringContent (string.Format (message, args)));
		}

		public static HttpResponse CreateError (Exception error)
		{
			return new HttpResponse (HttpStatusCode.InternalServerError, new StringContent (string.Format ("Got exception: {0}", error)));
		}

		public override string ToString ()
		{
			return string.Format ("[HttpResponse: StatusCode={0}, StatusMessage={1}, Body={2}]", StatusCode, StatusMessage, Body);
		}
	}
}

