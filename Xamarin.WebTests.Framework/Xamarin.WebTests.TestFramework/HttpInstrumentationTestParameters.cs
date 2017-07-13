//
// HttpInstrumentationTestParameters.cs
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
using System.Net;
using System.Security.Cryptography.X509Certificates;

namespace Xamarin.WebTests.TestFramework
{
	using ConnectionFramework;
	using HttpFramework;

	[HttpInstrumentationTestParameters]
	public class HttpInstrumentationTestParameters : ConnectionTestParameters
	{
		public HttpInstrumentationTestType Type {
			get;
		}

		public HttpInstrumentationTestParameters (ConnectionTestCategory category, HttpInstrumentationTestType type,
		                                          string identifier, X509Certificate certificate)
			: base (category, identifier, certificate)
		{
			Type = type;
		}

		protected HttpInstrumentationTestParameters (HttpInstrumentationTestParameters other)
			: base (other)
		{
			Type = other.Type;
			CountParallelRequests = other.CountParallelRequests;
			ConnectionLimit = other.ConnectionLimit;
			IdleTime = other.IdleTime;
			ExpectedStatus = other.ExpectedStatus;
			ExpectedError = other.ExpectedError;
			HasReadHandler = other.HasReadHandler;
		}

		public int CountParallelRequests {
			get; set;
		}

		public int ConnectionLimit {
			get; set;
		}

		public int IdleTime {
			get; set;
		}

		public bool HasReadHandler {
			get; set;
		}

		public HttpStatusCode ExpectedStatus {
			get; set;
		}

		public WebExceptionStatus ExpectedError {
			get; set;
		}

		public override ConnectionParameters DeepClone ()
		{
			return new HttpInstrumentationTestParameters (this);
		}
	}
}
