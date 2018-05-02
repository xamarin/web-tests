//
// ForkedTestInstance.cs
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

namespace Xamarin.AsyncTests.Framework
{
	using Remoting;

	class ForkedTestInstance : RemoteTestInstance, ITestParameter, IFork
	{
		public ForkType Type {
			get;
		}

		new public long ID {
			get;
		}

		new public ForkedTestHost Host {
			get { return (ForkedTestHost)base.Host; }
		}

		string ITestParameter.Value => ID.ToString ();

		string ITestParameter.FriendlyValue => ID.ToString ();

		public ForkedTestInstance (ForkedTestHost host, TestNode node, TestInstance parent, ForkType type, long id)
			: base (host, node, parent)
		{
			Type = type;
			ID = id;

			CurrentParameter = new RemoteTestValue (this, id.ToString (), this);
		}

		internal sealed override RemoteTestValue CurrentParameter {
			get;
		}

		internal async Task<TestResult> HandleReverseFork (TestContext ctx, TestInstance instance, CancellationToken cancellationToken)
		{
			var me = $"ForkedTestInstance ({this})";

			WalkStackAndSerialize (ctx, instance);

			var path = instance.GetCurrentPath ();
			var serialized = path.SerializePath (true);

			return await ObjectClient.RunRemoteTest (path, cancellationToken).ConfigureAwait (false);
		}

		internal async Task<TestResult> RunRemoteCommand (TestSession session, XElement node, CancellationToken cancellationToken)
		{
			var test = await session.ResolveFromPath (node, cancellationToken).ConfigureAwait (false);

			return await session.Run (test, cancellationToken).ConfigureAwait (false);
		}
	}
}

