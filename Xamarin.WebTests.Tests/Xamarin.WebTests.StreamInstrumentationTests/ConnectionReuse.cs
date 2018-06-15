//
// ConnectionReuse.cs
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
using System.Threading;
using System.Threading.Tasks;
using Xamarin.AsyncTests;
using Xamarin.AsyncTests.Constraints;

namespace Xamarin.WebTests.StreamInstrumentationTests
{
	using System.Net.Sockets;
	using ConnectionFramework;
	using TestAttributes;
	using TestFramework;
	using TestRunners;

	[ConnectionTestFlags (ConnectionTestFlags.RequireCleanShutdown)]
	public class ConnectionReuse : StreamInstrumentationTestFixture
	{
		protected override bool UseCleanShutdown => true;

		protected override bool NeedClientInstrumentation => true;

		protected override bool NeedServerInstrumentation => true;

		public bool CleanShutdown {
			get;
		}

		[AsyncTest]
		public ConnectionReuse (bool shutdown)
		{
			CleanShutdown = shutdown;
		}

		readonly TaskCompletionSource<bool> clientTcs = new TaskCompletionSource<bool> ();
		readonly TaskCompletionSource<bool> serverTcs = new TaskCompletionSource<bool> ();

		protected override StreamInstrumentation CreateClientInstrumentation (TestContext ctx, Connection connection, Socket socket)
		{
			return new StreamInstrumentation (ctx, ME, socket, false);
		}

		protected override StreamInstrumentation CreateServerInstrumentation (TestContext ctx, Connection connection, Socket socket)
		{
			return new StreamInstrumentation (ctx, ME, socket, false);
		}

		protected override async Task ClientShutdown (TestContext ctx, CancellationToken cancellationToken)
		{
			var me = $"{ME}({nameof (ClientShutdown)})";
			LogDebug (ctx, 4, me);

			if (CleanShutdown) {
				await Client.Shutdown (ctx, cancellationToken).ConfigureAwait (false);
				LogDebug (ctx, 4, $"{me} - client shutdown done");
			}

			Client.Close ();

			ClearClientInstrumentation ();

			await serverTcs.Task.ConfigureAwait (false);
			LogDebug (ctx, 4, $"{me} - server ready");

			try {
				LogDebug (ctx, 4, $"{me} - restarting client");
				await ((DotNetConnection)Client).Restart (ctx, cancellationToken).ConfigureAwait (false);
				LogDebug (ctx, 4, $"{me} - done restarting client");
			} catch (Exception ex) {
				LogDebug (ctx, 4, $"{me} - restarting client failed: {ex.Message}");
				throw;
			}

			Client.Close ();

			clientTcs.TrySetResult (true);
		}

		protected async override Task ServerShutdown (TestContext ctx, CancellationToken cancellationToken)
		{
			var me = $"{ME}({nameof (ServerShutdown)})";
			LogDebug (ctx, 4, me);

			Task<int> serverRead;
			if (CleanShutdown) {
				var buffer = new byte[256];
				serverRead = Server.Stream.ReadAsync (buffer, 0, buffer.Length);
			} else {
				serverRead = Task.FromResult (0);
			}

			await ctx.Assert (() => serverRead, Is.EqualTo (0), "read shutdown notify").ConfigureAwait (false);

			Server.Close ();

			LogDebug (ctx, 4, $"{me} - restarting: {ServerInstrumentation.Socket.LocalEndPoint}");

			ClearServerInstrumentation ();

			serverTcs.TrySetResult (true);

			try {
				await ((DotNetConnection)Server).Restart (ctx, cancellationToken).ConfigureAwait (false);
				LogDebug (ctx, 4, $"{me} - done restarting");
			} catch (Exception ex) {
				LogDebug (ctx, 4, $"{me} - failed to restart: {ex.Message}");
				throw;
			}

			await clientTcs.Task;

			Server.Close ();

			LogDebug (ctx, 4, $"{me} - done");
		}
	}
}
