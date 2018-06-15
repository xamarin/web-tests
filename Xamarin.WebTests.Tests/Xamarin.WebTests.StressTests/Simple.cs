//
// Simple.cs
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
using System.Threading;
using System.Threading.Tasks;
using Xamarin.AsyncTests;

namespace Xamarin.WebTests.StressTests
{
	using HttpFramework;
	using HttpOperations;
	using HttpHandlers;

	public class Simple : StressTestFixture
	{
		public const int MaxServicePoints = 100;

		public const int CountParallelTasks = 75;

		public const int RepeatCount = 2500;
	
		protected override async Task Run (TestContext ctx, CancellationToken cancellationToken)
		{
			var maxServicePoints = ServicePointManager.MaxServicePoints;
			try {
				ServicePointManager.MaxServicePoints = MaxServicePoints;

				var tasks = new Task[CountParallelTasks + 1];
				for (int i = 0; i < tasks.Length; i++) {
					tasks[i] = RunLoop (ctx, i, cancellationToken);
				}

				ctx.LogDebug (LogCategory, 2, $"{ME} GOT TASKS");

				await Task.WhenAll (tasks).ConfigureAwait (false);
			} finally {
				ServicePointManager.MaxServicePoints = maxServicePoints;
			}
		}

		async Task RunLoop (TestContext ctx, int idx, CancellationToken cancellationToken)
		{
			var loopMe = $"{ME} LOOP {idx}";

			for (int i = 0; i < RepeatCount; i++) {
				await LoopOperation (ctx, i, cancellationToken).ConfigureAwait (false);
				if ((i % 10) == 0)
					ctx.LogMessage ($"{loopMe} {i} DONE");
			}
		}

		async Task LoopOperation (TestContext ctx, int idx, CancellationToken cancellationToken)
		{
			var handler = HelloWorldHandler.GetSimple ($"{ME}:{idx}");
			var loopOperation = new TraditionalOperation (Server, handler, true);

			await loopOperation.Run (ctx, cancellationToken).ConfigureAwait (false);
		}
	}
}
