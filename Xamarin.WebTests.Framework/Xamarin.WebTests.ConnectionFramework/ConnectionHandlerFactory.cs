using System;
using System.Threading;
using System.Threading.Tasks;
using Xamarin.AsyncTests;

namespace Xamarin.WebTests.ConnectionFramework
{
	public abstract class ConnectionHandlerFactory
	{
		public abstract IConnectionHandler Create (IConnection connection);

		public static readonly ConnectionHandlerFactory OkAndDone = new SimpleFactory (c => new OkAndDoneHandler (c));

		public static readonly ConnectionHandlerFactory Echo = new SimpleFactory (c => new EchoHandler (c));

		delegate IConnectionHandler FactoryDelegate (IConnection connection);

		class SimpleFactory : ConnectionHandlerFactory
		{
			FactoryDelegate func;

			public SimpleFactory (FactoryDelegate func)
			{
				this.func = func;
			}

			public override IConnectionHandler Create (IConnection connection)
			{
				return func (connection);
			}
		}

		class OkAndDoneHandler : CommonConnectionHandler
		{
			public OkAndDoneHandler (IConnection connection)
				: base ((ICommonConnection)connection)
			{
			}

			protected override async Task MainLoop (TestContext ctx, ILineBasedStream stream, CancellationToken cancellationToken)
			{
				await stream.WriteLineAsync ("OK");
				await Shutdown (ctx, Connection.SupportsCleanShutdown, true, cancellationToken);
				Connection.Dispose ();
			}
		}

		class EchoHandler : CommonConnectionHandler
		{
			public EchoHandler (IConnection connection)
				: base ((ICommonConnection)connection)
			{
			}

			protected override async Task MainLoop (TestContext ctx, ILineBasedStream stream, CancellationToken cancellationToken)
			{
				string line;
				while ((line = await stream.ReadLineAsync ()) != null)
					await stream.WriteLineAsync (line);
			}
		}
	}
}

