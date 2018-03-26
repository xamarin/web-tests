//
// InstrumentationFixture.cs
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
using System.IO;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Xamarin.AsyncTests;
using Xamarin.AsyncTests.Constraints;

namespace Xamarin.WebTests.HttpClientTests
{
	using ConnectionFramework;
	using HttpFramework;
	using HttpHandlers;
	using TestRunners;

	public abstract class InstrumentationFixture : CustomHandlerFixture, IInstrumentationCallbacks
	{
		bool IInstrumentationCallbacks.ConfigureNetworkStream (
			TestContext ctx, StreamInstrumentation instrumentation)
		{
			return ConfigureNetworkStream (ctx, instrumentation);
		}

		protected abstract bool ConfigureNetworkStream (
			TestContext ctx, StreamInstrumentation instrumentation);

		Task<bool> IInstrumentationCallbacks.PrimaryReadHandler (
			TestContext ctx, Request request, int bytesRead,
			CancellationToken cancellationToken)
		{
			return PrimaryReadHandler (
				ctx, (CustomRequest)request, bytesRead, cancellationToken);
		}

		protected abstract Task<bool> PrimaryReadHandler (
			TestContext ctx, CustomRequest request, int bytesRead,
			CancellationToken cancellationToken);

		Task<bool> IInstrumentationCallbacks.SecondaryReadHandler (
			TestContext ctx, HttpClientTestRunner runner,
			int bytesRead, CancellationToken cancellationToken)
		{
			return SecondaryReadHandler (
				ctx, runner, bytesRead, cancellationToken);
		}

		protected abstract Task<bool> SecondaryReadHandler (
			TestContext ctx, HttpClientTestRunner runner,
			int bytesRead, CancellationToken cancellationToken);
	}
}
