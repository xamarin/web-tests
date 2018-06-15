//
// SocketTestFixture.cs
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
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Net.Security;
using System.Threading;
using System.Threading.Tasks;

namespace Xamarin.WebTests.SocketTests
{
	using ConnectionFramework;
	using TestAttributes;
	using TestFramework;
	using TestRunners;
	using Xamarin.AsyncTests;

	[AsyncTestFixture (Prefix = "SocketTests")]
	public abstract class SocketTestFixture : ConnectionTestRunner
	{
		public ConnectionTestProvider Provider {
			get;
		}

		SocketConnectionProvider ConnectionProvider {
			get;
		}

		protected SocketTestFixture ()
		{
			ConnectionProvider = new SocketConnectionProvider (this);
			Provider = new SocketConnectionTestProvider (ConnectionProvider, ConnectionTestFlags.None);
		}

		protected override ConnectionTestProvider GetProvider (TestContext ctx) => Provider;

		[AsyncTest]
		public static Task Run (TestContext ctx, CancellationToken cancellationToken, SocketTestFixture fixture)
		{
			return fixture.Run (ctx, cancellationToken);
		}

		[AsyncTest]
		[Martin (null, UseFixtureName = true)]
		public static Task MartinTest (TestContext ctx, CancellationToken cancellationToken, SocketTestFixture fixture)
		{
			return fixture.Run (ctx, cancellationToken);
		}

		protected sealed override string LogCategory => LogCategories.SocketTests;

		protected sealed override ConnectionParameters CreateParameters (TestContext ctx)
		{
			var parameters = new ConnectionParameters (null);
			CreateParameters (ctx, parameters);
			return parameters;
		}

		protected virtual void CreateParameters (TestContext ctx, ConnectionParameters parameters)
		{
		}

		protected abstract Task<Socket> StartServer (TestContext ctx, EndPoint endPoint, CancellationToken cancellationToken);

		protected abstract Task<Socket> StartClient (TestContext ctx, EndPoint endPoint, CancellationToken cancellationToken);

		class SocketConnectionProvider : ConnectionProvider
		{
			public SocketTestFixture Fixture {
				get;
			}

			internal SocketConnectionProvider (SocketTestFixture fixture)
				: base (ConnectionProviderType.Custom, ConnectionProviderFlags.SupportsCleanShutdown)
			{
				Fixture = fixture;
			}

			public override ProtocolVersions SupportedProtocols => throw new NotSupportedException ();

			public override Connection CreateClient (ConnectionParameters parameters)
			{
				return new SocketConnection (Fixture, parameters, false);
			}

			public override Connection CreateServer (ConnectionParameters parameters)
			{
				return new SocketConnection (Fixture, parameters, true);
			}

			protected override ISslStreamProvider GetSslStreamProvider () => throw new NotSupportedException ();
		}

		class SocketConnectionTestProvider : ConnectionTestProvider
		{
			public SocketConnectionTestProvider (SocketConnectionProvider provider, ConnectionTestFlags flags)
				: base (provider, provider, flags)
			{
			}
		}

		class SocketConnection : Connection
		{
			public SocketTestFixture Fixture {
				get;
			}

			public string ME {
				get;
			}

			public SocketConnection (SocketTestFixture fixture, ConnectionParameters parameters, bool isServer)
				: base (fixture.ConnectionProvider, parameters,
				        isServer ? parameters.ListenAddress ?? parameters.EndPoint : parameters.EndPoint)
			{
				Fixture = fixture;
				IsServer = isServer;
				ME = $"{Fixture.ME}[SocketConnection {(IsServer ? "server" : "client")},{EndPoint}]";
			}

			Stream innerStream;
			Task<Socket> socketTask;
			int started;

			public override Stream Stream => innerStream;

			public override SslStream SslStream => throw new NotSupportedException ();

			protected bool IsServer {
				get;
			}

			public sealed override Task Start (TestContext ctx, IConnectionInstrumentation instrumentation, CancellationToken cancellationToken)
			{
				if (Interlocked.CompareExchange (ref started, 1, 0) != 0)
					throw new InvalidOperationException ("Duplicated call to Start().");
				if (instrumentation != null)
					throw new NotSupportedException ();

				var me = $"{ME}.Start()";
				ctx.LogDebug (Fixture.LogCategory, 1, me);

				var startTask = IsServer ?
					Fixture.StartServer (ctx, EndPoint, cancellationToken) :
				               Fixture.StartClient (ctx, EndPoint, cancellationToken);

				socketTask = startTask.ContinueWith (Continuation);

				return FinishedTask;

				Socket Continuation (Task<Socket> task)
				{
					ctx.LogDebug (Fixture.LogCategory, 1, $"{me} done: {task.Status}");
					cancellationToken.ThrowIfCancellationRequested ();
					if (task.IsCanceled || task.IsFaulted)
						throw task.Exception;
					innerStream = ctx.RegisterDispose (new NetworkStream (task.Result, true));
					return task.Result;
				}
			}

			public sealed override Task WaitForConnection (TestContext ctx, CancellationToken cancellationToken)
			{
				return socketTask;
			}

			public sealed override Task Shutdown (TestContext ctx, CancellationToken cancellationToken)
			{
				var me = $"{ME}.Shutdown()";
				ctx.LogDebug (Fixture.LogCategory, 1, me);
				return FinishedTask;
			}

			public override void Close ()
			{
			}

			protected override void Destroy ()
			{
			}
		}
	}
}
