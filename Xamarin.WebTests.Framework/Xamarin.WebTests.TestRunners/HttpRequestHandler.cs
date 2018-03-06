//
// HttpRequestHandler.cs
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
using System.Text;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using System.Collections;
using System.Collections.Generic;
using Xamarin.AsyncTests;
using Xamarin.AsyncTests.Constraints;
using Xamarin.AsyncTests.Portable;

namespace Xamarin.WebTests.TestRunners
{
	using ConnectionFramework;
	using HttpFramework;
	using HttpHandlers;
	using TestFramework;
	using Resources;
	using Xamarin.WebTests.Server;

	public class HttpRequestHandler : Handler
	{
		public HttpRequestTestRunner TestRunner {
			get;
		}

		public bool CloseConnection {
			get;
		}

		public HttpContent Content {
			get;
		}

		public HttpContent ExpectedContent {
			get;
		}

		public IPEndPoint RemoteEndPoint {
			get;
			private set;
		}

		public Handler Target {
			get;
		}

		public HttpOperationFlags OperationFlags {
			get;
		}

		public string ME {
			get;
		}

		TaskCompletionSource<bool> readyTcs;

		public HttpRequestHandler (HttpRequestTestRunner parent)
			: base (parent.EffectiveType.ToString ())
		{
			TestRunner = parent;
			ME = $"{GetType ().Name}({parent.EffectiveType})";
			readyTcs = new TaskCompletionSource<bool> ();
			Flags = RequestFlags.KeepAlive;

			switch (parent.EffectiveType) {
			case HttpRequestTestType.LargeHeader:
			case HttpRequestTestType.LargeHeader2:
			case HttpRequestTestType.SendResponseAsBlob:
				Content = ConnectionHandler.TheQuickBrownFoxContent;
				CloseConnection = true;
				break;
			case HttpRequestTestType.CloseRequestStream:
				OperationFlags = HttpOperationFlags.AbortAfterClientExits;
				CloseConnection = false;
				break;
			case HttpRequestTestType.ReadTimeout:
				CloseConnection = false;
				break;
			case HttpRequestTestType.RedirectNoLength:
				Target = new HelloWorldHandler (ME);
				OperationFlags |= HttpOperationFlags.RedirectOnNewConnection;
				CloseConnection = false;
				break;
			case HttpRequestTestType.PutChunked:
			case HttpRequestTestType.PutChunkDontCloseRequest:
				CloseConnection = true;
				break;
			case HttpRequestTestType.ServerAbortsRedirect:
				OperationFlags = HttpOperationFlags.ServerAbortsRedirection;
				CloseConnection = false;
				break;
			case HttpRequestTestType.ServerAbortsPost:
				OperationFlags = HttpOperationFlags.ServerAbortsRedirection;
				CloseConnection = true;
				break;
			case HttpRequestTestType.PostChunked:
				OperationFlags = HttpOperationFlags.DontReadRequestBody;
				CloseConnection = false;
				break;
			case HttpRequestTestType.EntityTooBig:
			case HttpRequestTestType.ClientAbortsPost:
				OperationFlags = HttpOperationFlags.AbortAfterClientExits | HttpOperationFlags.DontReadRequestBody;
				CloseConnection = false;
				break;
			case HttpRequestTestType.PostContentLength:
				OperationFlags = HttpOperationFlags.DontReadRequestBody;
				break;
			case HttpRequestTestType.SimpleGZip:
				Content = HttpContent.TheQuickBrownFox;
				CloseConnection = true;
				break;
			case HttpRequestTestType.TestResponseStream:
				Content = new StringContent ("AAAA");
				CloseConnection = true;
				break;
			case HttpRequestTestType.LargeChunkRead:
				Content = HttpContent.TheQuickBrownFoxChunked;
				ExpectedContent = Content.RemoveTransferEncoding ();
				CloseConnection = false;
				break;
			case HttpRequestTestType.LargeGZipRead:
				Content = ConnectionHandler.GetLargeChunkedContent (16384);
				ExpectedContent = Content.RemoveTransferEncoding ();
				CloseConnection = false;
				break;
			case HttpRequestTestType.GZipWithLength:
				Content = ConnectionHandler.GetLargeStringContent (16384);
				ExpectedContent = Content;
				CloseConnection = false;
				break;
			case HttpRequestTestType.ResponseStreamCheckLength2:
				Content = HttpContent.HelloChunked;
				ExpectedContent = Content.RemoveTransferEncoding ();
				CloseConnection = false;
				break;
			case HttpRequestTestType.ResponseStreamCheckLength:
				Content = HttpContent.HelloWorld;
				ExpectedContent = Content;
				CloseConnection = false;
				break;
			case HttpRequestTestType.GetNoLength:
				ExpectedContent = ConnectionHandler.TheQuickBrownFoxContent;
				CloseConnection = false;
				break;
			case HttpRequestTestType.ImplicitHost:
			case HttpRequestTestType.CustomHost:
			case HttpRequestTestType.CustomHostWithPort:
			case HttpRequestTestType.CustomHostDefaultPort:
				CloseConnection = false;
				break;
			default:
				throw new NotSupportedException (parent.EffectiveType.ToString ());
			}

			if (CloseConnection)
				Flags |= RequestFlags.CloseConnection;

			if (ExpectedContent == null)
				ExpectedContent = Content ?? new StringContent (ME);
		}

		HttpRequestHandler (HttpRequestHandler other)
			: base (other.Value)
		{
			TestRunner = other.TestRunner;
			Content = other.Content;
			CloseConnection = CloseConnection;
			ME = other.ME;
			Flags = other.Flags;
			Target = other.Target;
			readyTcs = new TaskCompletionSource<bool> ();
		}

		HttpRequestRequest currentRequest;
		bool isSecondRequest;

		public override object Clone ()
		{
			return new HttpRequestHandler (this);
		}

		public Request CreateRequest (Uri uri)
		{
			switch (TestRunner.EffectiveType) {
			case HttpRequestTestType.CloseRequestStream:
			case HttpRequestTestType.ReadTimeout:
				return new HttpRequestRequest (this, uri);

			case HttpRequestTestType.PutChunked:
			case HttpRequestTestType.PutChunkDontCloseRequest:
				return new HttpRequestRequest (this, uri) {
					Content = ConnectionHandler.GetLargeStringContent (50)
				};
			case HttpRequestTestType.PostChunked:
			case HttpRequestTestType.EntityTooBig:
			case HttpRequestTestType.PostContentLength:
			case HttpRequestTestType.ClientAbortsPost:
			case HttpRequestTestType.SimpleGZip:
			case HttpRequestTestType.TestResponseStream:
			case HttpRequestTestType.LargeChunkRead:
			case HttpRequestTestType.LargeGZipRead:
			case HttpRequestTestType.GZipWithLength:
			case HttpRequestTestType.ResponseStreamCheckLength2:
			case HttpRequestTestType.ResponseStreamCheckLength:
			case HttpRequestTestType.GetNoLength:
				return new HttpRequestRequest (this, uri);
			default:
				return new TraditionalRequest (uri);
			}
		}

		public override void ConfigureRequest (TestContext ctx, Request request, Uri uri)
		{
			if (request is HttpRequestRequest instrumentationRequest) {
				if (Interlocked.CompareExchange (ref currentRequest, instrumentationRequest, null) != null)
					throw new InvalidOperationException ();
			}

			switch (TestRunner.EffectiveType) {
			case HttpRequestTestType.ReadTimeout:
				currentRequest.RequestExt.ReadWriteTimeout = 100;
				break;

			case HttpRequestTestType.PutChunked:
			case HttpRequestTestType.PutChunkDontCloseRequest:
				request.Method = "PUT";
				request.SetContentType ("application/octet-stream");
				request.SetContentLength (request.Content.Length);
				request.SendChunked ();
				break;

			case HttpRequestTestType.ServerAbortsPost:
				request.Method = "POST";
				request.SetContentType ("application/x-www-form-urlencoded");
				request.Content = new FormContent (("foo", "bar"), ("hello", "world"), ("escape", "this needs % escaping"));
				break;

			case HttpRequestTestType.EntityTooBig:
			case HttpRequestTestType.PostContentLength:
			case HttpRequestTestType.ClientAbortsPost:
				request.Method = "POST";
				request.SetContentType ("text/plain");
				request.SetContentLength (request.Content.Length);
				break;

			case HttpRequestTestType.SimpleGZip:
				break;

			case HttpRequestTestType.ImplicitHost:
				break;

			case HttpRequestTestType.CustomHost:
				((TraditionalRequest)request).RequestExt.Host = "custom";
				break;

			case HttpRequestTestType.CustomHostWithPort:
				((TraditionalRequest)request).RequestExt.Host = "custom:8888";
				break;

			case HttpRequestTestType.CustomHostDefaultPort:
				var defaultPort = TestRunner.Server.UseSSL ? 443 : 80;
				((TraditionalRequest)request).RequestExt.Host = $"custom:{defaultPort}";
				break;
			}

			base.ConfigureRequest (ctx, request, uri);
		}

		async Task<HttpResponse> HandlePostChunked (
			TestContext ctx, HttpOperation operation, HttpConnection connection, HttpRequest request,
			RequestFlags effectiveFlags, CancellationToken cancellationToken)
		{
			var me = $"{ME}.{nameof (HandlePostChunked)}";
			ctx.LogDebug (3, $"{me}: {connection.RemoteEndPoint}");

			var firstChunk = await ChunkedContent.ReadChunk (ctx, request.Reader, cancellationToken).ConfigureAwait (false);
			ctx.LogDebug (3, $"{me} got first chunk: {firstChunk.Length}");

			ctx.Assert (firstChunk, Is.EqualTo (ConnectionHandler.TheQuickBrownFoxBuffer), "first chunk");

			readyTcs.TrySetResult (true);

			ctx.LogDebug (3, $"{me} reading remaining body");

			await ChunkedContent.Read (ctx, request.Reader, cancellationToken).ConfigureAwait (false);

			return HttpResponse.CreateSuccess (ME);
		}

		internal Task WaitUntilReady ()
		{
			return readyTcs.Task;
		}

		protected internal override async Task<HttpResponse> HandleRequest (
			TestContext ctx, HttpOperation operation, HttpConnection connection, HttpRequest request,
			RequestFlags effectiveFlags, CancellationToken cancellationToken)
		{
			switch (TestRunner.EffectiveType) {
			case HttpRequestTestType.LargeHeader:
			case HttpRequestTestType.LargeHeader2:
			case HttpRequestTestType.SendResponseAsBlob:
			case HttpRequestTestType.CloseRequestStream:
			case HttpRequestTestType.ReadTimeout:
			case HttpRequestTestType.SimpleGZip:
			case HttpRequestTestType.TestResponseStream:
			case HttpRequestTestType.LargeChunkRead:
			case HttpRequestTestType.LargeGZipRead:
			case HttpRequestTestType.GZipWithLength:
			case HttpRequestTestType.ResponseStreamCheckLength2:
			case HttpRequestTestType.ResponseStreamCheckLength:
			case HttpRequestTestType.GetNoLength:
				ctx.Assert (request.Method, Is.EqualTo ("GET"), "method");
				break;

			case HttpRequestTestType.ServerAbortsPost:
				ctx.Assert (request.Method, Is.EqualTo ("POST"), "method");
				break;

			case HttpRequestTestType.RedirectNoLength:
			case HttpRequestTestType.PutChunked:
			case HttpRequestTestType.PutChunkDontCloseRequest:
			case HttpRequestTestType.ServerAbortsRedirect:
				break;

			case HttpRequestTestType.EntityTooBig:
				await EntityTooBig ().ConfigureAwait (false);
				return null;

			case HttpRequestTestType.PostChunked:
				return await HandlePostChunked (
					ctx, operation, connection, request, effectiveFlags, cancellationToken).ConfigureAwait (false);

			case HttpRequestTestType.PostContentLength:
				await PostContentLength ().ConfigureAwait (false);
				break;

			case HttpRequestTestType.ClientAbortsPost:
				await ClientAbortsPost ().ConfigureAwait (false);
				return null;

			case HttpRequestTestType.ImplicitHost:
				var hostAndPort = TestRunner.Uri.GetComponents (UriComponents.HostAndPort, UriFormat.Unescaped);
				ctx.Assert (request.Headers["Host"], Is.EqualTo (hostAndPort), "host");
				break;

			case HttpRequestTestType.CustomHost:
				ctx.Assert (request.Headers["Host"], Is.EqualTo ("custom"), "host");
				break;

			case HttpRequestTestType.CustomHostWithPort:
				ctx.Assert (request.Headers["Host"], Is.EqualTo ("custom:8888"), "host");
				break;

			case HttpRequestTestType.CustomHostDefaultPort:
				var defaultPort = TestRunner.Server.UseSSL ? 443 : 80;
				ctx.Assert (request.Headers["Host"], Is.EqualTo ($"custom:{defaultPort}"), "host");
				break;

			default:
				throw ctx.AssertFail (TestRunner.EffectiveType);
			}

			RemoteEndPoint = connection.RemoteEndPoint;

			HttpResponse response;
			HttpRequestContent content;
			ListenerOperation redirect;

			switch (TestRunner.EffectiveType) {
			case HttpRequestTestType.LargeHeader:
				response = new HttpResponse (HttpStatusCode.OK, Content);
				response.AddHeader ("LargeTest", ConnectionHandler.GetLargeText (100));
				return response;

			case HttpRequestTestType.LargeHeader2:
				response = new HttpResponse (HttpStatusCode.OK, Content);
				response.AddHeader ("LargeTest", ConnectionHandler.GetLargeText (100));
				response.WriteAsBlob = true;
				return response;

			case HttpRequestTestType.SendResponseAsBlob:
				return new HttpResponse (HttpStatusCode.OK, Content) {
					WriteAsBlob = true
				};

			case HttpRequestTestType.ReadTimeout:
				content = new HttpRequestContent (TestRunner, currentRequest);
				return new HttpResponse (HttpStatusCode.OK, content);

			case HttpRequestTestType.RedirectNoLength:
				redirect = operation.RegisterRedirect (ctx, Target);
				response = HttpResponse.CreateRedirect (HttpStatusCode.Redirect, redirect);
				response.NoContentLength = true;
				return response;

			case HttpRequestTestType.ServerAbortsRedirect:
				if (isSecondRequest)
					throw ctx.AssertFail ("Should never happen.");
				var cloned = new HttpRequestHandler (this);
				cloned.isSecondRequest = true;
				redirect = operation.RegisterRedirect (ctx, cloned);
				response = HttpResponse.CreateRedirect (HttpStatusCode.Redirect, redirect);
				return response;

			case HttpRequestTestType.ServerAbortsPost:
				return new HttpResponse (HttpStatusCode.BadRequest, Content);

			case HttpRequestTestType.SimpleGZip:
				var gzipContent = new GZipContent (ConnectionHandler.TheQuickBrownFoxBuffer);
				return new HttpResponse (HttpStatusCode.OK, gzipContent);

			case HttpRequestTestType.TestResponseStream:
				response = new HttpResponse (HttpStatusCode.OK, Content);
				response.WriteAsBlob = true;
				return response;

			case HttpRequestTestType.LargeChunkRead:
				response = new HttpResponse (HttpStatusCode.OK, Content);
				response.WriteBodyAsBlob = true;
				return response;

			case HttpRequestTestType.LargeGZipRead:
				gzipContent = new GZipContent ((ChunkedContent)Content);
				response = new HttpResponse (HttpStatusCode.OK, gzipContent);
				return response;

			case HttpRequestTestType.GZipWithLength:
				gzipContent = new GZipContent ((StringContent)Content);
				response = new HttpResponse (HttpStatusCode.OK, gzipContent);
				return response;

			case HttpRequestTestType.ResponseStreamCheckLength2:
			case HttpRequestTestType.ResponseStreamCheckLength:
				response = new HttpResponse (HttpStatusCode.OK, Content);
				return response;

			case HttpRequestTestType.GetNoLength:
				content = new HttpRequestContent (TestRunner, currentRequest);
				return new HttpResponse (HttpStatusCode.OK, content);

			default:
				return HttpResponse.CreateSuccess (ME);
			}

			async Task EntityTooBig ()
			{
				await request.ReadHeaders (ctx, cancellationToken).ConfigureAwait (false);
				await ctx.AssertException<IOException> (() => request.Read (ctx, cancellationToken), "client doesn't send any body");
			}

			async Task ClientAbortsPost ()
			{
				await request.ReadHeaders (ctx, cancellationToken).ConfigureAwait (false);
				await ctx.AssertException<IOException> (() => request.Read (ctx, cancellationToken), "client doesn't send any body");
			}

			async Task PostContentLength ()
			{
				await request.ReadHeaders (ctx, cancellationToken).ConfigureAwait (false);
				ctx.Assert (request.ContentLength, Is.EqualTo (currentRequest.Content.Length), "request.ContentLength");
				readyTcs.TrySetResult (true);
				await request.Read (ctx, cancellationToken);
			}
		}

		public override bool CheckResponse (TestContext ctx, Response response)
		{
			if (Target != null)
				return Target.CheckResponse (ctx, response);

			switch (TestRunner.EffectiveType) {
			case HttpRequestTestType.ReadTimeout:
				return ctx.Expect (response.Status, Is.EqualTo (HttpStatusCode.OK), "response.StatusCode");
			}

			if (!ctx.Expect (response.Content, Is.Not.Null, "response.Content != null"))
				return false;

			return HttpContent.Compare (ctx, response.Content, ExpectedContent, false, "response.Content");
		}

		class HttpRequestRequest : TraditionalRequest
		{
			public HttpRequestHandler Handler {
				get;
			}

			public HttpRequestTestRunner TestRunner {
				get;
			}

			public string ME {
				get;
			}

			TaskCompletionSource<bool> finishedTcs;

			public Task WaitForCompletion ()
			{
				return finishedTcs.Task;
			}

			public HttpRequestRequest (HttpRequestHandler handler, Uri uri)
				: base (uri)
			{
				Handler = handler;
				TestRunner = handler.TestRunner;
				finishedTcs = new TaskCompletionSource<bool> ();
				ME = $"{GetType ().Name}({TestRunner.EffectiveType})";

				switch (TestRunner.EffectiveType) {
				case HttpRequestTestType.PostChunked:
					Content = new HttpRequestContent (TestRunner, this);
					Method = "POST";
					SendChunked ();
					break;
				case HttpRequestTestType.EntityTooBig:
				case HttpRequestTestType.PostContentLength:
				case HttpRequestTestType.ClientAbortsPost:
					Content = new HttpRequestContent (TestRunner, this);
					Method = "POST";
					break;
				case HttpRequestTestType.SimpleGZip:
				case HttpRequestTestType.LargeGZipRead:
				case HttpRequestTestType.GZipWithLength:
					RequestExt.AutomaticDecompression = true;
					break;
				}
			}

			protected override Task WriteBody (TestContext ctx, CancellationToken cancellationToken)
			{
				switch (TestRunner.EffectiveType) {
				case HttpRequestTestType.PutChunked:
				case HttpRequestTestType.PutChunkDontCloseRequest:
					return PutChunked ();

				case HttpRequestTestType.EntityTooBig:
					return EntityTooBig ();

				case HttpRequestTestType.PostContentLength:
					return PostContentLength ();

				case HttpRequestTestType.ClientAbortsPost:
					return ClientAbortsPost ();

				default:
					return base.WriteBody (ctx, cancellationToken);
				}

				async Task EntityTooBig ()
				{
					var stream = await RequestExt.GetRequestStreamAsync ().ConfigureAwait (false);
					await Content.WriteToAsync (ctx, stream, cancellationToken).ConfigureAwait (false);
					// This throws on .NET
					try { stream.Dispose (); } catch { }
				}

				async Task PostContentLength ()
				{
					using (var stream = await RequestExt.GetRequestStreamAsync ().ConfigureAwait (false)) {
						await AbstractConnection.WaitWithTimeout (ctx, 1500, Handler.WaitUntilReady ());
						await Content.WriteToAsync (ctx, stream, cancellationToken);
						stream.Flush ();
					}
				}

				async Task PutChunked ()
				{
					var stream = await RequestExt.GetRequestStreamAsync ().ConfigureAwait (false);
					try {
						await Content.WriteToAsync (ctx, stream, cancellationToken).ConfigureAwait (false);
						await stream.FlushAsync ();
					} finally {
						if (TestRunner.EffectiveType == HttpRequestTestType.PutChunked)
							stream.Dispose ();
					}
				}

				async Task ClientAbortsPost ()
				{
					var stream = await RequestExt.GetRequestStreamAsync ().ConfigureAwait (false);
					try {
						stream.Dispose ();
					} catch (Exception ex) {
						ctx.LogDebug (4, $"{ME} GOT EX: {ex.Message}");
					}
				}
			}

			public override async Task<Response> SendAsync (TestContext ctx, CancellationToken cancellationToken)
			{
				var portable = DependencyInjector.Get<IPortableSupport> ();
				if (TestRunner.EffectiveType == HttpRequestTestType.CloseRequestStream) {
					Request.Method = "POST";
					RequestExt.SetContentLength (16384);
					var stream = await RequestExt.GetRequestStreamAsync ().ConfigureAwait (false);
					try {
						portable.Close (stream);
						throw ctx.AssertFail ("Expected exception.");
					} catch (Exception ex) {
						return new SimpleResponse (this, HttpStatusCode.InternalServerError, null, ex);
					}
				}

				return await base.SendAsync (ctx, cancellationToken).ConfigureAwait (false);
			}

			protected override async Task<Response> GetResponseFromHttp (
				TestContext ctx, HttpWebResponse response, WebException error, CancellationToken cancellationToken)
			{
				cancellationToken.ThrowIfCancellationRequested ();
				HttpContent content = null;

				ctx.LogDebug (4, $"{ME} GET RESPONSE FROM HTTP");

				switch (TestRunner.EffectiveType) {
				case HttpRequestTestType.ReadTimeout:
					return await ReadWithTimeout (5000, WebExceptionStatus.Timeout).ConfigureAwait (false);
				}

				using (var stream = response.GetResponseStream ()) {
					switch (TestRunner.EffectiveType) {
					case HttpRequestTestType.TestResponseStream:
						content = await TestResponseStream (stream).ConfigureAwait (false);
						break;

					case HttpRequestTestType.LargeChunkRead:
						content = await LargeChunkRead (stream).ConfigureAwait (false);
						break;

					case HttpRequestTestType.LargeGZipRead:
						content = await ReadAsString (stream).ConfigureAwait (false);
						break;

					case HttpRequestTestType.GZipWithLength:
						content = await GZipWithLength (stream).ConfigureAwait (false);
						break;

					case HttpRequestTestType.ResponseStreamCheckLength2:
						content = await ResponseStreamCheckLength (stream, true).ConfigureAwait (false);
						break;

					case HttpRequestTestType.ResponseStreamCheckLength:
						content = await ResponseStreamCheckLength (stream, false).ConfigureAwait (false);
						break;

					case HttpRequestTestType.GetNoLength:
						content = await GetNoLength (stream).ConfigureAwait (false);
						break;

					default:
						content = await ReadAsString (stream).ConfigureAwait (false);
						break;
					}
				}

				var status = response.StatusCode;

				response.Dispose ();
				finishedTcs.TrySetResult (true);
				return new SimpleResponse (this, status, content, error);

				async Task<Response> ReadWithTimeout (int timeout, WebExceptionStatus expectedStatus)
				{
					StreamReader reader = null;
					try {
						reader = new StreamReader (response.GetResponseStream ());
						var readTask = reader.ReadToEndAsync ();
						if (timeout > 0) {
							var timeoutTask = Task.Delay (timeout);
							var task = await Task.WhenAny (timeoutTask, readTask).ConfigureAwait (false);
							if (task == timeoutTask)
								throw ctx.AssertFail ("Timeout expired.");
						}
						var ret = await readTask.ConfigureAwait (false);
						ctx.LogMessage ($"EXPECTED ERROR: {ret}");
						throw ctx.AssertFail ("Expected exception.");
					} catch (WebException wexc) {
						ctx.Assert ((WebExceptionStatus)wexc.Status, Is.EqualTo (expectedStatus));
						return new SimpleResponse (this, HttpStatusCode.InternalServerError, null, wexc);
					} finally {
						finishedTcs.TrySetResult (true);
					}
				}

				async Task<HttpContent> ReadAsString (Stream stream)
				{
					using (var reader = new StreamReader (stream)) {
						string text = null;
						if (!reader.EndOfStream)
							text = await reader.ReadToEndAsync ().ConfigureAwait (false);
						return StringContent.CreateMaybeNull (text);
					}
				}

				async Task<HttpContent> TestResponseStream (Stream stream)
				{
					var buffer = new byte[5];
					var ret = await stream.ReadAsync (buffer, 4, 1).ConfigureAwait (false);
					ctx.Assert (ret, Is.EqualTo (1), "#A1");
					ctx.Assert (buffer[4], Is.EqualTo ((byte)65), "#A2");
					ret = await stream.ReadAsync (buffer, 0, 2);
					ctx.Assert (ret, Is.EqualTo (2), "#B1");
					return Handler.Content;
				}

				async Task<HttpContent> LargeChunkRead (Stream stream)
				{
					var buffer = new byte[43];
					var ret = await stream.ReadAsync (buffer, 0, buffer.Length).ConfigureAwait (false);
					ctx.Assert (ret, Is.EqualTo (ConnectionHandler.TheQuickBrownFox.Length), "#A1");
					var text = Encoding.UTF8.GetString (buffer, 0, ret);
					return new StringContent (text);
				}

				async Task<HttpContent> GZipWithLength (Stream stream)
				{
					using (var ms = new MemoryStream ()) {
						await stream.CopyToAsync (ms, 16384).ConfigureAwait (false);
						var bytes = ms.ToArray ();
						var text = Encoding.UTF8.GetString (bytes, 0, bytes.Length);
						return new StringContent (text);
					}
				}

				async Task<HttpContent> ResponseStreamCheckLength (Stream stream, bool chunked)
				{
					await ctx.AssertException<NotSupportedException> (() => Task.FromResult (stream.Length), "Length should throw");
					if (chunked) {
						ctx.Assert (response.ContentLength, Is.EqualTo (-1L), "ContentLength");
						ctx.Assert (response.Headers["Transfer-Encoding"], Is.EqualTo ("chunked"), "chunked encoding");
					} else {
						ctx.Assert (response.ContentLength, Is.EqualTo ((long)Handler.Content.Length), "ContentLength");
						ctx.Assert (response.Headers["Content-Length"], Is.EqualTo (Handler.Content.Length.ToString ()), "Content-Length header");
					}
					return await GZipWithLength (stream).ConfigureAwait (false);
				}

				async Task<HttpContent> GetNoLength (Stream stream)
				{
					ctx.Assert (response.ContentLength, Is.EqualTo (-1L), "ContentLength");
					ctx.Assert (response.Headers["Content-Length"], Is.Null, "No Content-Length: header");
					return await ReadAsString (stream);
				}
			}
		}

		class HttpRequestContent : HttpContent
		{
			public HttpRequestTestRunner TestRunner {
				get;
			}

			public HttpRequestRequest Request {
				get;
			}

			public string ME {
				get;
			}

			public HttpRequestContent (HttpRequestTestRunner runner, HttpRequestRequest request)
			{
				TestRunner = runner;
				Request = request;
				ME = $"{GetType ().Name}({runner.EffectiveType})";

				switch (runner.EffectiveType) {
				case HttpRequestTestType.EntityTooBig:
				case HttpRequestTestType.ClientAbortsPost:
					HasLength = true;
					Length = 16;
					break;
				case HttpRequestTestType.PostContentLength:
					HasLength = true;
					Length = ConnectionHandler.TheQuickBrownFoxBuffer.Length;
					break;
				case HttpRequestTestType.LargeChunkRead:
					break;
				case HttpRequestTestType.GetNoLength:
					NoLength = true;
					break;
				default:
					HasLength = true;
					Length = 4096;
					break;
				}
			}

			public sealed override bool HasLength {
				get;
			}

			public sealed override int Length {
				get;
			}

			public bool NoLength {
				get;
			}

			public override void AddHeadersTo (HttpMessage message)
			{
				if (NoLength) {
					message.ContentType = "text/plain";
				} else if (HasLength) {
					message.ContentType = "text/plain";
					message.ContentLength = Length;
				} else {
					message.TransferEncoding = "chunked";
				}
			}

			public override byte[] AsByteArray ()
			{
				throw new NotImplementedException ();
			}

			public override string AsString ()
			{
				throw new NotImplementedException ();
			}

			public override async Task WriteToAsync (TestContext ctx, Stream stream, CancellationToken cancellationToken)
			{
				ctx.LogDebug (4, $"{ME} WRITE BODY");

				switch (TestRunner.EffectiveType) {
				case HttpRequestTestType.ReadTimeout:
					await stream.WriteAsync (ConnectionHandler.TheQuickBrownFoxBuffer, cancellationToken).ConfigureAwait (false);
					await stream.FlushAsync (cancellationToken);
					await Task.WhenAny (Request.WaitForCompletion (), Task.Delay (10000));
					break;

				case HttpRequestTestType.PostChunked:
					await HandlePostChunked ().ConfigureAwait (false);
					break;

				case HttpRequestTestType.EntityTooBig:
					await ctx.AssertException<ProtocolViolationException> (() =>
						stream.WriteAsync (ConnectionHandler.TheQuickBrownFoxBuffer, cancellationToken),
						"writing too many bytes").ConfigureAwait (false);
					break;

				case HttpRequestTestType.PostContentLength:
					await stream.WriteAsync (ConnectionHandler.TheQuickBrownFoxBuffer, cancellationToken).ConfigureAwait (false);
					await stream.FlushAsync (cancellationToken);
					break;

				case HttpRequestTestType.LargeChunkRead:
					await HandleLargeChunkRead ().ConfigureAwait (false);
					break;

				case HttpRequestTestType.GetNoLength:
					await stream.WriteAsync (ConnectionHandler.TheQuickBrownFoxBuffer, cancellationToken).ConfigureAwait (false);
					await stream.FlushAsync (cancellationToken);
					stream.Dispose ();
					break;

				default:
					throw ctx.AssertFail (TestRunner.EffectiveType);
				}

				async Task HandlePostChunked ()
				{
					await stream.WriteAsync (ConnectionHandler.TheQuickBrownFoxBuffer, cancellationToken).ConfigureAwait (false);
					await stream.FlushAsync (cancellationToken);

					await AbstractConnection.WaitWithTimeout (ctx, 1500, Request.Handler.WaitUntilReady ());

					await stream.WriteAsync (ConnectionHandler.GetLargeTextBuffer (50), cancellationToken);
				}

				async Task HandleLargeChunkRead ()
				{
					await ChunkedContent.WriteChunkAsBlob (
						stream, ConnectionHandler.TheQuickBrownFoxBuffer,
						cancellationToken).ConfigureAwait (false);
					await stream.FlushAsync (cancellationToken);

					await ChunkedContent.WriteChunkAsBlob (
						stream, ConnectionHandler.GetLargeTextBuffer (50),
						cancellationToken);
					await ChunkedContent.WriteChunkTrailer (stream, cancellationToken);
					await stream.FlushAsync (cancellationToken);
				}
			}
		}
	}
}
