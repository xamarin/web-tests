//
// ServerRequestsShutdown.cs
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
using System.Text;
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

	[ConnectionTestCategory (ConnectionTestCategory.SslStreamInstrumentationServerShutdown)]
	public class ServerRequestsShutdown : StreamInstrumentationTestFixture
	{
		protected override bool UseCleanShutdown => true;

		protected override bool NeedClientInstrumentation => true;

		public bool ShutdownDuringWrite {
			get;
		}

		[AsyncTest]
		public ServerRequestsShutdown (bool shutdownDuringWrite)
		{
			ShutdownDuringWrite = shutdownDuringWrite;
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

			cancellationToken.ThrowIfCancellationRequested ();

			var writeEvent = new TaskCompletionSource<object> ();
			var shutdownEvent = new TaskCompletionSource<object> ();
			cancellationToken.Register (() => writeEvent.TrySetCanceled ());
			cancellationToken.Register (() => shutdownEvent.TrySetCanceled ());

			ClientInstrumentation.OnNextWrite (WriteHandler);

			int totalBytes = 0;

			if (ShutdownDuringWrite) {
				var largeBuffer = ConnectionHandler.GetLargeText (512);
				var largeBlob = Encoding.UTF8.GetBytes (largeBuffer);

				totalBytes = largeBlob.Length;
				LogDebug (ctx, 4, $"{me} writing {totalBytes} bytes");
				var writeTask = Client.SslStream.WriteAsync (largeBlob, 0, largeBlob.Length, cancellationToken);

				cancellationToken.ThrowIfCancellationRequested ();
				await writeEvent.Task.ConfigureAwait (false);
				LogDebug (ctx, 4, $"{me} done writing");
			}

			cancellationToken.ThrowIfCancellationRequested ();

			LogDebug (ctx, 4, $"{me}: server shutdown");
			await Server.Shutdown (ctx, cancellationToken).ConfigureAwait (false);
			LogDebug (ctx, 4, $"{me}: server shutdown done");

			cancellationToken.ThrowIfCancellationRequested ();

			var readBuffer = new byte[32768];
			var ret = await Client.SslStream.ReadAsync (readBuffer, 0, readBuffer.Length, cancellationToken);
			LogDebug (ctx, 4, $"{me}: client read: {ret}");

			shutdownEvent.TrySetResult (null);

			cancellationToken.ThrowIfCancellationRequested ();

			int remainingBytes = totalBytes;
			while (remainingBytes > 0) {
				LogDebug (ctx, 4, $"{me}: server read - {remainingBytes} / {totalBytes} remaining.");

				ret = await Server.SslStream.ReadAsync (readBuffer, 0, readBuffer.Length, cancellationToken);
				LogDebug (ctx, 4, $"{me}: server read returned: {ret}");

				if (ret <= 0)
					break;

				remainingBytes -= ret;
			}

			if (remainingBytes > 0)
				LogDebug (ctx, 4, $"{me} server read - connection closed with {remainingBytes} bytes remaining.");
			else
				LogDebug (ctx, 4, $"{me} server read complete.");

			async Task WriteHandler (byte[] buffer, int offset, int count,
						 StreamInstrumentation.AsyncWriteFunc func,
						 CancellationToken innerCancellationToken)
			{
				innerCancellationToken.ThrowIfCancellationRequested ();

				var writeMe = $"{me}: write handler ({ctx.GetUniqueId ()})";
				LogDebug (ctx, 4, $"{writeMe}: {offset} {count}");

				ClientInstrumentation.OnNextWrite (WriteHandler);

				const int shortWrite = 4096;
				if (count > shortWrite) {
					await func (buffer, offset, shortWrite, innerCancellationToken).ConfigureAwait (false);
					offset += shortWrite;
					count -= shortWrite;

					LogDebug (ctx, 4, $"{writeMe}: short write done");
					writeEvent.TrySetResult (null);

					await shutdownEvent.Task;

					LogDebug (ctx, 4, $"{writeMe}: remaining {offset} {count}");
				}

				await func (buffer, offset, count, innerCancellationToken).ConfigureAwait (false);
				LogDebug (ctx, 4, $"{writeMe}: done");

				await FinishedTask;
			}
		}
	
	}
}
