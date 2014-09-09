//
// RemoteTestFramework.cs
//
// Author:
//       Martin Baulig <martin.baulig@xamarin.com>
//
// Copyright (c) 2014 Xamarin Inc. (http://www.xamarin.com)
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

namespace Xamarin.AsyncTests.Server
{
	using Framework;

	class RemoteTestFramework : RemoteObject<TestFramework,TestFramework>
	{
		internal static ServerProxy CreateServer (Connection connection)
		{
			return new ServerProxy (connection, new RemoteTestFramework ());
		}

		internal static ClientProxy CreateClient (Connection connection, long objectId)
		{
			return new ClientProxy (connection, new RemoteTestFramework (), objectId);
		}

		protected override TestFramework CreateClientProxy (ClientProxy proxy)
		{
			return new FrameworkClient (proxy);
		}

		protected override TestFramework CreateServerProxy (Connection connection)
		{
			return new FrameworkServer (connection);
		}

		class FrameworkServer : TestFramework
		{
			readonly Connection connection;

			public FrameworkServer (Connection connection)
			{
				this.connection = connection;
			}

			public override Task<TestSuite> LoadTestSuite (TestApp app, CancellationToken cancellationToken)
			{
				return connection.GetLocalTestSuite (cancellationToken);
			}
		}

		class FrameworkClient : TestFramework
		{
			readonly ClientProxy proxy;

			public FrameworkClient (ClientProxy proxy)
			{
				this.proxy = proxy;
			}

			public override Task<TestSuite> LoadTestSuite (TestApp app, CancellationToken cancellationToken)
			{
				var command = new LoadTestSuiteCommand ();
				return command.Send (proxy, null, cancellationToken);
			}
		}

		class LoadTestSuiteCommand : ObjectCommand<TestFramework,TestFramework,object,TestSuite>
		{
			protected override Serializer<object> ArgumentSerializer {
				get { return null; }
			}

			protected override Serializer<TestSuite> ResponseSerializer {
				get { return Serializer.TestSuite; }
			}

			protected override Task<TestSuite> Run (
				Connection connection, TestFramework server, object argument, CancellationToken cancellationToken)
			{
				return connection.GetLocalTestSuite (cancellationToken);
			}
		}

	}
}

