//
// RedirectHandler.cs
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
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Globalization;

using Xamarin.AsyncTests;

namespace Xamarin.WebTests.HttpHandlers
{
	using HttpFramework;

	public class RedirectHandler : AbstractRedirectHandler
	{
		public HttpStatusCode Code {
			get;
			private set;
		}

		public override object Clone ()
		{
			return new RedirectHandler (Target, Code);
		}

		public RedirectHandler (Handler target, HttpStatusCode code, string identifier = null)
			: base (target, identifier ?? CreateIdentifier (target, code))
		{
			Code = code;

			if (!IsRedirectStatus (code))
				throw new InvalidOperationException ();
		}

		static string CreateIdentifier (Handler target, HttpStatusCode code)
		{
			return string.Format ("Redirect({0}): {1}", code, target.Value);
		}

		internal static bool IsRedirectStatus (HttpStatusCode code)
		{
			var icode = (int)code;
			return icode == 301 || icode == 302 || icode == 303 || icode == 307;
		}

		public override Task<HttpResponse> HandleRequest (
			TestContext ctx, HttpOperation operation, HttpConnection connection, HttpRequest request,
			RequestFlags effectiveFlags, CancellationToken cancellationToken)
		{
			var keepAlive = (effectiveFlags & (RequestFlags.KeepAlive | RequestFlags.CloseConnection)) == RequestFlags.KeepAlive;

			var redirect = operation.RegisterRedirect (ctx, Target);
			var response = HttpResponse.CreateRedirect (Code, redirect.Uri);
			if (keepAlive)
				response.KeepAlive = true;

			if (operation.HasAnyFlags (HttpOperationFlags.ClientDoesNotSendRedirect))
				redirect.Listener.UnregisterOperation (redirect);
			else
				response.Redirect = redirect;

			return Task.FromResult (response);
		}

		public override bool CheckResponse (TestContext ctx, Response response)
		{
			return Target.CheckResponse (ctx, response);
		}
	}
}

