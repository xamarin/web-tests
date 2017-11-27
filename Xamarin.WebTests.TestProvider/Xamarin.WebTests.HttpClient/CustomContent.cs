//
// CustomContent.cs
//
// Author:
//       Martin Baulig <mabaul@microsoft.com>
//
// Copyright (c) 2017 Xamarin Inc. (http://www.xamarin.com)
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
using System.Threading.Tasks;

namespace Xamarin.WebTests.HttpClient
{
	using Http = System.Net.Http;
	using X = HttpFramework;

	class CustomContent : HttpClientContent
	{
		new Custom Content => (Custom)base.Content;

		public CustomContent (ICustomHttpContent content)
			: base (new Custom (content))
		{
		}

		protected override Task<X.HttpContent> LoadContent ()
		{
			return Task.FromResult (Content.Impl.Content);
		}

		class Custom : Http.HttpContent
		{
			public ICustomHttpContent Impl {
				get;
			}

			public Custom (ICustomHttpContent impl)
			{
				Impl = impl;
			}

			protected override Task<Stream> CreateContentReadStreamAsync ()
			{
				return Impl.CreateContentReadStreamAsync ();
			}

			protected override Task SerializeToStreamAsync (Stream stream, TransportContext context)
			{
				return Impl.SerializeToStreamAsync (stream);
			}

			protected override bool TryComputeLength (out long length)
			{
				return Impl.TryComputeLength (out length);
			}
		}
	}
}
