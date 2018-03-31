//
// NtlmInstrumentation.cs
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
using Xamarin.AsyncTests.Constraints;

namespace Xamarin.WebTests.HttpInstrumentationTests
{
	using ConnectionFramework;
	using TestFramework;
	using TestAttributes;
	using HttpFramework;
	using HttpHandlers;
	using TestRunners;
	using System.IO;

	[HttpServerTestCategory (HttpServerTestCategory.NewWebStackInstrumentation)]
	public class NtlmInstrumentation : HttpInstrumentationTestFixture
	{
		public sealed override HttpStatusCode ExpectedStatus {
			get;
		}

		public sealed override WebExceptionStatus ExpectedError {
			get;
		}

		public override HttpContent ExpectedContent => HttpContent.HelloWorld;

		public sealed override RequestFlags RequestFlags {
			get;
		}

		public NtlmType Type {
			get;
		}

		[AsyncTest]
		public NtlmInstrumentation (NtlmType type)
		{
			Type = type;

			switch (type) {
			case NtlmType.NtlmWhileQueued:
				ExpectedStatus = HttpStatusCode.InternalServerError;
				ExpectedError = WebExceptionStatus.RequestCanceled;
				RequestFlags = RequestFlags.KeepAlive;
				break;
			case NtlmType.NtlmWhileQueued2:
			case NtlmType.NtlmInstrumentation:
			case NtlmType.NtlmReusesConnection:
			case NtlmType.ParallelNtlm:
				ExpectedStatus = HttpStatusCode.OK;
				ExpectedError = WebExceptionStatus.Success;
				RequestFlags = RequestFlags.KeepAlive;
				break;
			case NtlmType.NtlmClosesConnection:
				ExpectedStatus = HttpStatusCode.OK;
				ExpectedError = WebExceptionStatus.Success;
				RequestFlags = RequestFlags.CloseConnection;
				break;
			}
		}

		public enum NtlmType {
			NtlmWhileQueued,
			NtlmWhileQueued2,
			NtlmInstrumentation,
			NtlmClosesConnection,
			NtlmReusesConnection,
			ParallelNtlm
		}

		ServicePoint servicePoint;
		AuthenticationManager authManager;
		AuthenticationState currentAuthState;
		TaskCompletionSource<bool> finishedTcs;
		InstrumentationOperation primaryOperation;
		TraditionalRequest currentRequest;
		IPEndPoint challengeEndPoint;

		protected override void ConfigurePrimaryRequest (
			TestContext ctx, InstrumentationOperation operation,
			TraditionalRequest request)
		{
			ctx.Assert (Interlocked.CompareExchange (ref primaryOperation, operation, null), Is.Null);
			ctx.Assert (Interlocked.CompareExchange (ref currentRequest, request, null), Is.Null);

			finishedTcs = new TaskCompletionSource<bool> ();
			authManager = new AuthenticationManager (AuthenticationType.NTLM, AuthenticationHandler.GetCredentials ());
			authManager.ConfigureRequest (request);

			ctx.Assert (servicePoint, Is.Null, "ServicePoint");
			servicePoint = ServicePointManager.FindServicePoint (request.Uri);

			switch (Type) {
			case NtlmType.NtlmWhileQueued:
			case NtlmType.NtlmWhileQueued2:
				servicePoint.ConnectionLimit = 1;
				break;
			}

			base.ConfigurePrimaryRequest (ctx, operation, request);
		}

		protected override void ConfigureSecondaryRequest (
			TestContext ctx, InstrumentationOperation operation,
			TraditionalRequest request)
		{
			switch (Type) {
			case NtlmType.NtlmWhileQueued:
			case NtlmType.NtlmWhileQueued2:
				ctx.Assert (servicePoint, Is.Not.Null, "ServicePoint");
				ctx.Assert (servicePoint.CurrentConnections, Is.EqualTo (1), "ServicePoint.CurrentConnections");
				break;
			case NtlmType.ParallelNtlm:
				break;
			default:
				throw ctx.AssertFail (Type);
			}
			base.ConfigureSecondaryRequest (ctx, operation, request);
		}

		protected override bool ConfigureNetworkStream (TestContext ctx, StreamInstrumentation instrumentation)
		{
			switch (Type) {
			case NtlmType.NtlmWhileQueued:
			case NtlmType.NtlmWhileQueued2:
				return true;
			case NtlmType.NtlmInstrumentation:
			case NtlmType.NtlmClosesConnection:
			case NtlmType.NtlmReusesConnection:
			case NtlmType.ParallelNtlm:
				return false;
			default:
				throw ctx.AssertFail (Type);
			}
		}

		protected override Task<bool> PrimaryReadHandler (
			TestContext ctx, InstrumentationOperation operation,
			byte[] buffer, int offset, int size, int bytesRead,
			CancellationToken cancellationToken)
		{
			if (QueuedOperation != null)
				return Task.FromResult (false);

			switch (Type) {
			case NtlmType.NtlmWhileQueued:
				StartQueuedOperation (
					HttpOperationFlags.DelayedListenerContext | HttpOperationFlags.ClientAbortsRequest,
					HttpStatusCode.OK, WebExceptionStatus.Success);
				break;
			case NtlmType.NtlmWhileQueued2:
				StartQueuedOperation (
					HttpOperationFlags.DelayedListenerContext,
					HttpStatusCode.OK, WebExceptionStatus.Success);
				break;
			default:
				throw ctx.AssertFail (Type);
			}

			return Task.FromResult (false);

			HttpOperation StartQueuedOperation (
				HttpOperationFlags flags,
				HttpStatusCode expectedStatus,
				WebExceptionStatus expectedError)
			{
				var queued = CreateOperation (
					ctx, InstrumentationOperationType.Queued,
					flags, expectedStatus, expectedError);
				return StartOperation (
					ctx, cancellationToken, queued);
			}
		}

		public override async Task<HttpResponse> HandleRequest (
			TestContext ctx, InstrumentationOperation operation,
			HttpConnection connection, HttpRequest request,
			RequestFlags effectiveFlags,
			CancellationToken cancellationToken)
		{
			var me = $"{ME}.{nameof (HandleRequest)}";
			ctx.LogDebug (3, $"{me}: {connection.RemoteEndPoint}");

			if (operation.Type != InstrumentationOperationType.Primary)
				return new HttpResponse (HttpStatusCode.OK, ExpectedContent);

			await FinishedTask.ConfigureAwait (false);

			AuthenticationState state;
			var response = authManager.HandleAuthentication (ctx, connection, request, out state);
			ctx.LogDebug (3, $"{me}: {connection.RemoteEndPoint} - {state} {response}");

			if (state == AuthenticationState.Unauthenticated) {
				ctx.Assert (currentAuthState, Is.EqualTo (AuthenticationState.None), "first request");
				currentAuthState = AuthenticationState.Unauthenticated;
			} else if (Type == NtlmType.NtlmInstrumentation) {
				if (state == AuthenticationState.Challenge) {
					ctx.LogDebug (3, $"{me}: {connection.RemoteEndPoint}");
					challengeEndPoint = connection.RemoteEndPoint;
				} else
					ctx.Assert (connection.RemoteEndPoint, Is.EqualTo (challengeEndPoint), "must reuse connection");
			}

			if (Type == NtlmType.ParallelNtlm) {
				var parallelOperation = StartParallelNtlm (
					ctx, operation, state, cancellationToken);
				if (parallelOperation != null)
					await parallelOperation.WaitForCompletion ().ConfigureAwait (false);
			}

			var keepAlive = (effectiveFlags & (RequestFlags.KeepAlive | RequestFlags.CloseConnection)) == RequestFlags.KeepAlive;
			if (response != null) {
				response.RegisterRedirect (ctx, operation, request.Path);
				return response;
			}

			switch (Type) {
			case NtlmType.NtlmInstrumentation:
			case NtlmType.NtlmClosesConnection:
			case NtlmType.NtlmReusesConnection:
			case NtlmType.ParallelNtlm:
				return new HttpResponse (HttpStatusCode.OK, ExpectedContent) {
					KeepAlive = false
				};
			case NtlmType.NtlmWhileQueued:
				return new HttpResponse (
					HttpStatusCode.OK, new MyContent (this));
			case NtlmType.NtlmWhileQueued2:
				return new HttpResponse (
					HttpStatusCode.OK, new MyContent (this)) {
					CloseConnection = true
				};
			default:
				throw ctx.AssertFail (Type);
			}
		}

		HttpOperation StartParallelNtlm (
			TestContext ctx, InstrumentationOperation operation,
			AuthenticationState state, CancellationToken cancellationToken)
		{
			ctx.LogDebug (2, $"{ME}.{nameof (StartParallelNtlm)}: {operation == primaryOperation} {state}");
			if (operation != primaryOperation || state != AuthenticationState.Challenge)
				return null;

			var flags = primaryOperation.Flags;

			return operation.Parent.StartOperation (
				ctx, cancellationToken,
				InstrumentationOperationType.Queued, flags);
		}

		protected override Task<TraditionalResponse> ReadResponse (
			TestContext ctx, TraditionalRequest request,
			HttpWebResponse response, WebException error,
			CancellationToken cancellationToken)
		{
			switch (Type) {
			case NtlmType.NtlmWhileQueued:
			case NtlmType.NtlmWhileQueued2:
				return ReadWithTimeout (
					ctx, request, response,
					0, WebExceptionStatus.RequestCanceled);
			case NtlmType.NtlmInstrumentation:
			case NtlmType.NtlmClosesConnection:
			case NtlmType.NtlmReusesConnection:
				return base.ReadResponse (
					ctx, request, response,
					error, cancellationToken);
			default:
				throw ctx.AssertFail (Type);
			}
		}

		public async Task<TraditionalResponse> ReadWithTimeout (
			TestContext ctx, TraditionalRequest request,
			WebResponse response, int timeout,
			WebExceptionStatus expectedStatus)
		{
			StreamReader reader = null;
			try {
				reader = new StreamReader (response.GetResponseStream ());
				var readTask = reader.ReadToEndAsync ();
				if (timeout > 0) {
					var timeoutTask = Task.Delay (timeout);
					var task = await Task.WhenAny (timeoutTask, readTask).ConfigureAwait (false);
					if (task == timeoutTask)
						throw ctx.AssertFail ("Timeout expired.");
				}
				var ret = await readTask.ConfigureAwait (false);
				ctx.LogMessage ($"EXPECTED ERROR: {ret}");
				throw ctx.AssertFail ("Expected exception.");
			} catch (WebException wexc) {
				ctx.Assert ((WebExceptionStatus)wexc.Status, Is.EqualTo (expectedStatus));
				return new TraditionalResponse (request, HttpStatusCode.InternalServerError, wexc);
			} finally {
				finishedTcs.TrySetResult (true);
			}
		}

		async Task HandleNtlmWhileQueued (
			TestContext ctx, CancellationToken cancellationToken)
		{
			/*
			 * HandleNtlmWhileQueued and HandleNtlmWhileQueued2 don't work on .NET because they
			 * don't do the "priority request" mechanic to send the NTLM challenge before processing
			 * any queued requests.
			 */

			/*
			 * This test is tricky.  We set ServicePoint.ConnectionLimit to 1, then start an NTLM
			 * request.  Using the instrumentation's read handler, we then start another simple
			 * "Hello World" GET request, which will then be queued by the ServicePoint.
			 * 
			 * Once we got to this point, the client did the full NTLM authentication and we are about
			 * to return the final response body.
			 * 
			 * Now we start listening for a new connection by calling StartDelayedListener().
			 * 
			 */
			await Task.Delay (500).ConfigureAwait (false);
			ctx.LogDebug (4, $"{ME} WRITE BODY - ABORT!");

			await StartDelayedSecondaryOperation (ctx);

			/*
			 * Then we abort the client-side NTLM request and wait for it to complete.
			 * This will eventually close the connection, so the ServicePoint scheduler will
			 * start the "Hello World" request.
			 */

			PrimaryOperation.Request.Abort ();
			await Task.WhenAny (finishedTcs.Task, Task.Delay (10000));
		}

		async Task HandleNtlmWhileQueued2 (
			TestContext ctx, Stream stream, CancellationToken cancellationToken)
		{
			/*
			 * Similar to NtlmWhileQueued, but we now complete both requests.
			 */
			await Task.Delay (500).ConfigureAwait (false);
			await StartDelayedSecondaryOperation (ctx);

			var message = ExpectedContent.AsString ();
			await stream.WriteAsync (message, cancellationToken).ConfigureAwait (false);
			await stream.FlushAsync (cancellationToken);
		}

		class MyContent : HttpContent
		{
			public NtlmInstrumentation Parent {
				get;
			}

			public MyContent (NtlmInstrumentation parent)
			{
				Parent = parent;

				switch (parent.Type) {
				case NtlmType.NtlmWhileQueued:
					Length = 4096;
					break;
				case NtlmType.NtlmWhileQueued2:
					Length = Parent.ExpectedContent.Length;
					break;
				}
			}

			public override bool HasLength => true;

			public sealed override int Length {
				get;
			}

			public override void AddHeadersTo (HttpMessage message)
			{
				message.ContentType = "text/plain";
				message.ContentLength = Length;
			}

			public override byte[] AsByteArray ()
			{
				throw new NotImplementedException ();
			}

			public override string AsString ()
			{
				throw new NotImplementedException ();
			}

			public override Task WriteToAsync (
				TestContext ctx, Stream stream,
				CancellationToken cancellationToken)
			{
				switch (Parent.Type) {
				case NtlmType.NtlmWhileQueued:
					return Parent.HandleNtlmWhileQueued (
						ctx, cancellationToken);
				case NtlmType.NtlmWhileQueued2:
					return Parent.HandleNtlmWhileQueued2 (
						ctx, stream, cancellationToken);
				default:
					throw ctx.AssertFail (Parent.Type);
				}
			}
		}
	}
}
