//
// ClientAndServerHandlerFactory.cs
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

namespace Xamarin.WebTests.ConnectionFramework
{
	public abstract class ClientAndServerHandlerFactory
	{
		public abstract IConnectionHandler Create (ClientAndServer connection);

		public IConnectionHandler Create (IServer server, IClient client)
		{
			var connection = new ClientAndServer (server, client);
			return Create (connection);
		}

		public static readonly ClientAndServerHandlerFactory WaitForOkAndDone = new SimpleFactory (c => new WaitForOkAndDoneHandler (c));

		public static readonly ClientAndServerHandlerFactory HandshakeAndDone = new SimpleFactory (c => new HandshakeAndDoneHandler (c));

		delegate IConnectionHandler FactoryDelegate (ClientAndServer connection);

		class SimpleFactory : ClientAndServerHandlerFactory
		{
			FactoryDelegate func;

			public SimpleFactory (FactoryDelegate func)
			{
				this.func = func;
			}

			public override IConnectionHandler Create (ClientAndServer connection)
			{
				return func (connection);
			}
		}

		class WaitForOkAndDoneHandler : ClientAndServerHandler
		{
			public WaitForOkAndDoneHandler (ClientAndServer connection)
				: base (connection)
			{
			}

			protected override async Task MainLoop (TestContext ctx, ILineBasedStream serverStream, ILineBasedStream clientStream, CancellationToken cancellationToken)
			{
				await serverStream.WriteLineAsync ("OK");
				var line = await clientStream.ReadLineAsync ();
				if (!line.Equals ("OK"))
					throw new ConnectionException ("Got unexpected output '{0}'", line);
				await Shutdown (ctx, SupportsCleanShutdown, cancellationToken);
				Close ();
			}
		}

		class HandshakeAndDoneHandler : ClientAndServerHandler
		{
			public HandshakeAndDoneHandler (ClientAndServer connection)
				: base (connection)
			{
			}

			protected override async Task MainLoop (TestContext ctx, ILineBasedStream serverStream, ILineBasedStream clientStream, CancellationToken cancellationToken)
			{
				await serverStream.WriteLineAsync ("SERVER OK");
				var line = await clientStream.ReadLineAsync ();
				if (!line.Equals ("SERVER OK"))
					throw new ConnectionException ("Got unexpected output from server: '{0}'", line);
				await clientStream.WriteLineAsync ("CLIENT OK");
				line = await serverStream.ReadLineAsync ();
				if (!line.Equals ("CLIENT OK"))
					throw new ConnectionException ("Got unexpected output from client: '{0}'", line);
				await Shutdown (ctx, SupportsCleanShutdown, cancellationToken);
				Close ();
			}
		}
	}
}

