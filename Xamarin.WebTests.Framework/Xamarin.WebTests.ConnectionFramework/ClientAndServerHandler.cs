using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Xamarin.WebTests.ConnectionFramework
{
	public abstract class ClientAndServerHandler : IConnectionHandler
	{
		public ClientAndServer Connection {
			get;
			private set;
		}

		public ClientAndServerHandler (IServer server, IClient client)
		{
			Connection = new ClientAndServer (server, client, ClientAndServerParameters.Create (client.Parameters, server.Parameters));
		}

		public ClientAndServerHandler (ClientAndServer connection)
		{
			Connection = connection;
		}

		public bool SupportsCleanShutdown {
			get { return Connection.SupportsCleanShutdown; }
		}

		public Task WaitForConnection ()
		{
			return Connection.WaitForConnection ();
		}

		public async Task Run ()
		{
			await WaitForConnection ();
			var serverWrapper = new StreamWrapper (Connection.Server.Stream);
			var clientWrapper = new StreamWrapper (Connection.Client.Stream);
			await MainLoop (serverWrapper, clientWrapper);
		}

		protected abstract Task MainLoop (ILineBasedStream serverStream, ILineBasedStream clientStream);

		public Task<bool> Shutdown (bool attemptCleanShutdown, bool waitForReply)
		{
			return Connection.Shutdown (attemptCleanShutdown, waitForReply);
		}

		public void Close ()
		{
			Connection.Dispose ();
		}
	}
}

