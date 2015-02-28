//
// TestCaseClient.cs
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

namespace Xamarin.AsyncTests.Server
{
	class TestCaseClient : TestCase, ObjectClient<TestCaseClient>, RemoteTestCase
	{
		TestSuite TestCase.Suite {
			get { return Suite; }
		}

		public TestSuiteClient Suite {
			get;
			private set;
		}

		public TestName Name {
			get;
			private set;
		}

		public ITestPath Path {
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

		public TestCaseClient (TestSuiteClient suite, long objectID)
		{
			Suite = suite;
			Connection = suite.Connection;
			ObjectID = objectID;
		}

		List<TestCaseClient> children;

		public async Task<IReadOnlyCollection<TestCase>> GetChildren (TestContext ctx, CancellationToken cancellationToken)
		{
			if (children != null)
				return children;

			children = await GetChildren (cancellationToken);
			return children;
		}

		void Deserialize (XElement node)
		{
			if (!node.Name.LocalName.Equals ("TestCase"))
				throw new ServerErrorException ();

			Path = new Serializer.PathWrapper (node.Element ("Path"));
			Name = Serializer.TestName.Read (node.Element ("TestName"));
		}

		public async Task<TestResult> Run (TestContext ctx, CancellationToken cancellationToken)
		{
			var runCommand = new RunCommand ();
			var result = await runCommand.Send (this, null, cancellationToken);
			Connection.Debug ("RUN: {0}", result);
			return result;
		}

		TestCaseClient RemoteObject<TestCaseClient,TestCaseServant>.Client {
			get { return this; }
		}

		TestCaseServant RemoteObject<TestCaseClient, TestCaseServant>.Servant {
			get { throw new ServerErrorException (); }
		}

		class GetChildrenCommand : RemoteObjectCommand<RemoteTestCase,object,XElement>
		{
			protected override async Task<XElement> Run (
				Connection connection, RemoteTestCase proxy, object argument, CancellationToken cancellationToken)
			{
				var root = new XElement ("TestCaseList");
				foreach (var child in await proxy.Servant.GetChildren (cancellationToken))
					root.Add (RemoteObjectManager.WriteProxy (child));
				return root;
			}
		}

		async Task<List<TestCaseClient>> GetChildren (CancellationToken cancellationToken)
		{
			var command = new GetChildrenCommand ();
			var response = await command.Send (this, null, cancellationToken);

			Connection.Debug ("GOT RESPONSE: {0}", response);

			if (!response.Name.LocalName.Equals ("TestCaseList"))
				throw new ServerErrorException ();

			var children = new List<TestCaseClient> ();

			foreach (var element in response.Elements ()) {
				var proxy = RemoteObjectManager.ReadTestCase (Suite, element);
				var test = await FromProxy (proxy, cancellationToken);
				children.Add (test);
			}

			return children;
		}

		class RunCommand : RemoteObjectCommand<RemoteTestCase,object,TestResult>
		{
			protected override async Task<TestResult> Run (Connection connection, RemoteTestCase proxy, object argument, CancellationToken cancellationToken)
			{
				return await proxy.Servant.Run (cancellationToken);
			}
		}

		internal static async Task<TestCaseClient> FromProxy (ObjectProxy proxy, CancellationToken cancellationToken)
		{
			var test = (TestCaseClient)proxy;
			if (test.Path != null)
				return test;

			var command = new InitializeCommand ();
			var node = await command.Send (test, null, cancellationToken);
			if (!node.Name.LocalName.Equals ("TestCase"))
				throw new ServerErrorException ();

			test.Path = new Serializer.PathWrapper (node.Element ("TestPath"));
			test.Name = Serializer.TestName.Read (node.Element ("TestName"));
			return test;
		}

		class InitializeCommand : RemoteObjectCommand<RemoteTestCase,object,XElement>
		{
			protected override Task<XElement> Run (
				Connection connection, RemoteTestCase proxy, object argument, CancellationToken cancellationToken)
			{
				return Task.Run (() => {
					return proxy.Servant.SerializeServant ();
				});
			}
		}
	}
}

