using System;

namespace Xamarin.WebTests.ConnectionFramework
{
	public class ConnectionException : Exception
	{
		public ConnectionException (string message, params object[] args)
			: base (string.Format (message, args))
		{
		}
	}
}

