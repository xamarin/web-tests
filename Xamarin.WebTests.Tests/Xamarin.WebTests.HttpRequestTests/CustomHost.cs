//
// CustomHost.cs
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
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Xamarin.AsyncTests;
using Xamarin.AsyncTests.Constraints;

namespace Xamarin.WebTests.HttpRequestTests
{
	using TestFramework;
	using HttpFramework;
	using HttpHandlers;

	public class CustomHost : CustomHandlerFixture
	{
		public override HttpServerTestCategory Category => HttpServerTestCategory.Default;

		public override bool CloseConnection => true;

		public override bool HasRequestBody => false;

		public override HttpContent ExpectedContent => new StringContent (ME);

		public enum CustomHostType {
			Default,
			HostWithPort,
			DefaultPort,
			ImplicitHost
		}

		public CustomHostType Type {
			get; set;
		}

		protected override void ConfigureRequest (
			TestContext ctx, Uri uri, CustomHandler handler,
			TraditionalRequest request)
		{
			switch (Type) {
			case CustomHostType.Default:
				request.RequestExt.Host = "custom";
				break;

			case CustomHostType.HostWithPort:
				request.RequestExt.Host = "custom:8888";
				break;

			case CustomHostType.DefaultPort:
				var defaultPort = handler.TestRunner.Server.UseSSL ? 443 : 80;
				request.RequestExt.Host = $"custom:{defaultPort}";
				break;

			case CustomHostType.ImplicitHost:
				break;

			default:
				throw ctx.AssertFail (Type);
			}

			base.ConfigureRequest (ctx, uri, handler, request);
		}

		public override HttpResponse HandleRequest (
			TestContext ctx, HttpOperation operation,
			HttpRequest request, CustomHandler handler)
		{
			switch (Type) {
			case CustomHostType.Default:
				ctx.Assert (request.Headers["Host"], Is.EqualTo ("custom"), "host");
				break;

			case CustomHostType.HostWithPort:
				ctx.Assert (request.Headers["Host"], Is.EqualTo ("custom:8888"), "host");
				break;

			case CustomHostType.DefaultPort:
				var defaultPort = handler.TestRunner.Server.UseSSL ? 443 : 80;
				ctx.Assert (request.Headers["Host"], Is.EqualTo ($"custom:{defaultPort}"), "host");
				break;

			case CustomHostType.ImplicitHost:
				var uri = handler.TestRunner.Server.Uri;
				var hostAndPort = uri.GetComponents (UriComponents.HostAndPort, UriFormat.Unescaped);
				ctx.Assert (request.Headers["Host"], Is.EqualTo (hostAndPort), "host");
				break;

			default:
				throw ctx.AssertFail (Type);
			}

			return new HttpResponse (HttpStatusCode.OK, ExpectedContent);
		}
	}
}
