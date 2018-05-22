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

		public ForkedTestInvoker (ForkedTestHost host, TestNode node, TestInvoker inner)
			: base (host.Flags)
		{
			Host = host;
			Node = node;
			Inner = inner;
		}

		public override Task<bool> Invoke (TestContext ctx, TestInstance instance, CancellationToken cancellationToken)
		{
			Host.Attribute.SanityCheck (ctx);
			if (!ctx.IsEnabled (TestFeature.ForkedSupport))
				throw ctx.IgnoreThisTest ();

			var effectiveForkType = Host.GetEffectiveForkType (ctx);

			switch (effectiveForkType) {
			case ForkType.Task:
				return ForkTasks (ctx, instance, cancellationToken);
			case ForkType.Fork:
			case ForkType.Domain:
			case ForkType.Internal:
			case ForkType.ReverseFork:
			case ForkType.ReverseDomain:
				return RunExternalFork (ctx, instance, cancellationToken);
			case ForkType.None:
				return RunInner (ctx, instance, cancellationToken);
			default:
				throw ctx.AssertFail ($"Invalid fork type: {effectiveForkType}");
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
				var fork = forks[i] = new ForkedTestInstance (Host, Node, instance, ForkType.Task, i);

				tasks[i] = Task.Factory.StartNew (
					() => StartForked (fork, delay).Result,
					cancellationToken, TaskCreationOptions.LongRunning, TaskScheduler.Default);
			}

			var retval = await Task.WhenAll (tasks).ConfigureAwait (false);
			return retval.All (r => r);

			async Task<bool> StartForked (ForkedTestInstance forkedInstance, int delay)
			{
				if (delay == 0)
					await Task.Yield ();
				else
					await Task.Delay (delay).ConfigureAwait (false);
				return await Inner.Invoke (ctx, forkedInstance, cancellationToken);
			}
		}

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

		TestInstance CreateInstance (TestContext ctx, TestInstance parent)
		{
			return Host.CreateInstance (ctx, Node, parent);
		}

		TestInstance SetUp (TestContext ctx, TestInstance instance)
		{
			ctx.LogDebug (LogCategory, 10, "SetUp({0}): {1} {2}", ctx.FriendlyName, TestLogger.Print (Host), TestLogger.Print (instance));

			try {
				var innerInstance = CreateInstance (ctx, instance);
				innerInstance.Initialize (ctx);
				return innerInstance;
			} catch (OperationCanceledException) {
				ctx.OnTestCanceled ();
				return null;
			} catch (Exception ex) {
				ctx.OnError (ex);
				return null;
			}
		}

		Task<bool> RunInner (TestContext ctx, TestInstance instance, CancellationToken cancellationToken)
		{
			var forkedInstance = SetUp (ctx, instance);
			return InvokeInner (ctx, forkedInstance, Inner, cancellationToken);
		}

		void WalkStackAndRegister (TestContext ctx, Connection connection, TestInstance instance)
		{
			while (instance != null) {
				if (instance is HeavyTestInstance heavy)
					heavy.RegisterForkedServant (ctx, connection);
				if (instance is ForkedTestInstance forked)
					forked.RegisterForkedServant (ctx, connection);
				instance = instance.Parent;
			}
		}

		async Task<bool> RunExternalFork (TestContext ctx, TestInstance instance, CancellationToken cancellationToken)
		{
			if (Node.HasParameter) {
				ctx.LogDebug (LogCategory, 1, $"RUN FORKED: {Node.Parameter}");
				return await RunInner (ctx, instance, cancellationToken).ConfigureAwait (false);
			}

			var startTime = DateTime.Now;

			ctx.OnTestRunning ();

			try {
				var result = await ExternalFork (ctx, instance, cancellationToken).ConfigureAwait (false);
				ctx.OnTestFinished (result.Status, DateTime.Now - startTime);
				return true;
			} catch (OperationCanceledException) {
				ctx.OnTestCanceled ();
				return false;
			} catch (Exception ex) {
				ctx.OnError (ex, DateTime.Now - startTime);
				return false;
			}
		}

		async Task<TestResult> ExternalFork (TestContext ctx, TestInstance instance, CancellationToken cancellationToken)
		{
			var builder = GetSuiteBuilder (instance);

			TestServer server = null;
			TestSession session;

			var effectiveForkType = Host.GetEffectiveForkType (ctx);

			switch (effectiveForkType) {
			case ForkType.Internal:
				session = TestSession.CreateLocal (builder.App, builder.Framework);
				break;
			case ForkType.Domain:
			case ForkType.ReverseDomain:
				server = await TestServer.ForkAppDomain (
					builder.App, Host.Attribute.DomainName, cancellationToken).ConfigureAwait (false);
				session = server.Session;
				break;
			case ForkType.Fork:
			case ForkType.ReverseFork:
				server = await TestServer.ForkApplication (
					builder.App, cancellationToken).ConfigureAwait (false);
				session = server.Session;
				break;
			default:
				throw ctx.AssertFail (effectiveForkType);
			}

			try {
				await session.UpdateSettings (cancellationToken);

				var forkedInstance = new ForkedTestInstance (Host, Node, instance, effectiveForkType, 0);
				forkedInstance.Initialize (ctx);

				var path = forkedInstance.GetCurrentPath ();

				var servantCtx = ctx.CreateChild (path);
				if (server != null)
					WalkStackAndRegister (servantCtx, server.Connection, forkedInstance);

				TestInstance.LogDebug (ctx, instance, 5);

				var serialized = path.SerializePath (true);
				ctx.LogDebug (LogCategory, 1, $"RUN\n{serialized}\n");

				var test = await session.ResolveFromPath (serialized, cancellationToken);

				ctx.LogDebug (LogCategory, 1, $"RUN #1: {serialized}\n{test}");

				var result = await session.Run (test, cancellationToken).ConfigureAwait (false);

				ctx.LogDebug (LogCategory, 1, $"RUN DONE: {result}");

				ctx.Result.AddChild (result);

				await session.Shutdown (cancellationToken).ConfigureAwait (false);

				ctx.LogDebug (LogCategory, 1, $"RUN DONE #1");

				return result;
			} finally {
				if (server != null)
					await server.Stop (cancellationToken);
			}
		}
	}
}

