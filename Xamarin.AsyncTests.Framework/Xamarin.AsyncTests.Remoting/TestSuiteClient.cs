//
// TestSuiteClient.cs
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
using System.Threading;
using System.Threading.Tasks;

namespace Xamarin.AsyncTests.Remoting
{
	using Framework;

	class TestSuiteClient : TestSuite, ObjectClient<TestSuiteClient>, RemoteTestSuite
	{
		public TestSessionClient Session {
			get;
			private set;
		}

		public Connection Connection {
			get;
			private set;
		}

		public long ObjectID {
			get;
			private set;
		}

		public string Type {
			get { return "TestSuite"; }
		}

		public string Name {
			get;
			private set;
		}

		public TestCase RootTestCase {
			get { return Session.RootTestCase; }
		}

		public TestSuiteClient (TestSessionClient session, long objectID)
		{
			Session = session;
			Connection = session.Connection;
			ObjectID = objectID;
		}

		TestSuiteClient RemoteObject<TestSuiteClient,TestSuiteServant>.Client {
			get { return this; }
		}

		TestSuiteServant RemoteObject<TestSuiteClient, TestSuiteServant>.Servant {
			get { throw new ServerErrorException (); }
		}

		internal static Task<TestSuiteClient> FromProxy (ObjectProxy proxy, CancellationToken cancellationToken)
		{
			return Task.FromResult ((TestSuiteClient)proxy);
		}
	}
}

