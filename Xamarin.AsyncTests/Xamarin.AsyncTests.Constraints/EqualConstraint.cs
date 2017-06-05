//
// EqualityConstraint.cs
//
// Author:
//       Martin Baulig <martin.baulig@xamarin.com>
//
// Copyright (c) 2014 Xamarin Inc. (http://www.xamarin.com)
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
using System.Collections;
using System.Collections.Generic;

namespace Xamarin.AsyncTests.Constraints
{
	public class EqualConstraint : Constraint
	{
		public object Expected {
			get;
		}

		public string ExpectedName {
			get;
		}

		public EqualConstraint (object expected)
		{
			Expected = expected;
			ExpectedName = Format (expected);
		}

		static string Format (object value)
		{
			if (value == null)
				return "<null>";
			if (value is Enum enumValue)
				return string.Format ("{0}.{1}", enumValue.GetType ().Name, value);
			return value.ToString ();
		}

		public override bool Evaluate (object actual, out string message)
		{
			if (Expected is string expectedString) {
				if (actual == null) {
					message = "Expected string, but got <null>.";
					return false;
				}
				var actualString = actual as string;
				if (actualString != null)
					return CompareString (expectedString, actualString, out message);
				if (actual is Guid)
					return CompareGuid (Expected, (Guid)actual, out message);

				message = string.Format (
					"Expected string, but got instance of Type `{0}'.", actual.GetType ());
				return false;
			}

			if (Expected is IList expectedList) {
				if (actual == null) {
					message = "Expected list, but got <null>.";
					return false;
				}

				var actualList = actual as IList;
				if (actualList == null) {
					message = string.Format (
						"Expected list, but got instance of Type `{0}'.", actual.GetType ());
					return false;
				}

				return CompareList (expectedList, actualList, out message);
			}

			if (Expected is Enum expectedEnum) {
				var expectedType = expectedEnum.GetType ();
				if (actual.GetType () != expectedType) {
					message = string.Format (
						"Expected enum of type `{0}', but got `{1}'.", expectedType, actual.GetType ());
					return false;
				}

				if (expectedEnum.Equals (actual)) {
					message = null;
					return true;
				}
			}

			if (Expected is byte[] expectedBuffer) {
				var actualBuffer = actual as byte[];
				if (actualBuffer == null) {
					if (actual == null)
						message = string.Format ("Expected byte array of length {0}, but got <null>.", expectedBuffer.Length);
					else
						message = string.Format ("Expected byte array of length {0}, but got instance of Type `{0}'.", actual.GetType ());
					return false;
				}

				return CompareBuffer (expectedBuffer, actualBuffer, out message);
			}

			message = null;
			if (object.Equals (actual, Expected))
				return true;

			message = string.Format ("Expected '{0}', got '{1}'.", ExpectedName, Format (actual));
			return false;
		}

		bool CompareGuid (object expected, Guid actual, out string message)
		{
			var expectedString = expected as string;
			if (expectedString != null)
				return CompareString (expectedString, actual.ToString (), out message);

			var expectedGuid = (Guid)expected;
			return CompareString (expectedGuid.ToString (), actual.ToString (), out message);
		}

		bool CompareString (string expected, string actual, out string message)
		{
			if (actual.Length != expected.Length) {
				message = string.Format (
					"Strings differ in length: expected {0}, got {1}.", expected.Length, actual.Length);
				return false;
			}

			message = null;
			return string.Compare (expected, actual) == 0;
		}

		bool CompareElement (object expected, object actual)
		{
			return expected.Equals (actual);
		}

		bool CompareList (IList expected, IList actual, out string message)
		{
			if (expected.Count != actual.Count) {
				message = string.Format (
					"Collections differ in size: expected {0}, got {1}.", expected.Count, actual.Count);
				return false;
			}

			for (int i = 0; i < expected.Count; i++) {
				var ok = CompareElement (expected [i], actual [i]);
				if (!ok) {
					message = string.Format (
						"Collections differ at element {0}: expected {1}, got {2}.", i, expected [i], actual [i]);
					return false;
				}
			}

			message = null;
			return true;
		}

		bool CompareBuffer (byte[] expected, byte[] actual, out string message)
		{
			if (expected.Length != actual.Length) {
				message = string.Format (
					"Buffers differ in size: expected {0}, got {1}.", expected.Length, actual.Length);
				return false;
			}

			for (int i = 0; i < expected.Length; i++) {
				var ok = expected [i] == actual [i];
				if (!ok) {
					message = string.Format (
						"Buffers differ at element {0}: expected {1}, got {2}.", i, expected [i], actual [i]);
					return false;
				}
			}

			message = null;
			return true;
		}

		public override string Print ()
		{
			return string.Format ("Equal({0})", ExpectedName);
		}
	}
}

