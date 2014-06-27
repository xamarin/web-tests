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
			p.Parse (args);

			var asm = typeof(Xamarin.AsyncTests.Sample.SimpleTest).Assembly;

			try {
				Run (asm).Wait ();
			} catch (Exception ex) {
				Console.WriteLine ("ERROR: {0}", ex);
			}
		}

		static async Task Run (Assembly assembly)
		{
			var suite = await TestSuite.LoadAssembly (assembly);

			var context = new TestContext ();
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
