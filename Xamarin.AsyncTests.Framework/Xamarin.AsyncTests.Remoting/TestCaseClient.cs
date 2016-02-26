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

namespace Xamarin.AsyncTests.Remoting
{
	using Framework;

	class TestCaseClient : TestCase, ObjectClient<TestCaseClient>, RemoteTestCase
	{
		public TestSessionClient Session {
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

		public string Type {
			get { return "TestCase"; }
		}

		public bool HasParameters {
			get;
			private set;
		}

		public bool HasChildren {
			get;
			private set;
		}

		public TestCaseClient (TestSessionClient session, long objectID)
		{
			Session = session;
			Connection = session.Connection;
			ObjectID = objectID;
		}

		List<TestCaseClient> children;
		List<TestCaseClient> parameters;

		public async Task<IReadOnlyCollection<TestCase>> GetTestParameters (CancellationToken cancellationToken)
		{
			if (parameters != null)
				return parameters;

			var command = new GetParametersCommand ();
			var response = await command.Send (this, null, cancellationToken).ConfigureAwait (false);
			parameters = await ReadTestCaseList (response, cancellationToken);

			return parameters;
		}

		Task<IReadOnlyCollection<TestCase>> TestCase.GetChildren (CancellationToken cancellationToken)
		{
			return GetTestChildren (cancellationToken);
		}

		public async Task<IReadOnlyCollection<TestCase>> GetTestChildren (CancellationToken cancellationToken)
		{
			if (children != null)
				return children;

			var command = new GetChildrenCommand ();
			var response = await command.Send (this, null, cancellationToken).ConfigureAwait (false);
			children = await ReadTestCaseList (response, cancellationToken);

			return children;
		}

		void Deserialize (XElement node)
		{
			if (!node.Name.LocalName.Equals ("TestCase"))
				throw new ServerErrorException ();

			Path = new PathWrapper (node.Element ("TestPath"));
			Name = TestSerializer.ReadTestName (node.Element ("TestName"));
			HasParameters = bool.Parse (node.Attribute ("HasParameters").Value);
			HasChildren = bool.Parse (node.Attribute ("HasChildren").Value);
		}

		class PathWrapper : ITestPath
		{
			readonly XElement node;

			public PathWrapper (XElement node)
			{
				this.node = node;
				if (node == null)
					throw new InvalidOperationException();
			}

			public XElement SerializePath ()
			{
				return node;
			}
		}

		public async Task<TestResult> Run (CancellationToken cancellationToken)
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

		static XElement WriteTestCaseList (IEnumerable<TestCaseServant> list)
		{
			var root = new XElement ("TestCaseList");
			foreach (var child in list)
				root.Add (RemoteObjectManager.WriteProxy (child));
			return root;
		}

		async Task<List<TestCaseClient>> ReadTestCaseList (XElement node, CancellationToken cancellationToken)
		{
			if (!node.Name.LocalName.Equals ("TestCaseList"))
				throw new ServerErrorException ();

			var list = new List<TestCaseClient> ();

			foreach (var element in node.Elements ()) {
				var proxy = RemoteObjectManager.ReadTestCase (Session, element);
				var test = await FromProxy (proxy, cancellationToken);
				list.Add (test);
			}

			return list;
		}

		class GetParametersCommand : RemoteObjectCommand<RemoteTestCase,object,XElement>
		{
			protected override async Task<XElement> Run (
				Connection connection, RemoteTestCase proxy, object argument, CancellationToken cancellationToken)
			{
				var list = await proxy.Servant.GetParameters (cancellationToken).ConfigureAwait (false);
				return WriteTestCaseList (list);
			}
		}

		class GetChildrenCommand : RemoteObjectCommand<RemoteTestCase,object,XElement>
		{
			protected override async Task<XElement> Run (
				Connection connection, RemoteTestCase proxy, object argument, CancellationToken cancellationToken)
			{
				var list = await proxy.Servant.GetChildren (cancellationToken).ConfigureAwait (false);
				return WriteTestCaseList (list);
			}
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

			test.Deserialize (node);
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

