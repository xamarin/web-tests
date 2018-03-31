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
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xamarin.AsyncTests;

namespace Xamarin.WebTests.HttpFramework
{
	using TestFramework;
	using HttpHandlers;
	using Server;

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

		internal HttpListenerResponse HttpListenerResponse {
			get;
		}

		public bool? KeepAlive {
			get { return keepAlive; }
			set {
				if (headersResolved)
					throw new InvalidOperationException ();
				if (!value.HasValue)
					throw new InvalidOperationException ();
				keepAlive = value.Value;
			}
		}

		public bool CloseConnection {
			get { return closeConnection; }
			set {
				if (headersResolved)
					throw new InvalidOperationException ();
				closeConnection = value;
			}
		}

		public bool WriteAsBlob {
			get { return writeAsBlob; }
			set {
				if (headersResolved)
					throw new InvalidOperationException ();
				writeAsBlob = value;
			}
		}

		public bool WriteBodyAsBlob {
			get { return writeBodyAsBlob; }
			set {
				if (headersResolved)
					throw new InvalidOperationException ();
				writeBodyAsBlob = value;
				if (value)
					writeAsBlob = true;
			}
		}

		public bool NoContentLength {
			get { return noContentLength; }
			set {
				if (headersResolved)
					throw new InvalidOperationException ();
				noContentLength = value;
			}
		}

		internal ListenerOperation Redirect {
			get { return redirect; }
			set {
				if (headersResolved)
					throw new InvalidOperationException ();
				redirect = value;
			}
		}

		bool? keepAlive;
		bool closeConnection;
		bool headersResolved;
		bool writeAsBlob;
		bool writeBodyAsBlob;
		bool noContentLength;
		ListenerOperation redirect;

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
		{
		}

		HttpResponse (HttpListenerResponse response)
		{
			HttpListenerResponse = response;
		}

		internal static async Task<HttpResponse> Read (TestContext ctx, HttpStreamReader reader, CancellationToken cancellationToken)
		{
			cancellationToken.ThrowIfCancellationRequested ();
			try {
				var response = new HttpResponse ();
				await response.InternalRead (ctx, reader, cancellationToken).ConfigureAwait (false);
				return response;
			} catch (Exception ex) {
				return CreateError ("Failed to read response", ex);
			}
		}

		public bool IsRedirect {
			get {
				switch (StatusCode) {
				case HttpStatusCode.Moved:
				case HttpStatusCode.Redirect:
				case HttpStatusCode.SeeOther:
				case HttpStatusCode.TemporaryRedirect:
				case HttpStatusCode.Unauthorized:
				case HttpStatusCode.ProxyAuthenticationRequired:
					return true;
				default:
					return false;
				}
			}
		}

		bool NeedsContentLength {
			get {
				switch (StatusCode) {
				case HttpStatusCode.Moved:
				case HttpStatusCode.Redirect:
				case HttpStatusCode.SeeOther:
				case HttpStatusCode.TemporaryRedirect:
				case HttpStatusCode.OK:
					return true;
				default:
					return false;
				}
			}
		}

		internal void ResolveHeaders ()
		{
			if (headersResolved)
				return;
			headersResolved = true;
			if (Body != null) {
				Body.AddHeadersTo (this);
				if (!Body.HasLength)
					noContentLength = true;
			}
			if (Headers.TryGetValue ("Transfer-Encoding", out string transferEncoding) &&
			    string.Equals (transferEncoding, "chunked", StringComparison.OrdinalIgnoreCase))
				noContentLength = true;
			if (ContentLength == null && NeedsContentLength && !NoContentLength)
				ContentLength = 0;

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

		async Task InternalRead (TestContext ctx, HttpStreamReader reader, CancellationToken cancellationToken)
		{
			cancellationToken.ThrowIfCancellationRequested ();
			var header = await reader.ReadLineAsync (cancellationToken).ConfigureAwait (false);
			var fields = header.Split (new char[] { ' ' }, StringSplitOptions.None);
			if (fields.Length < 2 || fields.Length > 3)
				throw new InvalidOperationException ();

			Protocol = ProtocolFromString (fields [0]);
			StatusCode = (HttpStatusCode)int.Parse (fields [1]);
			StatusMessage = fields.Length == 3 ? fields [2] : string.Empty;

			cancellationToken.ThrowIfCancellationRequested ();
			await ReadHeaders (ctx, reader, cancellationToken);

			cancellationToken.ThrowIfCancellationRequested ();
			Body = await ReadBody (ctx, reader, false, cancellationToken);
		}

		public async Task Write (TestContext ctx, Stream stream, CancellationToken cancellationToken)
		{
			ResolveHeaders ();

			cancellationToken.ThrowIfCancellationRequested ();

			var message = StatusMessage ?? ((HttpStatusCode)StatusCode).ToString ();
			var headerLine = $"{ProtocolToString (Protocol)} {(int)StatusCode} {message}\r\n";

			bool bodyWritten = false;

			if (WriteAsBlob) {
				var sb = new StringBuilder ();
				sb.Append (headerLine);
				foreach (var entry in Headers)
					sb.Append ($"{entry.Key}: {entry.Value}\r\n");
				sb.Append ("\r\n");

				var ms = new MemoryStream ();
				var blob = Encoding.UTF8.GetBytes (sb.ToString ());
				ms.Write (blob, 0, blob.Length);

				if (WriteBodyAsBlob || Body is StringContent) {
					await Body.WriteToAsync (ctx, ms, cancellationToken).ConfigureAwait (false);
					bodyWritten = true;
				}

				var buffer = ms.ToArray ();
				await stream.WriteAsync (buffer, 0, buffer.Length).ConfigureAwait (false);
				await stream.FlushAsync ();
			} else {
				await stream.WriteAsync (headerLine, cancellationToken).ConfigureAwait (false);
				await WriteHeaders (stream, cancellationToken);
			}

			if (!bodyWritten && Body != null) {
				cancellationToken.ThrowIfCancellationRequested ();
				await Body.WriteToAsync (ctx, stream, cancellationToken);
			}
			await stream.FlushAsync ();
		}

		public void RegisterRedirect (
			TestContext ctx, HttpOperation operation, string path = null)
		{
			if (headersResolved)
				throw new InvalidOperationException ();
			redirect = operation.RegisterRedirect (ctx, operation.ListenerHandler, path);
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

		public static HttpResponse CreateRedirect (
			TestContext ctx, HttpStatusCode code, HttpOperation operation,
			Handler target, string path = null)
		{
			var redirect = operation.RegisterRedirect (ctx, target, path);
			var response = CreateRedirect (code, redirect.Uri);
			response.redirect = redirect;
			return response;
		}

		internal static HttpResponse CreateRedirect (HttpStatusCode code, ListenerOperation redirect)
		{
			var response = CreateRedirect (code, redirect.Uri);
			response.redirect = redirect;
			return response;
		}

		public static HttpResponse CreateFrom (HttpListenerResponse response)
		{
			return new HttpResponse (response);
		}

		public static HttpResponse CreateSuccess (string body = null)
		{
			return new HttpResponse (HttpStatusCode.OK, body != null ? new StringContent (body) : null);
		}

		public static HttpResponse CreateError (string message)
		{
			return new HttpResponse (HttpStatusCode.InternalServerError, new StringContent (message));
		}

		public static HttpResponse CreateError (string message, Exception error)
		{
			return new HttpResponse (HttpStatusCode.InternalServerError, new StringContent ($"Got exception: {error}"));
		}

		public override string ToString ()
		{
			return string.Format ("[HttpResponse: StatusCode={0}, StatusMessage={1}, Body={2}]", StatusCode, StatusMessage, Body);
		}
	}
}

