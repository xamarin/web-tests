//
// ServicePointLimit.cs
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

namespace Xamarin.WebTests.ExternalTests
{
	using HttpFramework;
	using HttpHandlers;

	public class ServicePointLimit : ExternalTestFixture
	{
		public override ForkType ForkType => ForkType.Domain;

		protected override void Initialize (TestContext ctx)
		{
			RegisterServer ("one").RegisterHandler ("hello", RequestFlags.KeepAlive, r => HelloWorldResponse);
			RegisterServer ("two").RegisterHandler ("hello", RequestFlags.KeepAlive, r => HelloWorldResponse);
			RegisterServer ("three").RegisterHandler ("hello", RequestFlags.KeepAlive, r => HelloWorldResponse);
		}

		protected override void RemoteInitialize (TestContext ctx)
		{
			ServicePointManager.MaxServicePoints = 2;
			base.RemoteInitialize (ctx);
		}

		static HttpResponse HelloWorldResponse => new HttpResponse (HttpStatusCode.OK, HttpContent.HelloWorld);

		protected override async Task Run (TestContext ctx, CancellationToken cancellationToken)
		{
			var one = ServerRegistry["one"].HandlerRegistry["hello"];
			await TraditionalSend (ctx, one, cancellationToken).ConfigureAwait (false);

			var two = ServerRegistry["two"].HandlerRegistry["hello"];
			await TraditionalSend (ctx, two, cancellationToken).ConfigureAwait (false);

			var three = ServerRegistry["three"].HandlerRegistry["hello"];
			var request = new TraditionalRequest (three.Uri);
			using (var response = await request.Send (ctx, cancellationToken).ConfigureAwait (false)) {
				ctx.Assert (response.Status, Is.EqualTo (HttpStatusCode.InternalServerError));
			}
		}
	}
}
