//
// TestSessionClient.cs
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
using System.Xml.Linq;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Xamarin.AsyncTests.Remoting
{
	using Framework;

	class TestSessionClient : TestSession, ObjectClient<TestSessionClient>, RemoteTestSession
	{
		public Connection Connection {
			get;
			private set;
		}

		public long ObjectID {
			get;
			private set;
		}

		public string Type {
			get { return "TestSession"; }
		}

		public override TestSuite Suite {
			get { return suite; }
		}

		TestSuiteClient suite;

		public TestSessionClient (ClientConnection connection, long objectID)
			: base (connection.App, connection.LocalFramework)
		{
			Connection = connection;
			ObjectID = objectID;
		}

		TestSessionClient RemoteObject<TestSessionClient, TestSessionServant>.Client {
			get { return this; }
		}

		TestSessionServant RemoteObject<TestSessionClient, TestSessionServant>.Servant {
			get { throw new ServerErrorException (); }
		}

		internal static async Task<TestSessionClient> FromProxy (ObjectProxy proxy, CancellationToken cancellationToken)
		{
			var session = (TestSessionClient)proxy;
			if (session.suite != null)
				return session;

			await RemoteObjectManager.GetRemoteTestConfiguration (session, cancellationToken).ConfigureAwait (false);

			session.suite = await RemoteObjectManager.GetRemoteTestSuite (session, cancellationToken).ConfigureAwait (false);
			return session;
		}

		public override Task<TestCase> GetRootTestCase (CancellationToken cancellationToken)
		{
			return RemoteObjectManager.GetRootTestCase (this, cancellationToken);
		}

		public override Task<TestCase> ResolveFromPath (XElement path, CancellationToken cancellationToken)
		{
			return RemoteObjectManager.ResolveFromPath (this, path, cancellationToken);
		}

		public override Task<IReadOnlyCollection<TestCase>> GetTestChildren (TestCase test, CancellationToken cancellationToken)
		{
			return ((TestCaseClient)test).GetTestChildren (cancellationToken);
		}

		public override Task<IReadOnlyCollection<TestCase>> GetTestParameters (TestCase test, CancellationToken cancellationToken)
		{
			return ((TestCaseClient)test).GetTestParameters (cancellationToken);
		}

		public override Task<TestResult> Run (TestCase test, CancellationToken cancellationToken)
		{
			return ((TestCaseClient)test).Run (cancellationToken);
		}
	}
}

