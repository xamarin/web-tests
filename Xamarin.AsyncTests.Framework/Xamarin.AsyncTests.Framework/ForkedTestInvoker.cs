//
// ForkedTestInvoker.cs
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
using System.IO;
using System.Linq;
using System.Text;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Xamarin.AsyncTests.Framework
{
	using Remoting;
	using Portable;

	class ForkedTestInvoker : AggregatedTestInvoker
	{
		public ForkedTestHost Host {
			get;
		}

		public TestNode Node {
			get;
		}

		public TestInvoker Inner {
			get;
		}

		public ForkType ForkType => Host.Attribute.Type;

		public ForkedTestInvoker (ForkedTestHost host, TestNode node, TestInvoker inner)
			: base (host.Flags)
		{
			Host = host;
			Node = node;
			Inner = inner;
		}

		public override Task<bool> Invoke (TestContext ctx, TestInstance instance, CancellationToken cancellationToken)
		{
			if (!ctx.IsEnabled (TestFeature.ForkedSupport))
				throw ctx.IgnoreThisTest ();

			switch (ForkType) {
			case ForkType.Task:
				return ForkTasks (ctx, instance, cancellationToken);
			case ForkType.Fork:
			case ForkType.Domain:
			case ForkType.Internal:
				return ExternalFork (ctx, instance, cancellationToken);
			default:
				throw ctx.AssertFail ($"Invalid fork type: {ForkType}");
			}
		}

		async Task<bool> ForkTasks (TestContext ctx, TestInstance instance, CancellationToken cancellationToken)
		{
			var forks = new ForkedTestInstance[Host.Attribute.Count];
			var tasks = new Task<bool>[forks.Length];

			var random = new Random ();

			for (int i = 0; i < forks.Length; i++) {
				int delay = 0;
				if (Host.Attribute.RandomDelay > 0)
					delay = random.Next (Host.Attribute.RandomDelay);
				var fork = forks[i] = new ForkedTestInstance (Host, Node, instance, i, delay, Inner);

				tasks[i] = Task.Factory.StartNew (
					() => fork.Start (ctx, cancellationToken).Result,
					cancellationToken, TaskCreationOptions.LongRunning, TaskScheduler.Default);
			}

			var retval = await Task.WhenAll (tasks).ConfigureAwait (false);
			return retval.All (r => r);
		}

		const string Category = "forked-invoker";

		TestSuiteBuilder GetSuiteBuilder (TestInstance instance)
		{
			while (instance != null) {
				if (instance.Host.PathType == TestPathType.Suite) {
					var host = (TestBuilderHost)instance.Host;
					return (TestSuiteBuilder)host.Builder;
				}

				instance = instance.Parent;
			}

			throw new InternalErrorException ();
		}

		void CheckInstanceStack (TestContext ctx, TestInstance instance)
		{
			ctx.LogDebug (Category, 5, $"CHECK INSTANCE STACK");
			TestInstance.LogDebug (ctx, instance, 5, Category);
			for (; instance != null; instance = instance.Parent) {
				var me = $"  STACK({instance.ID})";
				ctx.LogDebug (Category, 5, $"{me}");
				if (instance is TestBuilderInstance) {
					ctx.LogDebug (Category, 5, $"{me}: BUILDER");
					continue;
				}
				if (instance is ParameterizedTestInstance) {
					ctx.LogDebug (Category, 5, $"{me}: PARAMETER");
					continue;
				}
				if (instance is HeavyTestInstance heavy &&
				    heavy.Instance is IForkedTestInstance forked) {
					ctx.LogDebug (Category, 5, $"{me}: FORKED");
					continue;
				}
				ctx.LogDebug (Category, 5, $"{me}: UNKNOWN {instance.GetType ()}");
				throw new InvalidTimeZoneException ($"{me}: UNKNOWN {instance.GetType ()}");
			}
			ctx.LogDebug (Category, 5, $"CHECK INSTANCE STACK DONE");
		}

		IPortableEndPoint GetEndPoint ()
		{
			var support = DependencyInjector.Get<IPortableEndPointSupport> ();
			var port = TestContext.GetUniquePort ();
			return support.GetLoopbackEndpoint (port);
		}

		Task<bool> RunInner (TestContext ctx, TestInstance instance, CancellationToken cancellationToken)
		{
			var id = long.Parse (Node.Parameter.Value);
			var forkedInstance = new ForkedTestInstance (Host, Node, instance, id, 0, Inner);
			return InvokeInner (ctx, forkedInstance, Inner, cancellationToken);
		}

		async Task<bool> ExternalFork (TestContext ctx, TestInstance instance, CancellationToken cancellationToken)
		{
			if (Node.HasParameter) {
				ctx.LogDebug (Category, 1, $"RUN FORKED: {Node.Parameter}");
				return await RunInner (ctx, instance, cancellationToken).ConfigureAwait (false);
			}

			CheckInstanceStack (ctx, instance);

			var builder = GetSuiteBuilder (instance);

			TestServer server = null;
			TestSession session;

			switch (ForkType) {
			case ForkType.Internal:
				session = TestSession.CreateLocal (builder.App, builder.Framework);
				break;
			case ForkType.Domain:
				server = await TestServer.ForkAppDomain (
					builder.App, GetEndPoint (), null,
					cancellationToken).ConfigureAwait (false);
				session = server.Session;
				break;
			case ForkType.Fork:
				server = await TestServer.ForkApplication (
					builder.App, GetEndPoint (), null,
					cancellationToken).ConfigureAwait (false);
				session = server.Session;
				break;
			default:
				throw ctx.AssertFail (ForkType);
			}

			try {
				await session.UpdateSettings (cancellationToken);

				var forkedInstance = new ForkedTestInstance (Host, Node, instance, 0, -1, Inner);
				var path = forkedInstance.GetCurrentPath ();

				var serialized = path.SerializePath (true);
				var test = await session.ResolveFromPath (serialized, cancellationToken);

				ctx.LogDebug (Category, 1, $"RUN: {serialized}\n{test}");

				var result = await session.Run (test, cancellationToken).ConfigureAwait (false);

				ctx.LogDebug (Category, 1, $"RUN DONE: {result}");

				ctx.Result.AddChild (result);
			} finally {
				if (server != null)
					await server.Stop (cancellationToken);
			}

			ctx.LogDebug (Category, 1, $"RUN DONE #3");

			return true;
		}
	}
}

