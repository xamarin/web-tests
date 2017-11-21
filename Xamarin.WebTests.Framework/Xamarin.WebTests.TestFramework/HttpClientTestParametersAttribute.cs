﻿//
// HttpClientTestParametersAttribute.cs
//
// Author:
//       Martin Baulig <mabaul@microsoft.com>
//
// Copyright (c) 2017 Xamarin Inc. (http://www.xamarin.com)
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
using System.Linq;
using System.Collections.Generic;
using Xamarin.AsyncTests;

namespace Xamarin.WebTests.TestFramework
{
	using ConnectionFramework;
	using TestRunners;

	[AttributeUsage (AttributeTargets.Class | AttributeTargets.Parameter, AllowMultiple = false)]
	public class HttpClientTestParametersAttribute : TestParameterAttribute, ITestParameterSource<HttpClientTestParameters>
	{
		public HttpClientTestType? Type {
			get; set;
		}

		public HttpClientTestParametersAttribute (string filter = null)
			: base (filter, TestFlags.Browsable | TestFlags.ContinueOnError)
		{
		}

		public HttpClientTestParametersAttribute (HttpClientTestType type)
			: base (null, TestFlags.Browsable | TestFlags.ContinueOnError)
		{
			Type = type;
		}

		public IEnumerable<HttpClientTestParameters> GetParameters (TestContext ctx, string filter)
		{
			if (filter != null)
				throw new NotImplementedException ();

			var category = ctx.GetParameter<ConnectionTestCategory> ();

			if (Type != null) {
				yield return HttpClientTestRunner.GetParameters (ctx, category, Type.Value);
				yield break;
			}

			foreach (var type in HttpClientTestRunner.GetTestTypes (ctx, category))
				yield return HttpClientTestRunner.GetParameters (ctx, category, type);
		}
	}
}
