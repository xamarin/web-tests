//
// FlushInstrumentation.cs
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
	using HttpFramework;
	using TestRunners;

	[HttpServerFlags (HttpServerFlags.InternalVersionTwo)]
	public class FlushInstrumentation : StreamInstrumentationTestFixture
	{
		protected override bool UseCleanShutdown => true;

		protected override bool NeedClientInstrumentation => true;

		public enum InstrumentationType {
			Flush,
			WriteDoesNotFlush,
			FlushAfterDispose
		}

		public InstrumentationType Type {
			get;
		}

		[AsyncTest]
		public FlushInstrumentation (InstrumentationType type)
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

			int flushCalled = 0;
			ClientInstrumentation.OnNextFlush (FlushHandler);

			if (Type == InstrumentationType.WriteDoesNotFlush) {
				var text = ConnectionHandler.TheQuickBrownFoxBuffer;
				await Client.SslStream.WriteAsync (text, 0, text.Length, cancellationToken).ConfigureAwait (false);
				ctx.Assert (flushCalled, Is.EqualTo (0), "WriteAsync() does not flush the inner stream");
			}

			if (Type == InstrumentationType.FlushAfterDispose)
				Client.SslStream.Dispose ();

			await Client.SslStream.FlushAsync ().ConfigureAwait (false);
			ctx.Assert (flushCalled, Is.EqualTo (1), "Flush() called on inner stream");

			LogDebug (ctx, 4, $"{me} done");

			Task FlushHandler (StreamInstrumentation.AsyncFlushFunc func,
					   CancellationToken innerCancellationToken)
			{
				LogDebug (ctx, 4, $"FLUSH HANDLER!");
				Interlocked.Increment (ref flushCalled);
				return FinishedTask;
			}
		}
	}
}
