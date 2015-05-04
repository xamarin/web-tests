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
	using Portable;

	public abstract class Connection : IConnection, IDisposable
	{
		public abstract bool SupportsCleanShutdown {
			get;
		}

		public IPortableEndPoint EndPoint {
			get;
			private set;
		}

		public IConnectionParameters Parameters {
			get;
			private set;
		}

		protected Connection (IPortableEndPoint endpoint, IConnectionParameters parameters)
		{
			EndPoint = endpoint;
			Parameters = parameters;
		}

		public abstract Task Start (TestContext ctx, CancellationToken cancellationToken);

		public abstract Task WaitForConnection ();

		public abstract Task<bool> Shutdown (bool attemptCleanShutdown, bool waitForReply);

		protected abstract void Stop ();

		protected internal static Task FinishedTask {
			get { return Task.FromResult<object> (null); }
		}

		#region ITestInstance implementation

		public async Task Initialize (TestContext ctx, CancellationToken cancellationToken)
		{
			ctx.LogMessage ("Initialize: {0}", this);
			await Start (ctx, cancellationToken);
			ctx.LogMessage ("Initialize #1: {0}", this);
		}

		public Task PreRun (TestContext ctx, CancellationToken cancellationToken)
		{
			return FinishedTask;
		}

		public Task PostRun (TestContext ctx, CancellationToken cancellationToken)
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

