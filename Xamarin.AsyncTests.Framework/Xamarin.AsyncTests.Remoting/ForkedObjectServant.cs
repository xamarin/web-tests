//
// ForkedObjectServant.cs
//
// Author:
//       Martin Baulig <mabaul@microsoft.com>
//
// Copyright (c) 2018 Xamarin Inc. (http://www.xamarin.com)
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

	class ForkedObjectServant : ObjectServant, RemoteForkedObject
	{
		public override string Type => "RemoteObject";

		ForkedObjectClient RemoteObject<ForkedObjectClient, ForkedObjectServant>.Client => throw new ServerErrorException ();

		ForkedObjectServant RemoteObject<ForkedObjectClient, ForkedObjectServant>.Servant => this;

		public TestContext ServantContext {
			get;
		}

		public TestInstance Instance {
			get;
		}

		public IForkedObject ForkedInstance {
			get;
		}

		public ForkedObjectServant (TestContext ctx, Connection connection, long objectId, TestInstance instance)
			: base (connection, objectId)
		{
			ServantContext = ctx;
			Instance = instance;

			if (Instance is HeavyTestInstance heavy)
				ForkedInstance = heavy.Instance as IForkedObject;
		}

		[StackTraceEntryPoint]
		internal Task<XElement> HandleMessage (string message, XElement body, CancellationToken cancellationToken)
		{
			return ForkedInstance.HandleMessage (ServantContext, message, body, cancellationToken);
		}

		internal Task<TestResult> RunRemoteCommand (XElement node, CancellationToken cancellationToken)
		{
			var forkedInstance = (ForkedTestInstance)Instance;
			var session = RemoteTestInstance.GetCurrentSession (Instance);
			return forkedInstance.RunRemoteCommand (session, node, cancellationToken);
		}
	}
}
