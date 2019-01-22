//
// StreamInstrumentationTestRunner.cs
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
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Net.Security;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using Xamarin.AsyncTests;
using Xamarin.AsyncTests.Constraints;
using Xamarin.AsyncTests.Framework;
using Xamarin.AsyncTests.Portable;

namespace Xamarin.WebTests.TestRunners
{
	using ConnectionFramework;
	using TestAttributes;
	using TestFramework;
	using Resources;

	public abstract class StreamInstrumentationTestRunner : ConnectionTestRunner, IConnectionInstrumentation
	{
		StreamInstrumentation clientInstrumentation;
		StreamInstrumentation serverInstrumentation;

		protected StreamInstrumentation ClientInstrumentation => clientInstrumentation;
		protected StreamInstrumentation ServerInstrumentation => serverInstrumentation;

		protected override Task PreRun (TestContext ctx, CancellationToken cancellationToken)
		{
			return base.PreRun (ctx, cancellationToken);
		}

		protected override Task PostRun (TestContext ctx, CancellationToken cancellationToken)
		{
			if (clientInstrumentation != null) {
				clientInstrumentation.Dispose ();
				clientInstrumentation = null;
			}
			if (serverInstrumentation != null) {
				serverInstrumentation.Dispose ();
				serverInstrumentation = null;
			}

			return base.PostRun (ctx, cancellationToken);
		}

		protected override string LogCategory => LogCategories.StreamInstrumentationTestRunner;

		protected virtual bool HandshakeFails => false;

		protected virtual bool NeedClientInstrumentation => false;

		protected virtual bool NeedServerInstrumentation => false;

		protected override Task StartClient (TestContext ctx, CancellationToken cancellationToken)
		{
			if (NeedClientInstrumentation)
				return Client.Start (ctx, this, cancellationToken);
			return Client.Start (ctx, null, cancellationToken);
		}

		protected override Task StartServer (TestContext ctx, CancellationToken cancellationToken)
		{
			if (NeedServerInstrumentation)
				return Server.Start (ctx, this, cancellationToken);
			return Server.Start (ctx, null, cancellationToken);
		}

		protected override Task ClientShutdown (TestContext ctx, CancellationToken cancellationToken)
		{
			if (HandshakeFails)
				return FinishedTask;
			return Client.Shutdown (ctx, cancellationToken);
		}

		protected override Task ServerShutdown (TestContext ctx, CancellationToken cancellationToken)
		{
			if (HandshakeFails)
				return FinishedTask;
			return Server.Shutdown (ctx, cancellationToken);
		}

		protected void ClearClientInstrumentation ()
		{
			clientInstrumentation = null;
		}

		protected void ClearServerInstrumentation ()
		{
			serverInstrumentation = null;
		}

		protected virtual void ConfigureClientStream (TestContext ctx, StreamInstrumentation instrumentation)
		{
		}

		protected virtual void ConfigureServerStream (TestContext ctx, StreamInstrumentation instrumentation)
		{
		}

		protected virtual StreamInstrumentation CreateClientInstrumentation (TestContext ctx, Connection connection, Socket socket)
		{
			return new StreamInstrumentation (ctx, ME, socket, true);
		}

		protected virtual StreamInstrumentation CreateServerInstrumentation (TestContext ctx, Connection connection, Socket socket)
		{
			return new StreamInstrumentation (ctx, ME, socket, true);
		}

		public Stream CreateClientStream (TestContext ctx, Connection connection, Socket socket)
		{
			var instrumentation = CreateClientInstrumentation (ctx, connection, socket);

			if (Interlocked.CompareExchange (ref clientInstrumentation, instrumentation, null) != null)
				throw new InternalErrorException ();

			ctx.LogDebug (LogCategory, 4, $"{ME}.{nameof (CreateClientStream)}");

			ConfigureClientStream (ctx, instrumentation);

			return instrumentation;
		}

		public Stream CreateServerStream (TestContext ctx, Connection connection, Socket socket)
		{
			var instrumentation = CreateServerInstrumentation (ctx, connection, socket);

			if (Interlocked.CompareExchange (ref serverInstrumentation, instrumentation, null) != null)
				throw new InternalErrorException ();

			ctx.LogDebug (LogCategory, 4, $"{ME}.{nameof (CreateServerStream)}");

			ConfigureServerStream (ctx, instrumentation);

			return instrumentation;
		}

		protected virtual Task<bool> ClientHandshake (TestContext ctx, Func<Task> handshake, StreamInstrumentation instrumentation)
		{
			return Task.FromResult (false);
		}

		protected virtual Task<bool> ServerHandshake (TestContext ctx, Func<Task> handshake, StreamInstrumentation instrumentation)
		{
			return Task.FromResult (false);
		}

		public Task<bool> ClientHandshake (TestContext ctx, Func<Task> handshake, Connection connection)
		{
			return ClientHandshake (ctx, handshake, clientInstrumentation);
		}

		public Task<bool> ServerHandshake (TestContext ctx, Func<Task> handshake, Connection connection)
		{
			return ServerHandshake (ctx, handshake, serverInstrumentation);
		}
	}
}
