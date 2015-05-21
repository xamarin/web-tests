//
// ChunkedTestRunner.cs
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
using System.Net;
using XWebExceptionStatus = System.Net.WebExceptionStatus;
using System.Threading;
using System.Threading.Tasks;
using Xamarin.AsyncTests;
using Xamarin.AsyncTests.Constraints;

namespace Xamarin.WebTests.TestRunners
{
	using HttpFramework;
	using HttpHandlers;

	public class ChunkedTestRunner : TraditionalTestRunner
	{
		public ChunkContentType Type {
			get;
			private set;
		}

		public ChunkedTestRunner (HttpServer server, ChunkContentType type, bool sendAsync)
			: base (server, CreateHandler (type), sendAsync)
		{
			Type = type;
		}

		protected override Request CreateRequest (TestContext ctx, Uri uri)
		{
			return new ChunkedRequest (this, uri);
		}

		public Task Run (TestContext ctx, CancellationToken cancellationToken)
		{
			switch (Type) {
			case ChunkContentType.NormalChunk:
				return Run (ctx, cancellationToken, HttpStatusCode.OK, WebExceptionStatus.Success);
			case ChunkContentType.MissingTrailer:
				return Run (ctx, cancellationToken, HttpStatusCode.OK, WebExceptionStatus.ResponseContentException);
			case ChunkContentType.TruncatedChunk:
				return Run (ctx, cancellationToken, HttpStatusCode.OK, WebExceptionStatus.ResponseContentTruncated);
			default:
				throw new NotImplementedException ();
			}
		}

		class ChunkedRequest : TraditionalRequest
		{
			public ChunkedTestRunner Runner {
				get;
				private set;
			}

			public ChunkedRequest (ChunkedTestRunner runner, Uri uri)
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
			string content;
			var status = response.StatusCode;

			ctx.Assert (status, Is.EqualTo (HttpStatusCode.OK), "success");
			ctx.Assert (error, Is.Null, "null error");

			try {
				using (var reader = new StreamReader (response.GetResponseStream ())) {
					content = await reader.ReadToEndAsync ();
				}

				if (Type != ChunkContentType.NormalChunk)
					ctx.AssertFail ("expected exception");
			} catch (IOException ex) {
				error = new WebException ("failed to read response", ex, (XWebExceptionStatus)WebExceptionStatus.ResponseContentException, response);
				content = null;
			} catch (WebException ex) {
				if (Type != ChunkContentType.TruncatedChunk)
					throw;
				ctx.Assert ((WebExceptionStatus)ex.Status, Is.EqualTo (WebExceptionStatus.ConnectionClosed), "expected ConnectionClosed");
				error = new WebException (ex.Message, ex, (XWebExceptionStatus)WebExceptionStatus.ResponseContentTruncated, response);
				content = null;
			}
			response.Dispose ();

			return new SimpleResponse (request, status, StringContent.CreateMaybeNull (content), error);
		}

		static string ChunkTypeName (ChunkContentType type)
		{
			return type.ToString ();
		}

		public static Handler CreateHandler (ChunkContentType type)
		{
			return new GetHandler (ChunkTypeName (type), new TestChunkedContent (type));
		}

		class TestChunkedContent : HttpContent
		{
			public ChunkContentType Type {
				get;
				private set;
			}

			public TestChunkedContent (ChunkContentType type)
			{
				Type = type;
			}

			#region implemented abstract members of HttpContent
			public override string AsString ()
			{
				return Type.ToString ();
			}
			public override void AddHeadersTo (HttpMessage message)
			{
				message.SetHeader ("Transfer-Encoding", "chunked");
				message.SetHeader ("Content-Type", "text/plain");
			}
			public override async Task WriteToAsync (StreamWriter writer)
			{
				writer.AutoFlush = true;
				await writer.WriteAsync ("4\r\n");
				await writer.WriteAsync ("AAAA\r\n");

				switch (Type) {
				case ChunkContentType.NormalChunk:
					await writer.WriteAsync ("0\r\n\r\n\r\n");
					break;
				case ChunkContentType.TruncatedChunk:
					await writer.WriteAsync ("8\r\n");
					await writer.WriteAsync ("B");
					break;
				case ChunkContentType.MissingTrailer:
					break;
				default:
					throw new NotImplementedException ();
				}
			}
			#endregion
		}
	}
}

