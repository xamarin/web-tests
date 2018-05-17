﻿//
// InstrumentationRequest.cs
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
using Xamarin.AsyncTests.Constraints;

namespace Xamarin.WebTests.TestRunners
{
	using HttpFramework;
	using HttpHandlers;

	public sealed class InstrumentationRequest : TraditionalRequest
	{
		public InstrumentationTestRunner TestRunner {
			get;
		}

		public InstrumentationRequest (InstrumentationTestRunner fixture, Uri uri)
			: base (uri)
		{
			TestRunner = fixture;
		}

		public InstrumentationRequest (InstrumentationTestRunner fixture, HttpWebRequest request)
			: base (request)
		{
			TestRunner = fixture;
		}

		public override bool HasContent => TestRunner.HasRequestBody;

		internal Task<Response> DefaultSendAsync (TestContext ctx, CancellationToken cancellationToken)
		{
			return base.SendAsync (ctx, cancellationToken);
		}

		public override Task<Response> SendAsync (TestContext ctx, CancellationToken cancellationToken)
		{
			return TestRunner.SendRequest (ctx, this, cancellationToken);
		}

		protected override Task WriteBody (
			TestContext ctx, CancellationToken cancellationToken)
		{
			return TestRunner.WriteRequestBody (
				ctx, this, cancellationToken);
		}

		protected override Task<TraditionalResponse> GetResponseFromHttp (
			TestContext ctx, HttpWebResponse response,
			WebException error, CancellationToken cancellationToken)
		{
			return TestRunner.ReadResponse (
				ctx, this, response, error, cancellationToken);
		}
	}
}
