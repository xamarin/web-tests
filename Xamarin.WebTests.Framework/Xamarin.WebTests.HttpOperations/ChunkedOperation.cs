//
// ChunkedOperation.cs
//
// Author:
//       Martin Baulig <martin.baulig@xamarin.com>
//
// Copyright (c) 2015 Xamarin, Inc.
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
using System.Net;
using XWebExceptionStatus = System.Net.WebExceptionStatus;
using System.Threading;
using System.Threading.Tasks;
using Xamarin.AsyncTests;
using Xamarin.AsyncTests.Constraints;

namespace Xamarin.WebTests.HttpOperations
{
	using ConnectionFramework;
	using TestFramework;
	using HttpFramework;
	using HttpHandlers;

	public class ChunkedOperation : TraditionalOperation
	{
		public ChunkedOperationType Type {
			get;
			private set;
		}

		bool ExpectException {
			get {
				switch (Type) {
				case ChunkedOperationType.NormalChunk:
				case ChunkedOperationType.SyncRead:
				case ChunkedOperationType.BeginEndAsyncRead:
				case ChunkedOperationType.BeginEndAsyncReadNoWait:
				case ChunkedOperationType.ServerAbort:
					return false;
				default:
					return true;
				}
			}
		}

		ChunkedOperation (HttpServer server, Handler handler, ChunkedOperationType type, bool sendAsync,
		                   HttpOperationFlags flags, HttpStatusCode expectedStatus,
		                   WebExceptionStatus expectedError)
			: base (server, handler, sendAsync, flags, expectedStatus, expectedError)
		{
			Type = type;
		}

		public static ChunkedOperation Create (HttpServer server, ChunkedOperationType type, bool sendAsync)
		{
			var (flags, status, error) = GetParameters (type);
			var handler = new GetHandler (ChunkTypeName (type), ChunkContent (type));
			return new ChunkedOperation (server, handler, type, sendAsync, flags, status, error);
		}

		protected override Request CreateRequest (TestContext ctx, Uri uri)
		{
			var request = new ChunkedRequest (this, uri);
			// May need to adjust these values a bit.
			if (Type == ChunkedOperationType.SyncReadTimeout)
				request.RequestExt.ReadWriteTimeout = 500;
			else
				request.RequestExt.ReadWriteTimeout = 1500;
			return request;
		}

		protected override void ConfigureRequest (TestContext ctx, Uri uri, Request request)
		{
			base.ConfigureRequest (ctx, uri, request);
		}

		static (HttpOperationFlags flags, HttpStatusCode status, WebExceptionStatus error) GetParameters (ChunkedOperationType type)
		{
			switch (type) {
			case ChunkedOperationType.SyncRead:
			case ChunkedOperationType.NormalChunk:
			case ChunkedOperationType.BeginEndAsyncRead:
			case ChunkedOperationType.BeginEndAsyncReadNoWait:
			case ChunkedOperationType.ServerAbort:
				return (HttpOperationFlags.None, HttpStatusCode.OK, WebExceptionStatus.Success);
			case ChunkedOperationType.SyncReadTimeout:
				return (HttpOperationFlags.None, HttpStatusCode.OK, WebExceptionStatus.Timeout);
			case ChunkedOperationType.MissingTrailer:
				return (HttpOperationFlags.None, HttpStatusCode.OK, WebExceptionStatus.ResponseContentException);
			case ChunkedOperationType.TruncatedChunk:
				return (HttpOperationFlags.None, HttpStatusCode.OK, WebExceptionStatus.ResponseContentTruncated);
			default:
				throw new NotImplementedException ();
			}
		}

		class ChunkedRequest : TraditionalRequest
		{
			public ChunkedOperation Runner {
				get;
				private set;
			}

			public ChunkedRequest (ChunkedOperation runner, Uri uri)
				: base (uri)
			{
				Runner = runner;
			}

			protected override Task<Response> GetResponseFromHttp (TestContext ctx, HttpWebResponse response, WebException error, CancellationToken cancellationToken)
			{
				return Runner.GetResponseFromHttp (ctx, this, response, error, cancellationToken);
			}
		}

		async Task<Response> GetResponseFromHttp (TestContext ctx, ChunkedRequest request, HttpWebResponse response, WebException error, CancellationToken cancellationToken)
		{
			var content = string.Empty;
			var status = response.StatusCode;

			ctx.Assert (status, Is.EqualTo (HttpStatusCode.OK), "success");
			ctx.Assert (error, Is.Null, "null error");

			var stream = response.GetResponseStream ();

			try {
				if (Type == ChunkedOperationType.BeginEndAsyncRead || Type == ChunkedOperationType.BeginEndAsyncReadNoWait) {
					var provider = DependencyInjector.Get<IStreamProvider> ();

					var buffer = new byte [1024];
					var result = provider.BeginRead (stream, buffer, 0, buffer.Length, null, null);

					if (Type != ChunkedOperationType.BeginEndAsyncReadNoWait) {
						await Task.Run (() => {
							var timeout = ctx.Settings.DisableTimeouts ? -1 : 500;
							var waitResult = result.AsyncWaitHandle.WaitOne (timeout);
							ctx.Assert (waitResult, "WaitOne");
						});
					}

					var ret = provider.EndRead (stream, result);
					ctx.Assert (ret, Is.GreaterThan (0), "non-zero read");
					content = Encoding.UTF8.GetString (buffer, 0, ret);
				}

				var reader = new StreamReader (stream);
				if (Type == ChunkedOperationType.SyncRead || Type == ChunkedOperationType.SyncReadTimeout) {
					content += reader.ReadToEnd ();
				} else {
					content += await reader.ReadToEndAsync ();
				}

				if (Type == ChunkedOperationType.ServerAbort) {
					ctx.Assert (content.Length, Is.EqualTo (1), "read one byte");
				}

				if (ExpectException)
					ctx.AssertFail ("expected exception");
			} catch (IOException ex) {
				error = new WebException ("failed to read response", ex, (XWebExceptionStatus)WebExceptionStatus.ResponseContentException, response);
				content = null;
			} catch (WebException ex) {
				if (Type == ChunkedOperationType.SyncReadTimeout) {
					ctx.Assert ((WebExceptionStatus)ex.Status, Is.EqualTo (WebExceptionStatus.Timeout), "expected Timeout");
					error = new WebException (ex.Message, ex, (XWebExceptionStatus)WebExceptionStatus.Timeout, response);
				} else if (Type == ChunkedOperationType.TruncatedChunk) {
					ctx.Assert ((WebExceptionStatus)ex.Status, Is.EqualTo (WebExceptionStatus.ConnectionClosed), "expected ConnectionClosed");
					error = new WebException (ex.Message, ex, (XWebExceptionStatus)WebExceptionStatus.ResponseContentTruncated, response);
				} else {
					throw;
				}
				content = null;
			} finally {
				stream.Dispose ();
				response.Dispose ();
			}

			return new SimpleResponse (request, status, StringContent.CreateMaybeNull (content), error);
		}

		static string ChunkTypeName (ChunkedOperationType type)
		{
			return type.ToString ();
		}

		static HttpContent ChunkContent (ChunkedOperationType type)
		{
			if (type == ChunkedOperationType.ServerAbort)
				return new ServerAbortContent ();
			else
				return new TestChunkedContent (type);
		}

		class ServerAbortContent : HttpContent
		{
			#region implemented abstract members of HttpContent
			public override bool HasLength {
				get { return false; }
			}
			public override int Length {
				get { throw new NotSupportedException (); }
			}
			public override string AsString ()
			{
				return GetType ().Name;
			}
			public override byte[] AsByteArray ()
			{
				throw new NotSupportedException ();
			}
			public override void AddHeadersTo (HttpMessage message)
			{
				message.ContentLength = 65536;
			}
			public override async Task WriteToAsync (TestContext ctx, Stream stream, CancellationToken cancellationToken)
			{
				await stream.WriteAsync ("A", cancellationToken).ConfigureAwait (false);
				await stream.FlushAsync ();
			}
			#endregion
		}

		class TestChunkedContent : HttpContent
		{
			public ChunkedOperationType Type {
				get;
				private set;
			}

			public TestChunkedContent (ChunkedOperationType type)
			{
				Type = type;
			}

			#region implemented abstract members of HttpContent
			public override bool HasLength {
				get { return false; }
			}
			public override int Length {
				get { throw new NotSupportedException (); }
			}
			public override string AsString ()
			{
				return Type.ToString ();
			}
			public override byte[] AsByteArray ()
			{
				throw new NotSupportedException ();
			}
			public override void AddHeadersTo (HttpMessage message)
			{
				message.TransferEncoding = "chunked";
				message.ContentType = "text/plain";
			}
			public override async Task WriteToAsync (TestContext ctx, Stream stream, CancellationToken cancellationToken)
			{
				await stream.WriteAsync ("4\r\n");
				await stream.WriteAsync ("AAAA\r\n");
				if (Type == ChunkedOperationType.BeginEndAsyncRead || Type == ChunkedOperationType.BeginEndAsyncReadNoWait)
					await Task.Delay (50);
				else if (Type == ChunkedOperationType.SyncReadTimeout)
					await Task.Delay (5000);
				await stream.WriteAsync ("8\r\n");
				await stream.WriteAsync ("BBBBBBBB\r\n");

				switch (Type) {
				case ChunkedOperationType.SyncRead:
				case ChunkedOperationType.SyncReadTimeout:
				case ChunkedOperationType.NormalChunk:
				case ChunkedOperationType.BeginEndAsyncRead:
				case ChunkedOperationType.BeginEndAsyncReadNoWait:
					await stream.WriteAsync ("0\r\n\r\n");
					break;
				case ChunkedOperationType.TruncatedChunk:
					await stream.WriteAsync ("8\r\n");
					await stream.WriteAsync ("B");
					break;
				case ChunkedOperationType.MissingTrailer:
					break;
				default:
					throw new NotImplementedException ();
				}
			}
			#endregion
		}
	}
}

