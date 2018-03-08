//
// InstrumentationHandler.cs
//
// Author:
//       Martin Baulig <mabaul@microsoft.com>
//
// Copyright (c) 2018 Xamarin Inc. (http://www.xamarin.com)
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
using Xamarin.AsyncTests;
using Xamarin.AsyncTests.Constraints;

namespace Xamarin.WebTests.TestRunners
{
	using HttpFramework;
	using HttpHandlers;

	public abstract class InstrumentationHandler : Handler
	{
		public InstrumentationTestRunner TestRunner {
			get;
		}

		public string ME {
			get;
		}

		public InstrumentationHandler (InstrumentationTestRunner parent, string identifier)
			: base (identifier)
		{
			TestRunner = parent;
			ME = $"{GetType ().Name}({identifier})";
		}

		protected InstrumentationHandler (InstrumentationHandler other)
			: base (other.Value)
		{
			ME = other.ME;
			TestRunner = other.TestRunner;
		}

		public IPEndPoint RemoteEndPoint {
			get;
			protected set;
		}

		protected void AssertNotReusingConnection (TestContext ctx, HttpConnection connection)
		{
			var firstHandler = TestRunner.PrimaryHandler;
			ctx.LogDebug (2, $"{ME}: {this == firstHandler} {RemoteEndPoint}");
			if (this == firstHandler)
				return;
			ctx.Assert (connection.RemoteEndPoint, Is.Not.EqualTo (firstHandler.RemoteEndPoint), "RemoteEndPoint");
		}

		protected void AssertReusingConnection (TestContext ctx, HttpConnection connection)
		{
			var firstHandler = TestRunner.PrimaryHandler;
			ctx.LogDebug (2, $"{ME}: {this == firstHandler} {RemoteEndPoint}");
			if (this == firstHandler)
				return;
			ctx.Assert (connection.RemoteEndPoint, Is.EqualTo (firstHandler.RemoteEndPoint), "RemoteEndPoint");
		}
	}
}
