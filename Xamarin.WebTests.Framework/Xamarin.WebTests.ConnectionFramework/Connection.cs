﻿using System;
using System.IO;
using System.Net;
using System.Net.Security;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Xamarin.AsyncTests;
using Xamarin.AsyncTests.Portable;

namespace Xamarin.WebTests.ConnectionFramework
{
	public abstract class Connection : IDisposable
	{
		public ConnectionProvider Provider {
			get;
		}

		public EndPoint EndPoint {
			get;
		}

		public ConnectionParameters Parameters {
			get;
		}

		public bool SupportsCleanShutdown => Provider.SupportsCleanShutdown;

		public ProtocolVersions SupportedProtocols => Provider.SupportedProtocols;

		protected Connection (ConnectionProvider provider, ConnectionParameters parameters)
		{
			Provider = provider;
			EndPoint = GetEndPoint (parameters);
			Parameters = parameters;
		}

		static EndPoint GetEndPoint (ConnectionParameters parameters)
		{
			if (parameters.EndPoint != null)
				return parameters.EndPoint;

			return new IPEndPoint (IPAddress.Loopback, 4433);
		}

		public abstract Stream Stream {
			get;
		}

		public abstract SslStream SslStream {
			get;
		}

		public ProtocolVersions ProtocolVersion => (ProtocolVersions)SslStream.SslProtocol;

		protected internal static Task FinishedTask {
			get { return Task.FromResult<object> (null); }
		}

		[StackTraceEntryPoint]
		public abstract Task Start (TestContext ctx, IConnectionInstrumentation instrumentation, CancellationToken cancellationToken);

		[StackTraceEntryPoint]
		public abstract Task WaitForConnection (TestContext ctx, CancellationToken cancellationToken);

		[StackTraceEntryPoint]
		public abstract Task Shutdown (TestContext ctx, CancellationToken cancellationToken);

		[StackTraceEntryPoint]
		public abstract void Close ();

		protected abstract void Destroy ();

		int disposed;

		public void Dispose ()
		{
			Dispose (true);
			GC.SuppressFinalize (this);
		}

		protected virtual void Dispose (bool disposing)
		{
			if (Interlocked.CompareExchange (ref disposed, 1, 0) != 0)
				return;
			Destroy ();
		}
	}
}

