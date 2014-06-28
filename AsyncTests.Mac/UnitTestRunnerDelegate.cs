using System;
using MonoMac.Foundation;
using MonoMac.AppKit;

namespace Xamarin.AsyncTests.Mac
{
	using Framework;

	public class UnitTestRunnerDelegate : NSOutlineViewDelegate
	{
		enum ColumnTag {
			Name = 1,
			State,
			Count,
			Errors,
			Warnings
		}

		NSColor ColorForResult (TestResult result)
		{
			switch (result.Status) {
			case TestStatus.Success:
				return NSColor.Blue;
			case TestStatus.Error:
				return NSColor.Red;
			case TestStatus.Warning:
				return NSColor.Brown;
			default:
				return NSColor.Gray;
			}
		}
		
		string StateForResult (TestResult result)
		{
			switch (result.Status) {
			case TestStatus.Success:
				return "Pass";
			case TestStatus.Error:
				return "Fail";
			case TestStatus.Warning:
				return "Warning";
			default:
				return string.Empty;
			}
		}

		public override NSView GetView (NSOutlineView view, NSTableColumn col, NSObject item)
		{
			ResultWrapper wrapper = (ResultWrapper)item;
			var tag = (ColumnTag)col.DataCell.Tag;
			var identifier = (NSString)col.Identifier;

			var cell = (NSTableCellView)view.MakeView (identifier, view);

			var result = wrapper.Item as TestResult;
			if (result == null) {
				if (tag == ColumnTag.Name)
					cell.TextField.StringValue = (NSString)wrapper.Item.Name;
				else
					cell.TextField.StringValue = string.Empty;
				cell.TextField.TextColor = NSColor.Black;
				return cell;
			}

			switch (tag) {
			case ColumnTag.Name:
				cell.TextField.TextColor = ColorForResult (result);
				cell.TextField.StringValue = result.Name;
				break;

			case ColumnTag.State:
				cell.TextField.TextColor = ColorForResult (result);
				cell.TextField.StringValue = StateForResult (result);
				break;

			case ColumnTag.Count:
				cell.TextField.StringValue = result.TotalSuccess.ToString ();
				break;

			case ColumnTag.Errors:
				cell.TextField.StringValue = result.TotalErrors.ToString ();
				break;
				
			case ColumnTag.Warnings:
				cell.TextField.StringValue = result.TotalWarnings.ToString ();
				break;
			}

			return cell;
		}
	}
}

