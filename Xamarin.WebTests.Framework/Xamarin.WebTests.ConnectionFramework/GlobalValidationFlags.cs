using System;

namespace Xamarin.WebTests.ConnectionFramework
{
	[Flags]
	public enum GlobalValidationFlags
	{
		None = 0,
		SetToNull = 1,
		SetToTestRunner = 2,

		MustNotInvoke = 16,
		MustInvoke = 32,

		AlwaysFail = 64,
		AlwaysSucceed = 128,

		CheckChain = 256,
		ExpectError = 512
	}
}

