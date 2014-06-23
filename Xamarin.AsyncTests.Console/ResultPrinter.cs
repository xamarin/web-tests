using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Collections.Generic;

namespace Xamarin.AsyncTests.ConsoleRunner {

	using Framework;

	public class ResultPrinter {
		TextWriter writer;
		Stack<string> names;
		int totalErrors;
		int totalSuccess;
		int id;

		public ResultPrinter (TextWriter writer)
		{
			this.writer = writer;
			names = new Stack<string> ();
		}

		public static void Print (TextWriter writer, TestResultCollection result)
		{
			writer.WriteLine ();

			var printer = new ResultPrinter (writer);
			printer.Visit (result);

			writer.WriteLine ();
			writer.WriteLine ("Total: {0} tests, {1} passed, {2} errors.",
				result.Children.Count, printer.totalSuccess, printer.totalErrors);
			writer.WriteLine ();
		}

		string GetName ()
		{
			return string.Join (".", names.Reverse ().ToArray ());
		}

		void PushName (TestResult item)
		{
			if (item.Name != null)
				names.Push (item.Name.Name);
		}

		void PopName (TestResult item)
		{
			if (item.Name != null)
				names.Pop ();
		}

		public void Visit (TestResult node)
		{
			var collection = node as TestResultCollection;
			if (collection != null)
				VisitCollection (collection);
			else
				VisitSimple (node);
		}
	
		void VisitCollection (TestResultCollection node)
		{
			foreach (var item in node.Children) {
				PushName (item);
				Visit (item);
				PopName (item);
			}
		}

		void VisitSimple (TestResult node)
		{
			switch (node.Status) {
			case TestStatus.Success:
				totalSuccess++;
				break;
			case TestStatus.Error:
				totalErrors++;
				writer.WriteLine ("{0}) {1}: {2}\n{3}\n", ++id, GetName (), node.Message, node.Error);
				break;
			case TestStatus.Warning:
				writer.WriteLine ("{0}) {1}:\n{2}\n", ++id, GetName (), node);
				break;
			case TestStatus.Ignored:
				writer.WriteLine ("{0}) {1} - ignored\n", ++id, GetName ());
				break;
			}
		}
	}
}

