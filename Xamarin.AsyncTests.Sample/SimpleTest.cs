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
				context.Log ("INITIALIZE: {0}", id);
			});
		}

		public Task PreRun (TestContext context, CancellationToken cancellationToken)
		{
			return Task.Run (() => {
				context.Log ("PRE RUN: {0}", id);
			});
		}

		public Task PostRun (TestContext context, CancellationToken cancellationToken)
		{
			return Task.Run (() => {
				context.Log ("POST RUN: {0}", id);
			});
		}

		public Task Destroy (TestContext context, CancellationToken cancellationToken)
		{
			return Task.Run (() => {
				context.Log ("DESTROY: {0}", id);
			});
		}

		[AsyncTest]
		public void First (TestContext context)
		{
			context.Log ("FIRST: {0}", id);
		}

		[AsyncTest (Repeat = 3)]
		public void Second (TestContext context)
		{
			context.Log ("SECOND: {0}", id);
			throw new NotSupportedException ();
		}

		[AsyncTest(Repeat = 10)]
		public void Hello (TestContext context)
		{
			context.Log ("HELLO: {0}", id);
		}
	}
}
