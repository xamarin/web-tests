//
// RequestClientCertificate.cs
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
using System.Security.Cryptography.X509Certificates;
using Xamarin.AsyncTests;
using Xamarin.AsyncTests.Constraints;

namespace Xamarin.WebTests.HttpRequestTests
{
	using ConnectionFramework;
	using TestAttributes;
	using TestFramework;
	using HttpFramework;
	using HttpHandlers;
	using TestRunners;
	using Resources;

	[New]
	[Renegotiation]
	[HttpServerFlags (HttpServerFlags.RequireRenegotiation)]
	public class RequestClientCertificate : RequestTestFixture
	{
		public override RequestFlags RequestFlags => RequestFlags.CloseConnection;

		public override HttpContent ExpectedContent => HttpContent.TheQuickBrownFox;

		public override bool HasRequestBody => false;

		protected override void CreateParameters (TestContext ctx, ConnectionParameters parameters)
		{
			var provider = DependencyInjector.Get<ICertificateProvider> ();
			parameters.AskForClientCertificate = true;
			parameters.ServerCertificateValidator = provider.AcceptThisCertificate (ResourceManager.MonkeyCertificate);
			base.CreateParameters (ctx, parameters);
		}

		protected override void ConfigureRequest (TestContext ctx, InstrumentationOperation operation, TraditionalRequest request)
		{
			request.RequestExt.SetClientCertificates (new X509CertificateCollection {
				ResourceManager.MonkeyCertificate
			});
			base.ConfigureRequest (ctx, operation, request);
		}

		public override HttpResponse HandleRequest (
			TestContext ctx, InstrumentationOperation operation,
			HttpConnection connection, HttpRequest request)
		{
			ctx.Assert (connection.SslStream.IsMutuallyAuthenticated, "IsMutuallyAuthenticated");
			ctx.Assert (connection.SslStream.RemoteCertificate, Is.EqualTo (ResourceManager.MonkeyCertificate));
			return new HttpResponse (HttpStatusCode.OK, ExpectedContent);
		}
	}
}
