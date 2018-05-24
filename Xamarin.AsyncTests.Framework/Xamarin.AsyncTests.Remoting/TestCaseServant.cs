//
// TestCaseServant.cs
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

	class TestCaseServant : ObjectServant, RemoteTestCase
	{
		public TestSessionServant Session {
			get;
			private set;
		}

		public TestCase Test {
			get;
			private set;
		}

		public override string Type {
			get { return "TestCase"; }
		}

		public TestCaseServant (Connection connection, TestSessionServant session, TestCase test)
			: base (connection)
		{
			Session = session;
			Test = test;
		}

		internal XElement SerializeServant ()
		{
			var node = new XElement ("TestCase");
			node.SetAttributeValue ("HasParameters", Test.HasParameters);
			node.SetAttributeValue ("HasChildren", Test.HasChildren);
			node.Add (Test.Path.SerializePath ());
			return node;
		}

		List<TestCaseServant> parameters;
		List<TestCaseServant> children;

		public async Task<IEnumerable<TestCaseServant>> GetParameters (CancellationToken cancellationToken)
		{
			if (parameters != null)
				return parameters;

			parameters = new List<TestCaseServant> ();
			foreach (var parameter in await Session.LocalSession.GetTestParameters (Test, cancellationToken)) {
				var parameterServant = new TestCaseServant (Connection, Session, parameter);
				parameters.Add (parameterServant);
			}

			return parameters;
		}

		public async Task<IEnumerable<TestCaseServant>> GetChildren (CancellationToken cancellationToken)
		{
			if (children != null)
				return children;

			children = new List<TestCaseServant> ();
			foreach (var child in await Test.GetChildren (cancellationToken)) {
				var childServant = new TestCaseServant ((ServerConnection)Connection, Session, child);
				children.Add (childServant);
			}

			return children;
		}

		TestCaseClient RemoteObject<TestCaseClient,TestCaseServant>.Client {
			get { throw new ServerErrorException (); }
		}

		TestCaseServant RemoteObject<TestCaseClient,TestCaseServant>.Servant {
			get { return this; }
		}

		public Task<TestResult> Run (CancellationToken cancellationToken)
		{
			var tcs = new TaskCompletionSource<TestResult> ();

			Task.Factory.StartNew (() => {
				Session.LocalSession.Run (Test, cancellationToken).ContinueWith (task => {
					if (task.IsFaulted)
						tcs.SetException (task.Exception);
					else if (task.IsCanceled)
						tcs.SetCanceled ();
					else
						tcs.SetResult (task.Result);
				});
			}, cancellationToken, TaskCreationOptions.None, TaskScheduler.Default);

			return tcs.Task;
		}
	}
}

