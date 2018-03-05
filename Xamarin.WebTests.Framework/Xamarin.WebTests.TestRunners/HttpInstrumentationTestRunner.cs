//
// HttpInstrumentationTestRunner.cs
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
using System.IO;
using System.Text;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using System.Collections;
using System.Collections.Generic;
using Xamarin.AsyncTests;
using Xamarin.AsyncTests.Constraints;
using Xamarin.AsyncTests.Portable;

namespace Xamarin.WebTests.TestRunners
{
	using ConnectionFramework;
	using HttpFramework;
	using HttpHandlers;
	using TestFramework;
	using TestAttributes;

	[HttpInstrumentationTestRunner]
	public class HttpInstrumentationTestRunner : InstrumentationTestRunner
	{
		public HttpInstrumentationTestType Type {
			get;
		}

		public HttpInstrumentationTestType EffectiveType => GetEffectiveType (Type);

		static HttpInstrumentationTestType GetEffectiveType (HttpInstrumentationTestType type)
		{
			if (type == HttpInstrumentationTestType.MartinTest)
				return MartinTest;
			return type;
		}

		public HttpInstrumentationTestRunner (HttpServerProvider provider, HttpInstrumentationTestType type)
			: base (provider, type.ToString ())
		{
			Type = type;
		}

		const HttpInstrumentationTestType MartinTest = HttpInstrumentationTestType.NtlmInstrumentation;

		static readonly (HttpInstrumentationTestType type, HttpInstrumentationTestFlags flags) [] TestRegistration = {
			(HttpInstrumentationTestType.InvalidDataDuringHandshake, HttpInstrumentationTestFlags.Working),
			(HttpInstrumentationTestType.AbortDuringHandshake, HttpInstrumentationTestFlags.Working),
			(HttpInstrumentationTestType.ParallelRequests, HttpInstrumentationTestFlags.Working),
			(HttpInstrumentationTestType.ThreeParallelRequests, HttpInstrumentationTestFlags.Stress),
			(HttpInstrumentationTestType.ParallelRequestsSomeQueued, HttpInstrumentationTestFlags.Stress),
			(HttpInstrumentationTestType.ManyParallelRequests, HttpInstrumentationTestFlags.Stress),
			(HttpInstrumentationTestType.ManyParallelRequestsStress, HttpInstrumentationTestFlags.Stress),
			(HttpInstrumentationTestType.SimpleQueuedRequest, HttpInstrumentationTestFlags.Working),
			(HttpInstrumentationTestType.CancelQueuedRequest, HttpInstrumentationTestFlags.Working),
			(HttpInstrumentationTestType.CancelMainWhileQueued, HttpInstrumentationTestFlags.Working),
			(HttpInstrumentationTestType.SimpleNtlm, HttpInstrumentationTestFlags.Working),
			(HttpInstrumentationTestType.NtlmWhileQueued, HttpInstrumentationTestFlags.Working),
			(HttpInstrumentationTestType.NtlmWhileQueued2, HttpInstrumentationTestFlags.Working),
			(HttpInstrumentationTestType.ReuseConnection, HttpInstrumentationTestFlags.Working),
			(HttpInstrumentationTestType.PostNtlm, HttpInstrumentationTestFlags.Working),
			(HttpInstrumentationTestType.NtlmChunked, HttpInstrumentationTestFlags.Working),
			(HttpInstrumentationTestType.ReuseConnection2, HttpInstrumentationTestFlags.Working),
			(HttpInstrumentationTestType.CloseIdleConnection, HttpInstrumentationTestFlags.NewWebStack),
			(HttpInstrumentationTestType.NtlmInstrumentation, HttpInstrumentationTestFlags.NewWebStack),
			(HttpInstrumentationTestType.NtlmClosesConnection, HttpInstrumentationTestFlags.NewWebStack),
			(HttpInstrumentationTestType.NtlmReusesConnection, HttpInstrumentationTestFlags.NewWebStack),
			(HttpInstrumentationTestType.ParallelNtlm, HttpInstrumentationTestFlags.NewWebStack),
			(HttpInstrumentationTestType.ReuseAfterPartialRead, HttpInstrumentationTestFlags.Working),
			(HttpInstrumentationTestType.CustomConnectionGroup, HttpInstrumentationTestFlags.Working),
			(HttpInstrumentationTestType.ReuseCustomConnectionGroup, HttpInstrumentationTestFlags.Working),
			(HttpInstrumentationTestType.CloseCustomConnectionGroup, HttpInstrumentationTestFlags.Working),
			(HttpInstrumentationTestType.AbortResponse, HttpInstrumentationTestFlags.Working),
			(HttpInstrumentationTestType.RedirectOnSameConnection, HttpInstrumentationTestFlags.Working),
		};

		public static IList<HttpInstrumentationTestType> GetInstrumentationTypes (TestContext ctx, HttpServerTestCategory category)
		{
			if (category == HttpServerTestCategory.MartinTest)
				return new[] { MartinTest };

			var setup = DependencyInjector.Get<IConnectionFrameworkSetup> ();
			return TestRegistration.Where (t => Filter (t.flags)).Select (t => t.type).ToList ();

			bool Filter (HttpInstrumentationTestFlags flags)
			{
				switch (category) {
				case HttpServerTestCategory.MartinTest:
					return false;
				case HttpServerTestCategory.Instrumentation:
					return flags == HttpInstrumentationTestFlags.Working;
				case HttpServerTestCategory.Stress:
					return flags == HttpInstrumentationTestFlags.Stress;
				case HttpServerTestCategory.NewWebStackInstrumentation:
					return flags == HttpInstrumentationTestFlags.NewWebStack;
				case HttpServerTestCategory.Experimental:
					return flags == HttpInstrumentationTestFlags.Unstable;
				default:
					throw ctx.AssertFail (category);
				}
			}
		}

		const int IdleTime = 750;

		protected override async Task RunSecondary (TestContext ctx, CancellationToken cancellationToken)
		{
			var me = $"{ME}.{nameof (RunSecondary)}()";

			Operation secondOperation = null;

			switch (EffectiveType) {
			case HttpInstrumentationTestType.ParallelRequests:
				ctx.Assert (ReadHandlerCalled, Is.EqualTo (2), "ReadHandler called twice");
				break;
			case HttpInstrumentationTestType.ThreeParallelRequests:
				ctx.Assert (ReadHandlerCalled, Is.EqualTo (3), "ReadHandler called three times");
				break;
			case HttpInstrumentationTestType.SimpleQueuedRequest:
				ctx.Assert (QueuedOperation, Is.Not.Null, "have queued task");
				await QueuedOperation.WaitForCompletion ().ConfigureAwait (false);
				ctx.Assert (ReadHandlerCalled, Is.EqualTo (2), "ReadHandler called twice");
				break;
			case HttpInstrumentationTestType.ParallelRequestsSomeQueued:
			case HttpInstrumentationTestType.ManyParallelRequests:
			case HttpInstrumentationTestType.ManyParallelRequestsStress:
				// ctx.Assert (ReadHandlerCalled, Is.EqualTo (Parameters.CountParallelRequests + 1), "ReadHandler count");
				break;
			case HttpInstrumentationTestType.ReuseConnection:
			case HttpInstrumentationTestType.ReuseConnection2:
			case HttpInstrumentationTestType.ReuseAfterPartialRead:
			case HttpInstrumentationTestType.CustomConnectionGroup:
			case HttpInstrumentationTestType.ReuseCustomConnectionGroup:
				secondOperation = StartSecond (ctx, cancellationToken);
				break;
			case HttpInstrumentationTestType.CloseIdleConnection:
				ctx.LogDebug (5, $"{me}: active connections: {PrimaryOperation.ServicePoint.CurrentConnections}");
				await Task.Delay ((int)(IdleTime * 2.5)).ConfigureAwait (false);
				ctx.LogDebug (5, $"{me}: active connections #1: {PrimaryOperation.ServicePoint.CurrentConnections}");
				ctx.Assert (PrimaryOperation.ServicePoint.CurrentConnections, Is.EqualTo (0), "current connections");
				break;
			case HttpInstrumentationTestType.CloseCustomConnectionGroup:
				ctx.LogDebug (5, $"{me}: active connections: {PrimaryOperation.ServicePoint.CurrentConnections}");
				PrimaryOperation.ServicePoint.CloseConnectionGroup (((TraditionalRequest)PrimaryOperation.Request).RequestExt.ConnectionGroupName);
				ctx.LogDebug (5, $"{me}: active connections #1: {PrimaryOperation.ServicePoint.CurrentConnections}");
				break;
			}

			if (secondOperation != null) {
				ctx.LogDebug (2, $"{me} waiting for second operation.");
				try {
					await secondOperation.WaitForCompletion ().ConfigureAwait (false);
					ctx.LogDebug (2, $"{me} done waiting for second operation.");
				} catch (Exception ex) {
					ctx.LogDebug (2, $"{me} waiting for second operation failed: {ex.Message}.");
					throw;
				}
			}
		}

		protected override (Handler handler, HttpOperationFlags flags) CreateHandler (TestContext ctx, bool primary)
		{
			var hello = new HelloWorldHandler (EffectiveType.ToString ());
			var helloKeepAlive = new HelloWorldHandler (EffectiveType.ToString ()) {
				Flags = RequestFlags.KeepAlive
			};
			var postHello = new PostHandler (EffectiveType.ToString (), HttpContent.HelloWorld);
			var chunkedPost = new PostHandler (EffectiveType.ToString (), HttpContent.HelloChunked, TransferMode.Chunked);

			switch (EffectiveType) {
			case HttpInstrumentationTestType.InvalidDataDuringHandshake:
			case HttpInstrumentationTestType.AbortDuringHandshake:
			case HttpInstrumentationTestType.CancelMainWhileQueued:
				return (hello, HttpOperationFlags.ServerAbortsHandshake | HttpOperationFlags.AbortAfterClientExits);
			case HttpInstrumentationTestType.SimpleNtlm:
				return (new AuthenticationHandler (AuthenticationType.NTLM, hello), HttpOperationFlags.None);
			case HttpInstrumentationTestType.PostNtlm:
				return (new AuthenticationHandler (AuthenticationType.NTLM, postHello), HttpOperationFlags.None);
			case HttpInstrumentationTestType.NtlmChunked:
				return (new AuthenticationHandler (AuthenticationType.NTLM, chunkedPost), HttpOperationFlags.None);
			case HttpInstrumentationTestType.ParallelRequests:
			case HttpInstrumentationTestType.SimpleQueuedRequest:
			case HttpInstrumentationTestType.CancelQueuedRequest:
				return (hello, HttpOperationFlags.None);
			default:
				var handler = new HttpInstrumentationHandler (this, primary);
				return (handler, handler.OperationFlags);
			}
		}

		internal AuthenticationManager GetAuthenticationManager ()
		{
			var manager = new AuthenticationManager (AuthenticationType.NTLM, AuthenticationHandler.GetCredentials ());
			var old = Interlocked.CompareExchange (ref authManager, manager, null);
			return old ?? manager;
		}

		internal void AbortPrimaryRequest ()
		{
			PrimaryOperation.Request.Abort ();
		}

		internal Task StartDelayedSecondaryOperation (TestContext ctx)
		{
			return QueuedOperation.StartDelayedListener (ctx);
		}

		protected override InstrumentationOperation CreateOperation (
			TestContext ctx, Handler handler, InstrumentationOperationType type, HttpOperationFlags flags)
		{
			HttpStatusCode expectedStatus;
			WebExceptionStatus expectedError;

			switch (EffectiveType) {
			case HttpInstrumentationTestType.InvalidDataDuringHandshake:
				expectedStatus = HttpStatusCode.InternalServerError;
				expectedError = WebExceptionStatus.SecureChannelFailure;
				break;
			case HttpInstrumentationTestType.AbortDuringHandshake:
			case HttpInstrumentationTestType.AbortResponse:
				expectedStatus = HttpStatusCode.InternalServerError;
				expectedError = WebExceptionStatus.RequestCanceled;
				break;
			case HttpInstrumentationTestType.CancelMainWhileQueued:
			case HttpInstrumentationTestType.NtlmWhileQueued:
				if (type == InstrumentationOperationType.Primary) {
					expectedStatus = HttpStatusCode.InternalServerError;
					expectedError = WebExceptionStatus.RequestCanceled;
				} else {
					expectedStatus = HttpStatusCode.OK;
					expectedError = WebExceptionStatus.Success;
				}
				break;
			case HttpInstrumentationTestType.CancelQueuedRequest:
				if (type == InstrumentationOperationType.Queued) {
					expectedStatus = HttpStatusCode.InternalServerError;
					expectedError = WebExceptionStatus.RequestCanceled;
				} else {
					expectedStatus = HttpStatusCode.OK;
					expectedError = WebExceptionStatus.Success;
				}
				break;
			default:
				expectedStatus = HttpStatusCode.OK;
				expectedError = WebExceptionStatus.Success;
				break;
			}

			return new Operation (this, handler, type, flags, expectedStatus, expectedError);
		}

		internal HttpOperation StartParallelNtlm (
			TestContext ctx, HttpInstrumentationHandler handler,
			AuthenticationState state, CancellationToken cancellationToken)
		{
			var firstHandler = (HttpInstrumentationHandler)PrimaryOperation.Handler;
			ctx.LogDebug (2, $"{handler.ME}: {handler == firstHandler} {state}");
			if (handler != firstHandler || state != AuthenticationState.Challenge)
				return null;

			var newHandler = (HttpInstrumentationHandler)firstHandler.Clone ();
			var flags = PrimaryOperation.Flags;

			return StartOperation (
				ctx, cancellationToken, newHandler,
				InstrumentationOperationType.Queued, flags);
		}

		Operation StartSecond (TestContext ctx, CancellationToken cancellationToken,
				       HttpStatusCode expectedStatus = HttpStatusCode.OK,
				       WebExceptionStatus expectedError = WebExceptionStatus.Success)
		{
			var (handler, flags) = CreateHandler (ctx, false);
			var operation = new Operation (this, handler, InstrumentationOperationType.Parallel, flags, expectedStatus, expectedError);
			operation.Start (ctx, cancellationToken);
			return operation;
		}

		protected override async Task PrimaryReadHandler (TestContext ctx, CancellationToken cancellationToken)
		{
			Request request;
			InstrumentationOperation operation;
			switch (EffectiveType) {
			case HttpInstrumentationTestType.ParallelRequests:
				ctx.Assert (PrimaryOperation.HasRequest, "current request");
				await RunSimpleHello ().ConfigureAwait (false);
				break;

			case HttpInstrumentationTestType.SimpleQueuedRequest:
				ctx.Assert (PrimaryOperation.HasRequest, "current request");
				StartOperation (
					ctx, cancellationToken, HelloWorldHandler.GetSimple (),
					InstrumentationOperationType.Queued, HttpOperationFlags.None);
				break;

			case HttpInstrumentationTestType.ThreeParallelRequests:
				ctx.Assert (PrimaryOperation.HasRequest, "current request");
				var secondTask = RunSimpleHello ();
				var thirdTask = RunSimpleHello ();
				await Task.WhenAll (secondTask, thirdTask).ConfigureAwait (false);
				break;

			case HttpInstrumentationTestType.ParallelRequestsSomeQueued:
			case HttpInstrumentationTestType.ManyParallelRequests:
			case HttpInstrumentationTestType.ManyParallelRequestsStress:
				ctx.Assert (PrimaryOperation.HasRequest, "current request");
				var countParallel = CountParallelRequests ();
				var parallelTasks = new Task [countParallel];
				var parallelOperations = new InstrumentationOperation [countParallel];
				for (int i = 0; i < parallelOperations.Length; i++)
					parallelOperations [i] = StartOperation (
						ctx, cancellationToken, HelloWorldHandler.GetSimple (),
						InstrumentationOperationType.Parallel, HttpOperationFlags.None);
				for (int i = 0; i < parallelTasks.Length; i++)
					parallelTasks [i] = parallelOperations [i].WaitForCompletion ();
				await Task.WhenAll (parallelTasks).ConfigureAwait (false);
				break;

			case HttpInstrumentationTestType.AbortDuringHandshake:
				ctx.Assert (PrimaryOperation.HasRequest, "current request");
				PrimaryOperation.Request.Abort ();
				// Wait until the client request finished, to make sure we are actually aboring.
				await PrimaryOperation.WaitForCompletion ().ConfigureAwait (false);
				break;

			case HttpInstrumentationTestType.CancelQueuedRequest:
				ctx.Assert (PrimaryOperation.HasRequest, "current request");
				operation = StartOperation (
					ctx, cancellationToken, HelloWorldHandler.GetSimple (),
					InstrumentationOperationType.Queued, HttpOperationFlags.AbortAfterClientExits);
				request = await operation.WaitForRequest ().ConfigureAwait (false);
				// Wait a bit to make sure the request has been queued.
				await Task.Delay (500).ConfigureAwait (false);
				request.Abort ();
				break;

			case HttpInstrumentationTestType.CancelMainWhileQueued:
				ctx.Assert (PrimaryOperation.HasRequest, "current request");
				operation = StartOperation (
					ctx, cancellationToken, HelloWorldHandler.GetSimple (),
					InstrumentationOperationType.Queued, HttpOperationFlags.None);
				request = await operation.WaitForRequest ().ConfigureAwait (false);
				// Wait a bit to make sure the request has been queued.
				await Task.Delay (2500).ConfigureAwait (false);
				PrimaryOperation.Request.Abort ();
				break;

			case HttpInstrumentationTestType.NtlmWhileQueued:
				ctx.Assert (PrimaryOperation.HasRequest, "current request");
				if (QueuedOperation == null) {
					StartOperation (
						ctx, cancellationToken, HelloWorldHandler.GetSimple (),
						InstrumentationOperationType.Queued,
						HttpOperationFlags.DelayedListenerContext | HttpOperationFlags.ClientAbortsRequest);
				}
				break;

			case HttpInstrumentationTestType.NtlmWhileQueued2:
				ctx.Assert (PrimaryOperation.HasRequest, "current request");
				if (QueuedOperation == null) {
					StartOperation (
						ctx, cancellationToken, HelloWorldHandler.GetSimple (),
						InstrumentationOperationType.Queued,
						HttpOperationFlags.DelayedListenerContext);
				}
				break;

			default:
				throw ctx.AssertFail (EffectiveType);
			}

			int CountParallelRequests ()
			{
				switch (EffectiveType) {
				case HttpInstrumentationTestType.ParallelRequestsSomeQueued:
					return 5;
				case HttpInstrumentationTestType.ManyParallelRequests:
					return 10;
				case HttpInstrumentationTestType.ManyParallelRequestsStress:
					return 100;
				default:
					throw ctx.AssertFail (EffectiveType);
				}
			}

			Task RunSimpleHello ()
			{
				return StartOperation (
					ctx, cancellationToken, HelloWorldHandler.GetSimple (),
					InstrumentationOperationType.Parallel, HttpOperationFlags.None).WaitForCompletion ();
			}
		}

		protected override async Task SecondaryReadHandler (TestContext ctx, CancellationToken cancellationToken)
		{
			await FinishedTask.ConfigureAwait (false);

			switch (EffectiveType) {
			case HttpInstrumentationTestType.ParallelRequests:
				ctx.Assert (PrimaryOperation.HasRequest, "current request");
				ctx.Assert (PrimaryOperation.ServicePoint.CurrentConnections, Is.EqualTo (2), "ServicePoint.CurrentConnections");
				break;

			case HttpInstrumentationTestType.SimpleQueuedRequest:
			case HttpInstrumentationTestType.ThreeParallelRequests:
			case HttpInstrumentationTestType.ParallelRequestsSomeQueued:
			case HttpInstrumentationTestType.ManyParallelRequests:
			case HttpInstrumentationTestType.ManyParallelRequestsStress:
			case HttpInstrumentationTestType.CancelQueuedRequest:
			case HttpInstrumentationTestType.CancelMainWhileQueued:
			case HttpInstrumentationTestType.NtlmWhileQueued:
			case HttpInstrumentationTestType.NtlmWhileQueued2:
				ctx.Assert (PrimaryOperation.HasRequest, "current request");
				break;

			default:
				throw ctx.AssertFail (EffectiveType);
			}
		}

		class Operation : InstrumentationOperation
		{
			new public HttpInstrumentationTestRunner Parent => (HttpInstrumentationTestRunner)base.Parent;

			public HttpInstrumentationTestType EffectiveType => Parent.EffectiveType;

			public HttpInstrumentationHandler InstrumentationHandler => (HttpInstrumentationHandler)base.Handler;

			public Operation (HttpInstrumentationTestRunner parent, Handler handler,
					  InstrumentationOperationType type, HttpOperationFlags flags,
					  HttpStatusCode expectedStatus, WebExceptionStatus expectedError)
				: base (parent, $"{parent.EffectiveType}:{type}",
					handler, type, flags, expectedStatus, expectedError)
			{
			}

			protected override Request CreateRequest (TestContext ctx, Uri uri)
			{
				var primary = Type == InstrumentationOperationType.Primary;
				if (Handler is HttpInstrumentationHandler instrumentationHandler)
					return instrumentationHandler.CreateRequest (ctx, primary, uri);

				return new TraditionalRequest (uri);
			}

			protected override void ConfigureRequest (TestContext ctx, Uri uri, Request request)
			{
				var traditionalRequest = (TraditionalRequest)request;

				if (Type == InstrumentationOperationType.Primary)
					ConfigurePrimaryRequest (ctx, traditionalRequest);
				else
					ConfigureParallelRequest (ctx, traditionalRequest);

				Handler.ConfigureRequest (request, uri);

				request.SetProxy (Parent.Server.GetProxy ());
			}

			void ConfigureParallelRequest (TestContext ctx, TraditionalRequest request)
			{
				switch (EffectiveType) {
				case HttpInstrumentationTestType.ParallelRequests:
				case HttpInstrumentationTestType.SimpleQueuedRequest:
				case HttpInstrumentationTestType.CancelQueuedRequest:
				case HttpInstrumentationTestType.CancelMainWhileQueued:
				case HttpInstrumentationTestType.NtlmWhileQueued:
				case HttpInstrumentationTestType.NtlmWhileQueued2:
					ctx.Assert (ServicePoint, Is.Not.Null, "ServicePoint");
					ctx.Assert (ServicePoint.CurrentConnections, Is.EqualTo (1), "ServicePoint.CurrentConnections");
					break;
				case HttpInstrumentationTestType.ThreeParallelRequests:
				case HttpInstrumentationTestType.ParallelRequestsSomeQueued:
				case HttpInstrumentationTestType.ManyParallelRequests:
				case HttpInstrumentationTestType.ManyParallelRequestsStress:
				case HttpInstrumentationTestType.ReuseConnection:
				case HttpInstrumentationTestType.ReuseConnection2:
				case HttpInstrumentationTestType.ReuseAfterPartialRead:
				case HttpInstrumentationTestType.ParallelNtlm:
				case HttpInstrumentationTestType.CustomConnectionGroup:
					break;
				case HttpInstrumentationTestType.ReuseCustomConnectionGroup:
					request.RequestExt.ConnectionGroupName = "custom";
					break;
				default:
					throw ctx.AssertFail (Parent.EffectiveType);
				}
			}

			void ConfigurePrimaryRequest (TestContext ctx, TraditionalRequest request)
			{
				request.RequestExt.ReadWriteTimeout = int.MaxValue;
				request.RequestExt.Timeout = int.MaxValue;

				switch (EffectiveType) {
				case HttpInstrumentationTestType.CustomConnectionGroup:
				case HttpInstrumentationTestType.ReuseCustomConnectionGroup:
					request.RequestExt.ConnectionGroupName = "custom";
					break;
				case HttpInstrumentationTestType.CloseIdleConnection:
					ServicePoint.MaxIdleTime = IdleTime;
					break;
				case HttpInstrumentationTestType.SimpleQueuedRequest:
				case HttpInstrumentationTestType.CancelQueuedRequest:
					ServicePoint.ConnectionLimit = 1;
					break;
				case HttpInstrumentationTestType.CancelMainWhileQueued:
				case HttpInstrumentationTestType.NtlmWhileQueued:
					ServicePoint.ConnectionLimit = 1;
					break;
				case HttpInstrumentationTestType.NtlmWhileQueued2:
					ServicePoint.ConnectionLimit = 1;
					break;
				case HttpInstrumentationTestType.ThreeParallelRequests:
					ServicePoint.ConnectionLimit = 5;
					break;
				case HttpInstrumentationTestType.ParallelRequestsSomeQueued:
					ServicePoint.ConnectionLimit = 3;
					break;
				case HttpInstrumentationTestType.ManyParallelRequests:
					ServicePoint.ConnectionLimit = 5;
					break;
				case HttpInstrumentationTestType.ManyParallelRequestsStress:
					ServicePoint.ConnectionLimit = 25;
					break;
				}
			}

			protected override Task<Response> RunInner (TestContext ctx, Request request, CancellationToken cancellationToken)
			{
				ctx.LogDebug (2, $"{ME} RUN INNER");
				return ((TraditionalRequest)request).SendAsync (ctx, cancellationToken);
			}

			protected override void ConfigureNetworkStream (TestContext ctx, StreamInstrumentation instrumentation)
			{
				switch (EffectiveType) {
				case HttpInstrumentationTestType.CloseCustomConnectionGroup:
					instrumentation.IgnoreErrors = true;
					break;
				case HttpInstrumentationTestType.InvalidDataDuringHandshake:
				case HttpInstrumentationTestType.AbortDuringHandshake:
				case HttpInstrumentationTestType.SimpleQueuedRequest:
				case HttpInstrumentationTestType.CancelQueuedRequest:
				case HttpInstrumentationTestType.CancelMainWhileQueued:
				case HttpInstrumentationTestType.NtlmWhileQueued:
				case HttpInstrumentationTestType.NtlmWhileQueued2:
				case HttpInstrumentationTestType.ThreeParallelRequests:
				case HttpInstrumentationTestType.ParallelRequestsSomeQueued:
				case HttpInstrumentationTestType.ManyParallelRequests:
				case HttpInstrumentationTestType.ManyParallelRequestsStress:
				case HttpInstrumentationTestType.ParallelRequests:
					InstallReadHandler (ctx);
					break;
				}
			}

			protected override async Task ReadHandler (TestContext ctx, byte[] buffer, int offset, int size, int ret, CancellationToken cancellationToken)
			{
				if (EffectiveType == HttpInstrumentationTestType.InvalidDataDuringHandshake) {
					ctx.Assert (Type, Is.EqualTo (InstrumentationOperationType.Primary), "Primary request");
					InstallReadHandler (ctx);
					if (ret > 50) {
						for (int i = 10; i < 40; i++)
							buffer [i] = 0xAA;
					}
					return;
				}

				await base.ReadHandler (ctx, buffer, offset, size, ret, cancellationToken).ConfigureAwait (false);
			}
		}

		AuthenticationManager authManager;

		static bool ExpectWebException (TestContext ctx, Task task, WebExceptionStatus status, string message)
		{
			if (!ctx.Expect (task.Status, Is.EqualTo (TaskStatus.Faulted), message))
				return false;
			var error = TestContext.CleanupException (task.Exception);
			if (!ctx.Expect (error, Is.InstanceOf<WebException> (), message))
				return false;
			return ctx.Expect (((WebException)error).Status, Is.EqualTo (status), message);
		}
	}
}
