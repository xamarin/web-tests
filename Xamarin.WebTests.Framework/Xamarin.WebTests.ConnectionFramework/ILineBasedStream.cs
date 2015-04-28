using System;
using System.Threading;
using System.Threading.Tasks;

namespace Xamarin.WebTests.ConnectionFramework
{
	public interface ILineBasedStream
	{
		Task<string> ReadLineAsync ();

		Task WriteLineAsync (string line);
	}
}

