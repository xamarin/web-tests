//
// SslStreamTestRunner.cs
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
using System.Threading;
using System.Threading.Tasks;
using Xamarin.AsyncTests;
using Xamarin.AsyncTests.Constraints;

namespace Xamarin.WebTests.TestRunners
{
	using ConnectionFramework;

	public class SslStreamTestRunner : ClientAndServerTestRunner
	{
		public SslStreamTestRunner (IServer server, IClient client)
			: base (server, client)
		{
		}

		public SslStreamTestRunner (IServer server, IClient client, ClientAndServerParameters parameters)
			: base (server, client, parameters)
		{
		}

		protected override async Task OnRun (TestContext ctx, CancellationToken cancellationToken)
		{
			await base.OnRun (ctx, cancellationToken);

			ctx.Expect (Client.SslStream.IsAuthenticated, "client is authenticated");
			ctx.Expect (Server.SslStream.IsAuthenticated, "server is authenticated");

			ctx.Expect (Client.SslStream.HasRemoteCertificate, "client has server certificate");

			if (Server.Parameters.AskForCertificate && Client.Parameters.ClientCertificate != null)
				ctx.Expect (Client.SslStream.HasLocalCertificate, "client has local certificate");

			if (Server.Parameters.RequireCertificate) {
				ctx.Expect (Server.SslStream.IsMutuallyAuthenticated, "server is mutually authenticated");
				ctx.Expect (Server.SslStream.HasRemoteCertificate, "server has client certificate");
			}

			if (!Server.Parameters.AskForCertificate)
				ctx.Expect (Server.SslStream.HasRemoteCertificate, Is.False, "server has no client certificate");
		}
	}
}

