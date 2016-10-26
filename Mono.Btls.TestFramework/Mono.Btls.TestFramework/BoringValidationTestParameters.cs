//
// BoringValidationTestParameters.cs
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
using Xamarin.WebTests.MonoTestFramework;
using Mono.Btls.Interface;
using Xamarin.WebTests.ConnectionFramework;

namespace Mono.Btls.TestFramework
{
	[BoringValidationTestParameters]
	public class BoringValidationTestParameters : ValidationTestParameters
	{
		public BoringValidationTestType Type {
			get;
			private set;
		}

		public BoringValidationTestParameters (ValidationTestCategory category, BoringValidationTestType type, string identifier)
			: base (category, identifier)
		{
			Type = type;
		}

		public bool AddTrustedRoots {
			get; set;
		}

		public bool ExpectSuccess {
			get; set;
		}

		public DateTime? CheckTime {
			get; set;
		}

		public BoringVerifyParamType VerifyParamType {
			get; set;
		}

		public BtlsX509Error ExpectedResult {
			get; set;
		}

		protected BoringValidationTestParameters (BoringValidationTestParameters other)
			: base (other)
		{
			Type = other.Type;
			AddTrustedRoots = other.AddTrustedRoots;
			ExpectSuccess = other.ExpectSuccess;
			ExpectedResult = other.ExpectedResult;
			CheckTime = other.CheckTime;
			VerifyParamType = other.VerifyParamType;
		}

		public override ValidationParameters DeepClone ()
		{
			return new BoringValidationTestParameters (this);
		}
	}
}

