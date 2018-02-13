using System;
using P = AutoProvisionTool.Program;

namespace Xamarin.AsyncTests.Console
{
	static class Program
	{
		public static void Debug (string message)
		{
			P.Log (message);
		}

		public static void Debug (string format, params object[] args)
		{
			P.Log (string.Format (format, args));
		}
	}
}
