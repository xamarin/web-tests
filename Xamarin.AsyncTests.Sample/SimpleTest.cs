using System;
using System.Threading;
using System.Threading.Tasks;

namespace Xamarin.AsyncTests.Sample
{
	[AsyncTestFixture]
	public class SimpleTest : ITestInstance
	{
		int id;
		static int next_id;

		public Task Initialize (TestContext context, CancellationToken cancellationToken)
		{
			return Task.Run (() => {
				id = ++next_id;
				context.LogMessage ("INITIALIZE: {0}", id);
			});
		}

		public Task PreRun (TestContext context, CancellationToken cancellationToken)
		{
			return Task.Run (() => {
				context.LogMessage ("PRE RUN: {0}", id);
			});
		}

		public Task PostRun (TestContext context, CancellationToken cancellationToken)
		{
			return Task.Run (() => {
				context.LogMessage ("POST RUN: {0}", id);
			});
		}

		public Task Destroy (TestContext context, CancellationToken cancellationToken)
		{
			return Task.Run (() => {
				context.LogMessage ("DESTROY: {0}", id);
			});
		}

		[AsyncTest]
		public void First (TestContext context)
		{
			context.LogMessage ("FIRST: {0}", id);
		}

		[NotWorking]
		[AsyncTest (Repeat = 3)]
		public void Second (TestContext context)
		{
			context.LogMessage ("SECOND: {0}", id);
			throw new NotSupportedException ();
		}

		[AsyncTest(Repeat = 10)]
		public void Hello (TestContext context)
		{
			context.LogMessage ("HELLO: {0}", id);
		}
	}
}
