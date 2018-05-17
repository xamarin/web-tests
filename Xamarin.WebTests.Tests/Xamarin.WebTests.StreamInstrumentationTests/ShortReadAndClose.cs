//
// ShortReadAndClose.cs
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
	using TestFramework;
	using TestRunners;

	public class ShortReadAndClose : StreamInstrumentationTestFixture
	{
		protected override bool UseCleanShutdown => true;

		protected override bool NeedClientInstrumentation => true;

		protected async override Task ClientShutdown (TestContext ctx, CancellationToken cancellationToken)
		{
			var me = $"{ME}({nameof (ClientShutdown)})";
			LogDebug (ctx, 4, me);

			bool readDone = false;

			ClientInstrumentation.OnNextRead (ReadHandler);

			var writeBuffer = ConnectionHandler.TheQuickBrownFoxBuffer;
			var readBuffer = new byte[writeBuffer.Length + 256];

			var readTask = ClientRead ();

			await Server.Stream.WriteAsync (writeBuffer, 0, writeBuffer.Length);

			Server.Close ();

			LogDebug (ctx, 4, $"{me} - closed server");

			await ctx.Assert (() => readTask, Is.EqualTo (writeBuffer.Length), "first client read").ConfigureAwait (false);

			LogDebug (ctx, 4, $"{me} - first read done");

			readDone = true;

			await ctx.Assert (ClientRead, Is.EqualTo (0), "second client read").ConfigureAwait (false);

			LogDebug (ctx, 4, $"{me} - second read done");

			async Task<int> ReadHandler (byte[] buffer, int offset, int size,
						     StreamInstrumentation.AsyncReadFunc func,
						     CancellationToken innerCancellationToken)
			{
				ClientInstrumentation.OnNextRead (ReadHandler);

				LogDebug (ctx, 4, $"{me} - read handler: {offset} {size}");
				var ret = await func (buffer, offset, size, innerCancellationToken).ConfigureAwait (false);
				LogDebug (ctx, 4, $"{me} - read handler done: {ret}");

				if (readDone)
					ctx.Assert (ret, Is.EqualTo (0), "inner read returns zero");
				return ret;
			}

			Task<int> ClientRead ()
			{
				return Client.Stream.ReadAsync (readBuffer, 0, readBuffer.Length, CancellationToken.None);
			}
		}

		protected override Task ServerShutdown (TestContext ctx, CancellationToken cancellationToken)
		{
			return FinishedTask;
		}
	}
}
