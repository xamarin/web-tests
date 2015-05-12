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

	public class SslStreamTestRunner : ClientAndServer
	{
		public SslStreamTestRunner (IServer server, IClient client)
			: base (server, client)
		{
		}

		public SslStreamTestRunner (IServer server, IClient client, ClientAndServerParameters parameters)
			: base (server, client, parameters)
		{
		}

		public async Task Run (TestContext ctx, CancellationToken cancellationToken)
		{
			try {
				await WaitForConnection (ctx, cancellationToken);
			} catch (ConnectionFinishedException) {
				return;
			}

			if ((Server.Parameters.Flags & ServerFlags.ExpectServerException) != 0)
				ctx.AssertFail ("expecting server exception");
			if ((Server.Parameters.Flags & ServerFlags.ClientAbortsHandshake) != 0)
				ctx.AssertFail ("expecting client to abort handshake");
			if ((Client.Parameters.Flags & (ClientFlags.ExpectTrustFailure | ClientFlags.ExpectWebException)) != 0)
				ctx.AssertFail ("expecting client exception");

			ctx.Expect (Client.SslStream.IsAuthenticated, "client is authenticated");
			ctx.Expect (Server.SslStream.IsAuthenticated, "server is authenticated");

			ctx.Expect (Client.SslStream.HasRemoteCertificate, "client has server certificate");

			if (Server.Parameters.AskForCertificate && Client.Parameters.ClientCertificate != null)
				ctx.Expect (Client.SslStream.HasLocalCertificate, "client has local certificate");

			if (Server.Parameters.RequireCertificate) {
				ctx.Expect (Server.SslStream.IsMutuallyAuthenticated, "server is mutually authenticated");
				ctx.Expect (Server.SslStream.HasRemoteCertificate, "server has client certificate");
			}

			var serverWrapper = new StreamWrapper (Server.Stream);
			var clientWrapper = new StreamWrapper (Client.Stream);
			await MainLoop (ctx, serverWrapper, clientWrapper, cancellationToken);
		}

		protected override async Task WaitForServerConnection (TestContext ctx, CancellationToken cancellationToken)
		{
			try {
				await base.WaitForServerConnection (ctx, cancellationToken);
				if ((Server.Parameters.Flags & ServerFlags.ExpectServerException) != 0)
					ctx.AssertFail ("expecting exception");
			} catch {
				if ((Server.Parameters.Flags & (ServerFlags.ClientAbortsHandshake | ServerFlags.ExpectServerException)) != 0)
					throw new ConnectionFinishedException ();
				throw;
			}
		}

		protected override async Task WaitForClientConnection (TestContext ctx, CancellationToken cancellationToken)
		{
			try {
				await base.WaitForClientConnection (ctx, cancellationToken);
			} catch {
				if ((Client.Parameters.Flags & (ClientFlags.ExpectWebException|ClientFlags.ExpectTrustFailure)) != 0)
					throw new ConnectionFinishedException ();
				throw;
			}
		}

		class ConnectionFinishedException : Exception
		{
		}

		protected async Task MainLoop (TestContext ctx, ILineBasedStream serverStream, ILineBasedStream clientStream, CancellationToken cancellationToken)
		{
			await serverStream.WriteLineAsync ("SERVER OK");
			var line = await clientStream.ReadLineAsync ();
			if (!line.Equals ("SERVER OK"))
				throw new ConnectionException ("Got unexpected output from server: '{0}'", line);
			await clientStream.WriteLineAsync ("CLIENT OK");
			line = await serverStream.ReadLineAsync ();
			if (!line.Equals ("CLIENT OK"))
				throw new ConnectionException ("Got unexpected output from client: '{0}'", line);
			await Shutdown (ctx, SupportsCleanShutdown, true, cancellationToken);
		}
	}
}

