﻿//
// TestValidator.cs
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
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using System.Security.Cryptography.X509Certificates;
using Mono.Security.Interface;
using Xamarin.AsyncTests;
using Xamarin.AsyncTests.Constraints;
using Xamarin.WebTests.TestFramework;
using Xamarin.WebTests.MonoTestFeatures;
using Xamarin.WebTests.MonoTestFramework;
using Xamarin.WebTests.Resources;
using Xamarin.WebTests.ConnectionFramework;

namespace Xamarin.WebTests.MonoTests
{
	[AsyncTestFixture]
	public class TestValidator
	{
		[AsyncTest]
		[ValidationTestCategory (ValidationTestCategory.Default)]
		public void Run (TestContext ctx,
				 MonoValidationTestParameters parameters,
				 MonoValidationTestRunner runner)
		{
			runner.Run (ctx);
		}

		[AsyncTest]
		[Martin ("MonoValidator")]
		[ValidationTestCategory (ValidationTestCategory.MartinTest)]
		public void MartinTest (TestContext ctx,
		                        MonoValidationTestParameters parameters,
		                        MonoValidationTestRunner runner)
		{
			runner.Run (ctx);
		}

		[AppleTls]
		[AsyncTest]
		[ValidationTestCategory (ValidationTestCategory.AppleTls)]
		public void RunAppleTls (TestContext ctx,
		                         MonoValidationTestParameters parameters,
		                         MonoValidationTestRunner runner)
		{
			runner.Run (ctx);
		}
	}
}

