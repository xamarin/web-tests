using System;
using System.IO;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using System.Reflection;
using System.Xml;
using System.Xml.Serialization;
using NDesk.Options;

namespace Xamarin.AsyncTests.ConsoleRunner
{
	using Framework;

	class MainClass
	{
		static bool xml;

		public static void Main (string[] args)
		{
			Debug.AutoFlush = true;
			Debug.Listeners.Add (new ConsoleTraceListener ());

			var p = new OptionSet ().Add ("xml", v => xml = true);
			var remaining = p.Parse (args);

			Assembly asm;
			if (remaining.Count == 1)
				asm = LoadAssembly (remaining [0]);
			else if (remaining.Count == 0)
				asm = typeof(Sample.SimpleTest).Assembly;
			else
				throw new InvalidOperationException ();

			try {
				Run (asm).Wait ();
			} catch (Exception ex) {
				Console.WriteLine ("ERROR: {0}", ex);
			}
		}

		static Assembly LoadAssembly (string name)
		{
			return Assembly.LoadFile (name);
		}

		static async Task Run (Assembly assembly)
		{
			var suite = await TestSuite.LoadAssembly (assembly);

			var context = new TestContext ();
			context.DebugLevel = 0;
			var result = new TestResult (new TestName (assembly.GetName ().Name));
			await suite.Run (context, result, CancellationToken.None);
			WriteResults (result);
		}

		static void WriteResults (TestResult results)
		{
			if (xml) {
				var serializer = new XmlSerializer (typeof(TestResult));
				serializer.Serialize (Console.Out, results);
				Console.WriteLine ();
			} else {
				ResultPrinter.Print (Console.Out, results);
			}
		}
	}
}
