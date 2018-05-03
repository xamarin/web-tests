//
// MultipleParallelRequests.cs
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
using System.Collections.Generic;
using Xamarin.AsyncTests;
using Xamarin.AsyncTests.Constraints;

namespace Xamarin.WebTests.HttpInstrumentationTests
{
	using ConnectionFramework;
	using TestFramework;
	using TestAttributes;
	using HttpFramework;
	using HttpHandlers;
	using TestRunners;

	[HttpServerFlags (HttpServerFlags.RequireMono)]
	[HttpServerTestCategory (HttpServerTestCategory.Stress)]
	public class MultipleParallelRequests : HttpInstrumentationTestFixture
	{
		public override RequestFlags RequestFlags => RequestFlags.KeepAlive | RequestFlags.CloseConnection;

		public ParallelType Type {
			get;
		}

		[AsyncTest]
		public MultipleParallelRequests (ParallelType type)
		{
			Type = type;
		}

		public enum ParallelType {
			TwoRequests,
			ThreeRequests,
			ParallelSomeQueued,
			[Stress]
			ManyParallel,
			[Stress]
			ManyParallelStress
		}

		ServicePoint servicePoint;

		protected override void ConfigurePrimaryRequest (
			TestContext ctx, InstrumentationOperation operation,
			TraditionalRequest request)
		{
			ctx.Assert (servicePoint, Is.Null, "ServicePoint");
			servicePoint = ServicePointManager.FindServicePoint (request.Uri);

			switch (Type) {
			case ParallelType.TwoRequests:
				break;
			case ParallelType.ThreeRequests:
				servicePoint.ConnectionLimit = 5;
				break;
			case ParallelType.ParallelSomeQueued:
				servicePoint.ConnectionLimit = 3;
				break;
			case ParallelType.ManyParallel:
				servicePoint.ConnectionLimit = 5;
				break;
			case ParallelType.ManyParallelStress:
				servicePoint.ConnectionLimit = 25;
				break;
			default:
				throw ctx.AssertFail (Type);
			}
			base.ConfigurePrimaryRequest (ctx, operation, request);
		}

		protected override void ConfigureSecondaryRequest (
			TestContext ctx, InstrumentationOperation operation,
			TraditionalRequest request)
		{
			ctx.Assert (servicePoint, Is.Not.Null, "ServicePoint");
			switch (Type) {
			case ParallelType.TwoRequests:
				ctx.Assert (servicePoint.CurrentConnections, Is.EqualTo (1), "ServicePoint.CurrentConnections");
				break;
			}
			base.ConfigureSecondaryRequest (ctx, operation, request);
		}

		public override HttpResponse HandleRequest (
			TestContext ctx, InstrumentationOperation operation,
			HttpConnection connection, HttpRequest request)
		{
			return new HttpResponse (HttpStatusCode.OK, ExpectedContent);
		}

		protected override bool ConfigureNetworkStream (
			TestContext ctx, StreamInstrumentation instrumentation)
		{
			return true;
		}

		int CountParallelRequests ()
		{
			switch (Type) {
			case ParallelType.ParallelSomeQueued:
				return 5;
			case ParallelType.ManyParallel:
				return 10;
			case ParallelType.ManyParallelStress:
				return 25;
			default:
				throw new InvalidOperationException ();
			}
		}

		int primaryReadHandlerCalled;
		int secondaryReadHandlerCalled;

		protected override async Task<bool> PrimaryReadHandler (
			TestContext ctx, InstrumentationOperation operation,
			byte[] buffer, int offset, int size, int bytesRead,
			CancellationToken cancellationToken)
		{
			primaryReadHandlerCalled++;
			var parallel = new List<Task> ();
			var parallelOperations = new List<HttpOperation> ();
			switch (Type) {
			case ParallelType.TwoRequests:
				parallel.Add (RunParallel ());
				break;
			case ParallelType.ThreeRequests:
				parallel.Add (RunParallel ());
				parallel.Add (RunParallel ());
				break;
			case ParallelType.ParallelSomeQueued:
			case ParallelType.ManyParallel:
			case ParallelType.ManyParallelStress:
				for (int i = 0; i < CountParallelRequests (); i++) {
					parallelOperations.Add (StartParallel ());
				}
				break;
			default:
				throw ctx.AssertFail (Type);
			}

			foreach (var parallelOperation in parallelOperations)
				parallel.Add (parallelOperation.WaitForCompletion ());

			await Task.WhenAll (parallel).ConfigureAwait (false);
			return false;

			Task RunParallel ()
			{
				return RunParallelOperation (
					ctx, HttpOperationFlags.None,
					cancellationToken);
			}

			HttpOperation StartParallel ()
			{
				return StartParallelOperation (
					ctx, HttpOperationFlags.None,
					cancellationToken);
			}
		}

		protected override Task<bool> SecondaryReadHandler (
			TestContext ctx, InstrumentationOperation operation,
			int bytesRead, CancellationToken cancellationToken)
		{
			secondaryReadHandlerCalled++;
			switch (Type) {
			case ParallelType.TwoRequests:
				ctx.Assert (servicePoint.CurrentConnections, Is.EqualTo (2), "ServicePoint.CurrentConnections");
				break;
			}
			return Task.FromResult (false);
		}

		protected override Task<HttpOperation> RunSecondary (
			TestContext ctx, CancellationToken cancellationToken)
		{
			switch (Type) {
			case ParallelType.TwoRequests:
				ctx.Assert (primaryReadHandlerCalled, Is.EqualTo (1), "PrimaryReadHandler called");
				ctx.Assert (secondaryReadHandlerCalled, Is.EqualTo (1), "SecondaryReadHandler called");
				break;
			case ParallelType.ThreeRequests:
				ctx.Assert (primaryReadHandlerCalled, Is.EqualTo (1), "PrimaryReadHandler called");
				ctx.Assert (secondaryReadHandlerCalled, Is.EqualTo (2), "SecondaryReadHandler called twice");
				break;
			}
			return Task.FromResult<HttpOperation> (null);
		}
	}
}
