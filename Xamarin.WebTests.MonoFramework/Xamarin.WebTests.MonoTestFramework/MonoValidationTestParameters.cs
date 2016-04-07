//
// MonoValidationTestParameters.cs
//
// Author:
//       Martin Baulig <martin.baulig@xamarin.com>
//
// Copyright (c) 2016 Xamarin Inc. (http://www.xamarin.com)

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
namespace Xamarin.WebTests.MonoTestFramework
{
	using MonoTestFeatures;

	[MonoValidationTestParameters]
	public class MonoValidationTestParameters : ValidationTestParameters
	{
		public MonoValidationTestType Type {
			get;
			private set;
		}

		public MonoValidationTestParameters (ValidationTestCategory category, MonoValidationTestType type, string identifier)
			: base (category, identifier)
		{
			Type = type;
		}

		protected MonoValidationTestParameters (MonoValidationTestParameters other)
			: base (other)
		{
			Type = other.Type;
			UseProvider = other.UseProvider;
			expectSuccess = other.expectSuccess;
			expectError = other.expectError;
		}

		bool? expectSuccess;
		int? expectError;

		public bool UseProvider {
			get; set;
		}

		public bool ExpectSuccess {
			get {
				if (expectSuccess == null)
					throw new InvalidOperationException ();
				return expectSuccess.Value;
			}
			set {
				expectSuccess = value;
			}
		}

		public int? ExpectError {
			get {
				return expectError;
			}
			set {
				if (expectSuccess != null && expectSuccess.Value)
					throw new InvalidOperationException ();
				expectError = value;
			}
		}

		public override ValidationTestParameters DeepClone ()
		{
			return new MonoValidationTestParameters (this);
		}
	}
}

