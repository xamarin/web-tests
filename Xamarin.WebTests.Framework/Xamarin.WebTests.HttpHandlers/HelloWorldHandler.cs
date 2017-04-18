//
// HelloWorldBehavior.cs
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
using System.Threading;
using System.Threading.Tasks;
using Xamarin.AsyncTests;
using Xamarin.AsyncTests.Constraints;

namespace Xamarin.WebTests.HttpHandlers
{
	using HttpFramework;

	public class HelloWorldHandler : Handler
	{
		public HelloWorldHandler (string identifier)
			: base (identifier)
		{
			Message = string.Format ("Hello World {0}!", ++next_id);
		}

		public string Message {
			get;
		}

		static int next_id;

		public override object Clone ()
		{
			return new HelloWorldHandler (Value);
		}

		internal protected override Task<HttpResponse> HandleRequest (
			TestContext ctx, HttpConnection connection, HttpRequest request,
			RequestFlags effectiveFlags, CancellationToken cancellationToken)
		{
			ctx.Assert (request.Method, Is.EqualTo ("GET"), "method");
			return Task.FromResult (HttpResponse.CreateSuccess (Message));
		}

		public override bool CheckResponse (TestContext ctx, Response response)
		{
			if (!ctx.Expect (response.Content, Is.Not.Null, "response.Content != null"))
				return false;
			if (!ctx.Expect (response.Content.Length, Is.EqualTo (Message.Length), "response.Content.Length"))
				return false;
			if (!ctx.Expect (response.Content.AsString (), Is.EqualTo (Message), "response.Content.AsString()"))
				return false;
			return true;
		}
	}
}

