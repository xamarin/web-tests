//
// RemoteObjectManager.cs
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
using System.Xml.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Xamarin.AsyncTests.Server
{
	using Framework;

	static class RemoteObjectManager
	{
		internal static RemoteObject<U,V>.ClientProxy CreateClient<T,U,V> (Connection connection, long objectId)
			where T : RemoteObject<U,V>, new()
		{
			return new RemoteObject<U,V>.ClientProxy (connection, new T (), objectId);
		}

		abstract class CreateCommand<T,U,V> : Command<object,long>
			where T : RemoteObject<U,V>, new()
		{
			protected override Serializer<object> ArgumentSerializer {
				get { return null; }
			}

			protected override Serializer<long> ResponseSerializer {
				get { return Serializer.ObjectID; }
			}

			protected override Task<long> Run (Connection connection, object argument, CancellationToken cancellationToken)
			{
				return Task.Run (() => CreateInstance (connection));
			}

			protected abstract long CreateInstance (Connection connection);
		}

		class GetRemoteTestFrameworkCommand : CreateCommand<RemoteTestFramework, TestFrameworkClient, TestFrameworkServant>
		{
			protected override long CreateInstance (Connection connection)
			{
				return RemoteTestFramework.CreateServer ((ServerConnection)connection).ObjectID;
			}
		}

		public static async Task<TestFramework> GetRemoteTestFramework (
			ClientConnection connection, CancellationToken cancellationToken)
		{
			var command = new GetRemoteTestFrameworkCommand ();
			var objectID = await command.Send (connection, cancellationToken);
			var remote = RemoteTestFramework.CreateClient (connection, objectID);
			return remote.Instance;
		}

		class GetRemoteTestSuiteCommand : ObjectCommand<TestFrameworkClient,TestFrameworkServant,object,long>
		{
			protected override Serializer<object> ArgumentSerializer {
				get { return null; }
			}

			protected override Serializer<long> ResponseSerializer {
				get { return Serializer.ObjectID; }
			}

			protected override async Task<long> Run (
				Connection connection, TestFrameworkServant servant, object argument, CancellationToken cancellationToken)
			{
				var serverConnection = (ServerConnection)connection;
				var suite = RemoteTestSuite.CreateServer (serverConnection, servant);
				await suite.Instance.Initialize (serverConnection.Logger, cancellationToken);
				return suite.ObjectID;
			}
		}

		public static async Task<TestSuite> GetRemoteTestSuite (
			RemoteTestFramework.ClientProxy framework, CancellationToken cancellationToken)
		{
			var command = new GetRemoteTestSuiteCommand ();
			var objectID = await command.Send (framework, null, cancellationToken);
			var remote = RemoteTestSuite.CreateClient (framework, objectID);
			return remote.Instance;
		}

		class ResolveTestSuiteCommand : ObjectCommand<TestSuiteClient,TestSuiteServant,object,long>
		{
			protected override Serializer<object> ArgumentSerializer {
				get { return null; }
			}

			protected override Serializer<long> ResponseSerializer {
				get { return Serializer.ObjectID; }
			}

			protected override async Task<long> Run (
				Connection connection, TestSuiteServant servant, object argument, CancellationToken cancellationToken)
			{
				var test = await servant.Resolve (cancellationToken);
				var suite = RemoteTestCase.CreateServer ((ServerConnection)connection, servant, test);
				return suite.ObjectID;
			}
		}

		class GetTestPathCommand : ObjectCommand<TestCaseClient,TestCaseServant,object,XElement>
		{
			protected override Serializer<object> ArgumentSerializer {
				get { return null; }
			}

			protected override Serializer<XElement> ResponseSerializer {
				get { return Serializer.Element; }
			}

			protected override Task<XElement> Run (
				Connection connection, TestCaseServant servant, object argument, CancellationToken cancellationToken)
			{
				return Task.Run (() => {
					return servant.Serialize ();
				});
			}
		}

		public static async Task<TestCase> ResolveTestSuite (
			RemoteTestSuite.ClientProxy suite, CancellationToken cancellationToken)
		{
			var command = new ResolveTestSuiteCommand ();
			var objectID = await command.Send (suite, null, cancellationToken);
			var remote = RemoteTestCase.CreateClient (suite, objectID);

			var getPathCommand = new GetTestPathCommand ();
			var testPath = await getPathCommand.Send (remote, null, cancellationToken);

			remote.Instance.SerializedPath = testPath;

			return remote.Instance;
		}

	}
}

