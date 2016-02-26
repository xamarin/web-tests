using System;
using System.IO;
using System.Net;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Xamarin.AsyncTests;
using Xamarin.AsyncTests.Portable;

namespace Xamarin.WebTests.ConnectionFramework
{
	public abstract class Connection : IConnection, IDisposable
	{
		public abstract bool SupportsCleanShutdown {
			get;
		}

		public IPortableEndPoint EndPoint {
			get;
			private set;
		}

		public ConnectionParameters Parameters {
			get;
			private set;
		}

		public abstract ProtocolVersions SupportedProtocols {
			get;
		}

		protected Connection (IPortableEndPoint endpoint, ConnectionParameters parameters)
		{
			EndPoint = endpoint;
			Parameters = parameters;
		}

		public abstract Task Start (TestContext ctx, CancellationToken cancellationToken);

		public abstract Task WaitForConnection (TestContext ctx, CancellationToken cancellationToken);

		public abstract Task<bool> Shutdown (TestContext ctx, CancellationToken cancellationToken);

		protected abstract void Stop ();

		protected internal static Task FinishedTask {
			get { return Task.FromResult<object> (null); }
		}

		#region ITestInstance implementation

		public async Task Initialize (TestContext ctx, CancellationToken cancellationToken)
		{
			await Start (ctx, cancellationToken);
		}

		public virtual Task PreRun (TestContext ctx, CancellationToken cancellationToken)
		{
			return FinishedTask;
		}

		public virtual Task PostRun (TestContext ctx, CancellationToken cancellationToken)
		{
			return FinishedTask;
		}

		public Task Destroy (TestContext ctx, CancellationToken cancellationToken)
		{
			return Task.Run (() => {
				Dispose ();
			});
		}

		#endregion

		public void Dispose ()
		{
			Dispose (true);
			GC.SuppressFinalize (this);
		}

		bool disposed;

		protected virtual void Dispose (bool disposing)
		{
			lock (this) {
				if (disposed)
					return;
				disposed = true;
			}
			Stop ();
		}

		~Connection ()
		{
			Dispose (false);
		}
	}
}

