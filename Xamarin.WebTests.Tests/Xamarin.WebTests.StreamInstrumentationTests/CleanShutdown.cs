//
// CleanShutdown.cs
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

	[ConnectionTestCategory (ConnectionTestCategory.SslStreamInstrumentationShutdown)]
	public class CleanShutdown : StreamInstrumentationTestFixture
	{
		protected override bool UseCleanShutdown => true;

		protected override bool NeedClientInstrumentation => true;

		readonly TaskCompletionSource<bool> clientTcs = new TaskCompletionSource<bool> ();

		public enum ShutdownType {
			CleanShutdown,
			DoubleShutdown,
			WriteAfterShutdown,
			ReadAfterShutdown,
			WaitForShutdown
		}

		public ShutdownType Type {
			get;
		}

		[AsyncTest]
		public CleanShutdown (ShutdownType type)
		{
			Type = type;
		}

		protected override async Task ClientShutdown (TestContext ctx, CancellationToken cancellationToken)
		{
			var me = $"{ME}({nameof (ClientShutdown)})";
			ctx.LogDebug (LogCategories.StreamInstrumentationTestRunner, 4, me);

			int bytesWritten = 0;

			ClientInstrumentation.OnNextWrite (WriteHandler);

			try {
				cancellationToken.ThrowIfCancellationRequested ();
				await Client.Shutdown (ctx, cancellationToken).ConfigureAwait (false);
				ctx.LogDebug (LogCategories.StreamInstrumentationTestRunner, 4, $"{me} done");
			} catch (OperationCanceledException) {
				ctx.LogDebug (LogCategories.StreamInstrumentationTestRunner, 4, $"{me} canceled");
				clientTcs.TrySetCanceled ();
				throw;
			} catch (Exception ex) {
				ctx.LogDebug (LogCategories.StreamInstrumentationTestRunner, 4, $"{me} error: {ex.Message}");
				clientTcs.TrySetException (ex);
				throw;
			}

			var ok = ctx.Expect (bytesWritten, Is.GreaterThan (0), $"{me} - bytes written");
			ok &= ctx.Expect (Client.SslStream.CanWrite, Is.False, $"{me} - SslStream.CanWrite");

			clientTcs.TrySetResult (ok);

			cancellationToken.ThrowIfCancellationRequested ();

			switch (Type) {
			case ShutdownType.CleanShutdown:
				break;
			case ShutdownType.DoubleShutdown:
				await DoubleShutdown ();
				break;
			case ShutdownType.WriteAfterShutdown:
				await WriteAfterShutdown ();
				break;
			case ShutdownType.ReadAfterShutdown:
				await ReadAfterShutdown ();
				break;
			case ShutdownType.WaitForShutdown:
				break;
			default:
				throw ctx.AssertFail (Type);
			}

			Task DoubleShutdown ()
			{
				// Don't use Client.Shutdown() as that might throw a different exception.
				var setup = DependencyInjector.Get<IConnectionFrameworkSetup> ();
				return ctx.AssertException<InvalidOperationException> (
					() => setup.ShutdownAsync (Client.SslStream), "double shutdown");
			}

			Task WriteAfterShutdown ()
			{
				return ctx.AssertException<InvalidOperationException> (() => ConnectionHandler.WriteBlob (ctx, Client, cancellationToken));
			}

			async Task ReadAfterShutdown ()
			{
				ctx.LogDebug (LogCategories.StreamInstrumentationTestRunner, 4, $"{me} reading");
				await ConnectionHandler.ExpectBlob (ctx, Client, cancellationToken).ConfigureAwait (false);
				ctx.LogDebug (LogCategories.StreamInstrumentationTestRunner, 4, $"{me} reading done");
			}

			async Task WriteHandler (byte[] buffer, int offset, int size,
						 StreamInstrumentation.AsyncWriteFunc func,
						 CancellationToken innerCancellationToken)
			{
				cancellationToken.ThrowIfCancellationRequested ();
				innerCancellationToken.ThrowIfCancellationRequested ();
				ctx.LogDebug (LogCategories.StreamInstrumentationTestRunner, 4, $"{me} write handler: {offset} {size}");
				await func (buffer, offset, size, innerCancellationToken).ConfigureAwait (false);
				ctx.LogDebug (LogCategories.StreamInstrumentationTestRunner, 4, $"{me} write handler done");
				bytesWritten += size;
			}
		}

		protected override async Task ServerShutdown (TestContext ctx, CancellationToken cancellationToken)
		{
			var me = $"{ME}({nameof (ServerShutdown)})";
			ctx.LogDebug (LogCategories.StreamInstrumentationTestRunner, 4, me);

			switch (Type) {
			case ShutdownType.CleanShutdown:
			case ShutdownType.WriteAfterShutdown:
			case ShutdownType.DoubleShutdown:
				return;
			case ShutdownType.ReadAfterShutdown:
				await ReadAfterShutdown ().ConfigureAwait (false);
				return;
			case ShutdownType.WaitForShutdown:
				await WaitForShutdown ().ConfigureAwait (false);
				break;
			default:
				throw ctx.AssertFail (Type);
			}

			async Task ReadAfterShutdown ()
			{
				cancellationToken.ThrowIfCancellationRequested ();
				var ok = await clientTcs.Task.ConfigureAwait (false);
				ctx.LogDebug (LogCategories.StreamInstrumentationTestRunner, 4, $"{me} - client finished {ok}");

				cancellationToken.ThrowIfCancellationRequested ();

				await ConnectionHandler.WriteBlob (ctx, Server, cancellationToken).ConfigureAwait (false);
				ctx.LogDebug (LogCategories.StreamInstrumentationTestRunner, 4, $"{me} - write done");
			}

			async Task WaitForShutdown ()
			{
				cancellationToken.ThrowIfCancellationRequested ();
				var ok = await clientTcs.Task.ConfigureAwait (false);
				ctx.LogDebug (LogCategories.StreamInstrumentationTestRunner, 4, $"{me} - client finished {ok}");

				cancellationToken.ThrowIfCancellationRequested ();

				var buffer = new byte[256];
				await ctx.Assert (() => Server.Stream.ReadAsync (buffer, 0, buffer.Length), Is.EqualTo (0), "wait for shutdown");
			}
		}
	}
}
