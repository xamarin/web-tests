//
// PutData.cs
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
using System.Threading;
using System.Threading.Tasks;
using Xamarin.AsyncTests;

namespace Xamarin.WebTests.HttpClientTests
{
	using TestFramework;
	using TestAttributes;
	using HttpFramework;
	using HttpHandlers;
	using TestRunners;

	[HttpServerTestCategory (HttpServerTestCategory.Default)]
	public class PutData : HttpClientTestFixture
	{
		public PutDataType Type {
			get;
		}

		public HttpContent Content {
			get;
		}

		[AsyncTest]
		public PutData (PutDataType type)
		{
			Type = type;

			switch (type) {
			case PutDataType.SendLargeBlob:
				Content = BinaryContent.CreateRandom (102400);
				break;
			case PutDataType.SendLargeBlobOddSize:
				Content = BinaryContent.CreateRandom (102431);
				break;
			}
		}

		public enum PutDataType
		{
			// Bug 41206
			SendLargeBlob,
			SendLargeBlobOddSize,
		}

		public override Handler CreateHandler (TestContext ctx)
		{
			switch (Type) {
			case PutDataType.SendLargeBlob:
				return new PostHandler (ME, Content);
			case PutDataType.SendLargeBlobOddSize:
				return new PostHandler (ME, Content);
			default:
				throw ctx.AssertFail (Type);
			}
		}

		protected override void ConfigureRequest (
			TestContext ctx, InstrumentationOperation operation,
			Request request, Uri uri)
		{
			request.Content = Content;
			base.ConfigureRequest (ctx, operation, request, uri);
		}

		public override Task<Response> Run (
			TestContext ctx, HttpClientRequest request,
			CancellationToken cancellationToken)
		{
			return request.PutDataAsync (ctx, cancellationToken);
		}
	}
}
