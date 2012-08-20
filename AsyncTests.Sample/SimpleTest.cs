using System;

namespace AsyncTests.Sample
{
	using Framework;

	[AsyncTestFixture]
	public class SimpleTest
	{
		[AsyncTest]
		public void First (TestContext context)
		{
		}

		[AsyncTest]
		public void Second (TestContext context)
		{
			throw new NotImplementedException ();
		}
	}
}
