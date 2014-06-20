using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Collections.Generic;

namespace AsyncTests.ConsoleRunner {

	using Framework;

	public class ResultPrinter : ResultVisitor {
		TextWriter writer;
		Stack<string> names;
		int id;

		public ResultPrinter (TextWriter writer)
		{
			this.writer = writer;
			names = new Stack<string> ();
		}

		public static void Print (TextWriter writer, TestResultCollection result)
		{
			writer.WriteLine ();
			writer.WriteLine ("Total: {0} tests, {1} passed, {2} errors.",
			                  result.Count, result.TotalSuccess, result.TotalErrors);
			writer.WriteLine ();

			var printer = new ResultPrinter (writer);
			printer.Visit (result);
		}

		string GetName ()
		{
			return string.Join (".", names.ToArray ());
		}

		void PushName (TestResultItem item)
		{
			if (item.Name != null)
				names.Push (item.Name);
		}

		void PopName (TestResultItem item)
		{
			if (item.Name != null)
				names.Pop ();
		}

		#region implemented abstract members of ResultVisitor
		public override void Visit (TestResultCollection node)
		{
			for (int i = 0; i < node.Count; i++) {
				var item = node [i];
				PushName (item);
				item.Accept (this);
				PopName (item);
			}
		}

		public override void Visit (TestResultText node)
		{
			;
		}

		public override void Visit (TestSuccess node)
		{
			;
		}

		public override void Visit (TestError node)
		{
			writer.WriteLine ("{0}) {1}\n{2}\n", ++id, GetName (), node.Error);
		}

		public override void Visit (TestWarning node)
		{
			writer.WriteLine ("{0}) {1}\n{2}\n", ++id, GetName (), node);
		}

		public override void Visit (TestResultWithErrors node)
		{
			for (int i = 0; i < node.Count; i++) {
				var item = node [i];
				PushName (item);
				item.Accept (this);
				PopName (item);
			}
		}
		#endregion

	}
}

