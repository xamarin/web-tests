using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xamarin.AsyncTests;
using Xamarin.AsyncTests.Portable;

namespace Xamarin.WebTests.ConnectionFramework
{
	public class StreamWrapper : ILineBasedStream
	{
		public Stream InnerStream {
			get;
			private set;
		}

		StreamReader reader;
		StreamWriter writer;

		public StreamWrapper (Stream innerStream)
		{
			InnerStream = innerStream;

			var support = DependencyInjector.Get<IPortableSupport> ();
			var ascii = support.ASCIIEncoding;

			reader = new StreamReader (innerStream, ascii);
			writer = new StreamWriter (innerStream, ascii);
			writer.AutoFlush = true;
		}

		public Task<string> ReadLineAsync ()
		{
			return reader.ReadLineAsync ();
		}

		public Task WriteLineAsync (string line)
		{
			return writer.WriteLineAsync (line);
		}

		public Task WriteLineAsync ()
		{
			return writer.WriteLineAsync ();
		}
	}
}

