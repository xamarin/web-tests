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

		class GetRemoteTestLoggerCommand : CreateCommand<RemoteTestLogger, TestLogger, TestLoggerBackend>
		{
			protected override long CreateInstance (Connection connection)
			{
				return RemoteTestLogger.CreateServer (connection).ObjectID;
			}
		}

		public static async Task<TestLogger> GetRemoteTestLogger (
			Connection connection, CancellationToken cancellationToken)
		{
			var command = new GetRemoteTestLoggerCommand ();
			var objectID = await command.Send (connection, cancellationToken);
			var remote = RemoteTestLogger.CreateClient (connection, objectID);
			return remote.Instance;
		}

		class GetRemoteTestFrameworkCommand : CreateCommand<RemoteTestFramework, TestFramework, TestFramework>
		{
			protected override long CreateInstance (Connection connection)
			{
				return RemoteTestFramework.CreateServer (connection).ObjectID;
			}
		}

		public static async Task<TestFramework> GetRemoteTestFramework (
			Connection connection, CancellationToken cancellationToken)
		{
			var command = new GetRemoteTestFrameworkCommand ();
			var objectID = await command.Send (connection, cancellationToken);
			var remote = RemoteTestFramework.CreateClient (connection, objectID);
			return remote.Instance;
		}
	}
}

