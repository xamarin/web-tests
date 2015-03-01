//
// TestFrameworkClient.cs
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

namespace Xamarin.AsyncTests.Server
{
	using Portable;
	using Framework;

	class TestFrameworkClient : TestFramework, ObjectClient<TestFrameworkClient>, RemoteTestFramework
	{
		public Connection Connection {
			get;
			private set;
		}

		public long ObjectID {
			get;
			private set;
		}

		public TestFramework LocalFramework {
			get;
			private set;
		}

		public string Type {
			get { return "TestFramework"; }
		}

		public TestFrameworkClient (ClientConnection connection, long objectID)
		{
			Connection = connection;
			ObjectID = objectID;

			LocalFramework = connection.LocalFramework;
		}

		TestFrameworkClient RemoteObject<TestFrameworkClient,TestFrameworkServant>.Client {
			get { return this; }
		}

		TestFrameworkServant RemoteObject<TestFrameworkClient,TestFrameworkServant>.Servant {
			get { throw new ServerErrorException (); }
		}

		public override TestName Name {
			get { return LocalFramework.Name; }
		}

		public override IPortableSupport PortableSupport {
			get { return LocalFramework.PortableSupport; }
		}

		protected override TestLogger Logger {
			get { return Connection.App.Logger; }
		}

		public override TestConfiguration Configuration {
			get { return LocalFramework.Configuration; }
		}

		public override Task<TestSuite> LoadTestSuite (CancellationToken cancellationToken)
		{
			return RemoteObjectManager.GetRemoteTestSuite (this, cancellationToken);
		}
	}
}

