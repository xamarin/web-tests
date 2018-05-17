//
// DisposeInstrumentation.cs
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
using System.Threading;
using System.Threading.Tasks;
using Xamarin.AsyncTests;
using Xamarin.AsyncTests.Constraints;

namespace Xamarin.WebTests.StreamInstrumentationTests
{
	using ConnectionFramework;
	using TestAttributes;
	using TestFramework;
	using TestRunners;

	[ConnectionTestCategory (ConnectionTestCategory.SslStreamInstrumentationNewWebStack)]
	public class DisposeInstrumentation : StreamInstrumentationTestFixture
	{
		protected override bool UseCleanShutdown => true;

		protected override bool NeedClientInstrumentation => true;

		public enum InstrumentationType
		{
			Flush,
			WriteDoesNotFlush,
			FlushAfterDispose
		}

		public InstrumentationType Type {
			get;
		}

		[AsyncTest]
		public DisposeInstrumentation (InstrumentationType type)
		{
			Type = type;
		}

		protected override Task ClientShutdown (TestContext ctx, CancellationToken cancellationToken)
		{
			return FinishedTask;
		}

		protected override Task ServerShutdown (TestContext ctx, CancellationToken cancellationToken)
		{
			return FinishedTask;
		}

		protected override async Task MainLoop (TestContext ctx, CancellationToken cancellationToken)
		{
			var me = $"{ME}({nameof (MainLoop)})";
			LogDebug (ctx, 4, me);

			await FinishedTask;

			int disposeCalled = 0;
			ClientInstrumentation.OnDispose (DisposeHandler);

			Client.SslStream.Dispose ();

			ctx.Expect (disposeCalled, Is.EqualTo (1), "called Dispose() on inner stream");

			ctx.Expect (() => Client.SslStream.CanRead, Is.False, "SslStream.CanRead after dispose");
			ctx.Expect (() => Client.SslStream.CanWrite, Is.False, "SslStream.CanWrite after dispose");
			ctx.Assert (() => Client.SslStream.CanTimeout, Is.True, "SslStream.CanTimeout after dispose");

			void DisposeHandler (StreamInstrumentation.DisposeFunc func)
			{
				Interlocked.Increment (ref disposeCalled);
				func ();
			}
		}
	}
}
