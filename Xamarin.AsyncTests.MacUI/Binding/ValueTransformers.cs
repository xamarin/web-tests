//
// ValueTransformers.cs
//
// Author:
//       Martin Baulig <martin.baulig@xamarin.com>
//
// Copyright (c) 2015 Xamarin, Inc.
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
using System;
using AppKit;
using Foundation;
using Xamarin.AsyncTests;

namespace Xamarin.AsyncTests.MacUI
{
	static class ValueTransformers
	{
		[Register ("TestStatusNameValueTransformer")]
		public class TestStatusNameValueTransformer : NSValueTransformer
		{
			public override NSObject TransformedValue (NSObject value)
			{
				if (value == null)
					return null;
				var number = (NSNumber)value;
				var status = (TestStatus)number.Int32Value;
				switch ((TestStatus)number.Int32Value) {
				case TestStatus.None:
					return (NSString)"None";
				case TestStatus.Ignored:
					return (NSString)"Ignored";
				case TestStatus.Success:
					return (NSString)"Success";
				case TestStatus.Error:
					return (NSString)"Error";
				case TestStatus.Canceled:
					return (NSString)"Canceled";
				default:
					return (NSString)"Unknown";
				}
			}

			public override NSObject ReverseTransformedValue (NSObject value)
			{
				return base.ReverseTransformedValue (value);
			}
		}

		[Register ("TestStatusColorValueTransformer")]
		public class TestStatusColorValueTransformer : NSValueTransformer
		{
			public override NSObject TransformedValue (NSObject value)
			{
				if (value == null)
					return null;
				var number = (NSNumber)value;
				var status = (TestStatus)number.Int32Value;
				switch ((TestStatus)number.Int32Value) {
				case TestStatus.None:
					return NSColor.Black;
				case TestStatus.Ignored:
					return NSColor.Gray;
				case TestStatus.Success:
					return NSColor.Blue;
				case TestStatus.Error:
					return NSColor.Red;
				case TestStatus.Canceled:
					return NSColor.Brown;
				default:
					return NSColor.Blue;
				}
			}
		}

		[Register ("TestCategoryNameValueTransformer")]
		public class TestCategoryNameValueTransformer : NSValueTransformer
		{
			public override NSObject TransformedValue (NSObject value)
			{
				var category = value as TestCategoryModel;
				Console.WriteLine ("TRANSFORM: {0} {1}", value, value.GetType ());
				if (category == null)
					return (NSString)"FUCK";
				return (NSString)category.Category.Name;
			}
		}

		[Register ("ServerModeIsAndroidValueTransformer")]
		public class ServerModeIsAndroidValueTransformer : NSValueTransformer
		{
			public override NSObject TransformedValue (NSObject value)
			{
				var mode = value as ServerModeModel;
				var ok = mode.Mode == ServerMode.Android;
				return NSNumber.FromBoolean (ok);
			}

		}

		[Register ("ServerModeIsIOSValueTransformer")]
		public class ServerModeIsIOSValueTransformer : NSValueTransformer
		{
			public override NSObject TransformedValue (NSObject value)
			{
				var mode = value as ServerModeModel;
				var ok = mode.Mode == ServerMode.iOS;
				return NSNumber.FromBoolean (ok);
			}

		}

		[Register ("ServerModeIsLocalValueTransformer")]
		public class ServerModeIsLocalValueTransformer : NSValueTransformer
		{
			public override NSObject TransformedValue (NSObject value)
			{
				var mode = value as ServerModeModel;
				var ok = mode.Mode == ServerMode.Local;
				return NSNumber.FromBoolean (ok);
			}

		}

		[Register ("ServerModeHasListenAddressValueTransformer")]
		public class ServerModeHasListenAddressValueTransformer : NSValueTransformer
		{
			public override NSObject TransformedValue (NSObject value)
			{
				var mode = value as ServerModeModel;
				var ok = mode.Mode == ServerMode.WaitForConnection || mode.Mode == ServerMode.Local;
				return NSNumber.FromBoolean (ok);
			}
		}
	}
}

