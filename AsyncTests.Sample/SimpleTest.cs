using System;
using System.Threading;
using System.Threading.Tasks;
using System.Diagnostics;

namespace AsyncTests.Sample
{
	using Framework;

	[AsyncTestFixture(Repeat = 3)]
	public class SimpleTest : IAsyncTestFixture
	{
		int id;
		static int next_id;

		public Task SetUp (TestContext context, CancellationToken cancellationToken)
		{
			return Task.Run (() => {
				id = ++next_id;
				Debug.WriteLine ("SETUP: {0}", id);
			});
		}

		public Task TearDown (TestContext context, CancellationToken cancellationToken)
		{
			return Task.Run (() => {
				Debug.WriteLine ("TEAR DOWN: {0}", id);
			});
		}

		[AsyncTest]
		public void First (TestContext context)
		{
			Debug.WriteLine ("FIRST: {0}", id);
		}

		[AsyncTest]
		public void Second (TestContext context)
		{
			Debug.WriteLine ("SECOND: {0}", id);
			throw new NotSupportedException ();
		}

		[AsyncTest(Repeat = 10)]
		public void Hello (TestContext context)
		{
			Debug.WriteLine ("HELLO: {0}", id);
		}
	}
}
