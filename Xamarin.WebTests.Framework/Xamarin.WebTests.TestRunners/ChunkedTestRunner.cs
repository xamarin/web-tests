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
using System.Threading;
using System.Threading.Tasks;
using Xamarin.AsyncTests;

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
			return new ChunkedRequest (uri);
		}

		public Task Run (TestContext ctx, CancellationToken cancellationToken)
		{
			switch (Type) {
			case ChunkContentType.NormalChunk:
				return Run (ctx, cancellationToken, HttpStatusCode.OK, WebExceptionStatus.Success);
			default:
				throw new NotImplementedException ();
			}
		}

		class ChunkedRequest : TraditionalRequest
		{
			public ChunkedRequest (Uri uri)
				: base (uri)
			{
			}

			protected override Task<Response> GetResponseFromHttp (TestContext ctx, HttpWebResponse response, WebException error, CancellationToken cancellationToken)
			{
				return base.GetResponseFromHttp (ctx, response, error, cancellationToken);
			}
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

