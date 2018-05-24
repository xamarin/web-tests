//
// RedirectCustomContent.cs
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
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Xamarin.AsyncTests;

namespace Xamarin.WebTests.HttpClientTests
{
	using TestAttributes;
	using TestFramework;
	using HttpFramework;
	using HttpHandlers;
	using HttpClient;

	[NotWorking]
	public class RedirectCustomContent : HttpClientTestFixture
	{
		byte[] ContentBuffer => ConnectionHandler.GetTextBuffer (ME);

		public override Handler CreateHandler (TestContext ctx)
		{
			var handler = new PostHandler (ME, new CustomContent (this), TransferMode.ContentLength);
			return new RedirectHandler (handler, HttpStatusCode.TemporaryRedirect) {
				Flags = RequestFlags.KeepAlive
			};
		}

		public override Task<Response> Run (
			TestContext ctx, HttpClientRequest request,
			CancellationToken cancellationToken)
		{
			return request.PostString (ctx, cancellationToken);
		}

		class CustomContent : HttpContent, ICustomHttpContent
		{
			public RedirectCustomContent Parent {
				get;
			}

			public CustomContent (RedirectCustomContent parent)
			{
				Parent = parent;
			}

			HttpContent ICustomHttpContent.Content => this;

			public override bool HasLength => true;
			public override int Length => AsByteArray ().Length;

			public override string AsString () => ConnectionHandler.TheQuickBrownFox;

			public override byte[] AsByteArray () => ConnectionHandler.TheQuickBrownFoxBuffer;

			public override void AddHeadersTo (HttpMessage message)
			{
			}

			public override async Task WriteToAsync (TestContext ctx, Stream stream, CancellationToken cancellationToken)
			{
				await stream.WriteAsync (AsString (), cancellationToken).ConfigureAwait (false);
			}

			public override HttpContent RemoveTransferEncoding ()
			{
				return this;
			}

			public Task<Stream> CreateContentReadStreamAsync ()
			{
				throw new NotImplementedException ();
			}

			bool first = true;

			public async Task SerializeToStreamAsync (Stream stream)
			{
				byte[] buffer;
				if (first) {
					buffer = AsByteArray ();
					first = false;
				} else {
					buffer = Parent.ContentBuffer;
				}

				await stream.WriteAsync (buffer, 0, buffer.Length).ConfigureAwait (false);
			}

			public bool TryComputeLength (out long length)
			{
				if (HasLength) {
					length = Length;
					return true;
				}

				length = -1;
				return false;
			}
		}

	}
}
