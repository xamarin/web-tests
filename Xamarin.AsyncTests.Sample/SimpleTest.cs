using System;
using System.Threading;
using System.Threading.Tasks;

namespace Xamarin.AsyncTests.Sample
{
	using Framework;

	[AsyncTestFixture]
	public class SimpleTest : IAsyncTestFixture
	{
		int id;
		static int next_id;

		public Task SetUp (TestContext context, CancellationToken cancellationToken)
		{
			return Task.Run (() => {
				id = ++next_id;
				context.Log ("SETUP: {0}", id);
			});
		}

		public Task TearDown (TestContext context, CancellationToken cancellationToken)
		{
			return Task.Run (() => {
				context.Log ("TEAR DOWN: {0}", id);
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
