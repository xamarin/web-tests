//
// HttpsServerAttribute.cs
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
using Xamarin.AsyncTests;
using Xamarin.AsyncTests.Framework;
using Xamarin.AsyncTests.Portable;

namespace Xamarin.WebTests.Features
{
	using ConnectionFramework;
	using HttpFramework;
	using Portable;
	using Providers;
	using Resources;

	public class HttpsServerAttribute : HttpServerAttribute
	{
		public HttpsServerAttribute ()
			: base (typeof (HttpsServerAttribute))
		{
		}

		protected override IServerParameters GetServerParameters (TestContext ctx)
		{
			var mode = ctx.GetParameter<SslTestMode> ();

			SslStreamFlags flags;
			switch (mode) {
			case SslTestMode.Default:
				flags = SslStreamFlags.None;
				break;
			case SslTestMode.RejectServerCertificate:
				flags = SslStreamFlags.ExpectTrustFailure | SslStreamFlags.RejectServerCertificate;
				break;
			case SslTestMode.RequireClientCertificate:
				flags = SslStreamFlags.ProvideClientCertificate | SslStreamFlags.RequireClientCertificate;
				break;
			case SslTestMode.RejectClientCertificate:
				flags = SslStreamFlags.RejectClientCertificate | SslStreamFlags.ExpectError |
					SslStreamFlags.ProvideClientCertificate | SslStreamFlags.RequireClientCertificate;
				break;
			case SslTestMode.MissingClientCertificate:
				flags = SslStreamFlags.RequireClientCertificate | SslStreamFlags.ExpectError;
				break;
			default:
				throw new InvalidOperationException ();
			}

			ServerCertificateType certificateType;
			if (!ctx.TryGetParameter (out certificateType))
				certificateType = ServerCertificateType.Default;

			var certificate = ResourceManager.GetServerCertificate (certificateType);

			return new ServerParameters ("https", certificate) {
				SslStreamFlags = flags
			};
		}
	}
}

