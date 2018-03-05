//
// HttpClientTestRunner.cs
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
	using HttpClient;

	[HttpClientTestRunner]
	public class HttpClientTestRunner : InstrumentationTestRunner
	{
		public HttpClientTestType Type {
			get;
		}

		public HttpClientTestType EffectiveType => GetEffectiveType (Type);

		static HttpClientTestType GetEffectiveType (HttpClientTestType type)
		{
			if (type == HttpClientTestType.MartinTest)
				return MartinTest;
			return type;
		}

		public HttpClientTestRunner (HttpServerProvider provider, HttpClientTestType type)
			: base (provider, type.ToString ())
		{
			Type = type;
		}

		const HttpClientTestType MartinTest = HttpClientTestType.ReuseHandlerGZip;

		static readonly (HttpClientTestType type, HttpClientTestFlags flags)[] TestRegistration = {
			(HttpClientTestType.Simple, HttpClientTestFlags.Working),
			(HttpClientTestType.GetString, HttpClientTestFlags.Working),
			(HttpClientTestType.PostString, HttpClientTestFlags.Working),
			(HttpClientTestType.PostStringWithResult, HttpClientTestFlags.Working),
			(HttpClientTestType.PutString, HttpClientTestFlags.Working),
			(HttpClientTestType.PutChunked, HttpClientTestFlags.Working),
			(HttpClientTestType.SendAsyncEmptyBody, HttpClientTestFlags.Working),
			(HttpClientTestType.SendAsyncGet, HttpClientTestFlags.Working),
			(HttpClientTestType.SendAsyncHead, HttpClientTestFlags.Working),
			(HttpClientTestType.SendLargeBlob, HttpClientTestFlags.Working),
			(HttpClientTestType.SendLargeBlobOddSize, HttpClientTestFlags.Working),
			(HttpClientTestType.ChunkSizeWithLeadingZero, HttpClientTestFlags.Working),
			(HttpClientTestType.PutRedirectEmptyBody, HttpClientTestFlags.Working),
			(HttpClientTestType.PutRedirect, HttpClientTestFlags.NewWebStack),
			(HttpClientTestType.PutRedirectKeepAlive, HttpClientTestFlags.NewWebStack),
			// Fixed in PR #6059 / #6068.
			(HttpClientTestType.SendAsyncObscureVerb, HttpClientTestFlags.WorkingMaster),
			(HttpClientTestType.GetError, HttpClientTestFlags.Working),

			(HttpClientTestType.ParallelRequests, HttpClientTestFlags.Instrumentation),
			(HttpClientTestType.SimpleQueuedRequest, HttpClientTestFlags.Instrumentation),
			(HttpClientTestType.SimpleGZip, HttpClientTestFlags.GZip),
			(HttpClientTestType.ParallelGZip, HttpClientTestFlags.GZip),
			(HttpClientTestType.SequentialRequests, HttpClientTestFlags.Working),
			(HttpClientTestType.SequentialChunked, HttpClientTestFlags.Working),
			(HttpClientTestType.SequentialGZip, HttpClientTestFlags.GZip),
			(HttpClientTestType.ParallelGZipNoClose, HttpClientTestFlags.GZip),

			(HttpClientTestType.ReuseHandler, HttpClientTestFlags.Working),
			(HttpClientTestType.ReuseHandlerNoClose, HttpClientTestFlags.Working),
			(HttpClientTestType.ReuseHandlerChunked, HttpClientTestFlags.Working),

			(HttpClientTestType.ReuseHandlerGZip, HttpClientTestFlags.Ignore),
		};

		public static IList<HttpClientTestType> GetTestTypes (TestContext ctx, HttpServerTestCategory category)
		{
			if (category == HttpServerTestCategory.MartinTest)
				return new[] { MartinTest };

			var setup = DependencyInjector.Get<IConnectionFrameworkSetup> ();
			return TestRegistration.Where (t => Filter (t.flags)).Select (t => t.type).ToList ();

			bool Filter (HttpClientTestFlags flags)
			{
				if (flags == HttpClientTestFlags.GZip) {
					if (!setup.SupportsGZip)
						return false;
					flags = HttpClientTestFlags.Instrumentation;
				}

				switch (category) {
				case HttpServerTestCategory.MartinTest:
					return false;
				case HttpServerTestCategory.Default:
					if (flags == HttpClientTestFlags.Working)
						return true;
					if (setup.UsingDotNet || setup.InternalVersion >= 1)
						return flags == HttpClientTestFlags.WorkingMaster;
					return false;
				case HttpServerTestCategory.Instrumentation:
					if (flags == HttpClientTestFlags.Instrumentation)
						return true;
					if (setup.UsingDotNet || setup.InternalVersion >= 1)
						return flags == HttpClientTestFlags.WorkingMaster;
					return false;
				case HttpServerTestCategory.NewWebStack:
					return flags == HttpClientTestFlags.NewWebStack;
				default:
					throw ctx.AssertFail (category);
				}
			}
		}

		protected override async Task RunSecondary (TestContext ctx, CancellationToken cancellationToken)
		{
			var me = $"{ME}.{nameof (RunSecondary)}()";

			Operation secondOperation = null;

			switch (EffectiveType) {
			case HttpClientTestType.ParallelRequests:
			case HttpClientTestType.ParallelGZip:
				ctx.Assert (ReadHandlerCalled, Is.EqualTo (2), "ReadHandler called twice");
				break;
			case HttpClientTestType.ParallelGZipNoClose:
				ctx.Assert (ReadHandlerCalled, Is.GreaterThanOrEqualTo (2), "ReadHandler called twice");
				break;
			case HttpClientTestType.SequentialRequests:
			case HttpClientTestType.SequentialChunked:
			case HttpClientTestType.SequentialGZip:
			case HttpClientTestType.ReuseHandler:
			case HttpClientTestType.ReuseHandlerNoClose:
			case HttpClientTestType.ReuseHandlerChunked:
			case HttpClientTestType.ReuseHandlerGZip:
				secondOperation = StartSequentialRequest ();
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

			Operation StartSequentialRequest ()
			{
				var (handler, flags) = CreateHandler (ctx, false);
				var operation = new Operation (
					this, handler, InstrumentationOperationType.Parallel, flags,
					HttpStatusCode.OK, WebExceptionStatus.Success);
				operation.Start (ctx, cancellationToken);
				return operation;
			}
		}

		protected override (Handler handler, HttpOperationFlags flags) CreateHandler (TestContext ctx, bool primary)
		{
			var identifier = EffectiveType.ToString ();
			var hello = new HelloWorldHandler (identifier);
			var helloKeepAlive = new HelloWorldHandler (identifier) {
				Flags = RequestFlags.KeepAlive
			};

			HttpOperationFlags flags = HttpOperationFlags.None;
			Handler handler;

			switch (EffectiveType) {
			case HttpClientTestType.Simple:
				handler = hello;
				break;
			case HttpClientTestType.GetString:
				handler = new GetHandler (identifier, HttpContent.HelloWorld);
				break;
			case HttpClientTestType.PostString:
				handler = new PostHandler (identifier, HttpContent.HelloWorld);
				break;
			case HttpClientTestType.PostStringWithResult:
				handler = new PostHandler (identifier, HttpContent.HelloWorld) {
					ReturnContent = HttpContent.ReturningWorld
				};
				break;
			case HttpClientTestType.PutString:
				handler = new PostHandler (identifier, HttpContent.HelloWorld) {
					Method = "PUT"
				};
				break;
			case HttpClientTestType.PutChunked:
				handler = new PostHandler (identifier, ConnectionHandler.GetLargeChunkedContent (50), TransferMode.Chunked) {
					Method = "PUT"
				};
				break;
			case HttpClientTestType.SendAsyncEmptyBody:
				handler = new PostHandler (identifier, null, TransferMode.ContentLength);
				break;
			case HttpClientTestType.SendAsyncObscureVerb:
				handler = new PostHandler (identifier, null, TransferMode.ContentLength) { Method = "EXECUTE" };
				break;
			case HttpClientTestType.SendAsyncGet:
				handler = new PostHandler (identifier, null) { Method = "GET" };
				break;
			case HttpClientTestType.SendAsyncHead:
				handler = new PostHandler (identifier, null) { Method = "HEAD" };
				break;
			case HttpClientTestType.SendLargeBlob:
				handler = new PostHandler (identifier, BinaryContent.CreateRandom (102400));
				break;
			case HttpClientTestType.SendLargeBlobOddSize:
				handler = new PostHandler (identifier, BinaryContent.CreateRandom (102431));
				break;
			case HttpClientTestType.ChunkSizeWithLeadingZero:
				handler = new PostHandler (identifier, HttpContent.HelloWorld) {
					ReturnContent = new Bug20583Content ()
				};
				break;
			case HttpClientTestType.PutRedirectEmptyBody:
				handler = new PostHandler (identifier, null, TransferMode.ContentLength) { Method = "PUT" };
				handler = new RedirectHandler (handler, HttpStatusCode.TemporaryRedirect);
				break;
			case HttpClientTestType.PutRedirect:
				handler = new PostHandler (identifier, HttpContent.HelloWorld, TransferMode.ContentLength) { Method = "PUT" };
				handler = new RedirectHandler (handler, HttpStatusCode.TemporaryRedirect);
				break;
			case HttpClientTestType.PutRedirectKeepAlive:
				handler = new PostHandler (identifier, HttpContent.HelloWorld, TransferMode.ContentLength) { Method = "PUT" };
				handler = new RedirectHandler (handler, HttpStatusCode.TemporaryRedirect) {
					Flags = RequestFlags.KeepAlive
				};
				break;
			case HttpClientTestType.RedirectCustomContent:
				handler = new PostHandler (identifier, new CustomContent (this, ctx), TransferMode.ContentLength);
				handler = new RedirectHandler (handler, HttpStatusCode.TemporaryRedirect) {
					Flags = RequestFlags.KeepAlive
				};
				break;
			case HttpClientTestType.GetError:
				handler = new GetHandler (identifier, HttpContent.HelloWorld, HttpStatusCode.InternalServerError);
				break;
			case HttpClientTestType.ParallelRequests:
			case HttpClientTestType.SimpleQueuedRequest:
				return (hello, flags);
			case HttpClientTestType.SimpleGZip:
			case HttpClientTestType.ParallelGZip:
			case HttpClientTestType.ParallelGZipNoClose:
			case HttpClientTestType.SequentialRequests:
			case HttpClientTestType.SequentialChunked:
			case HttpClientTestType.SequentialGZip:
				handler = new HttpClientInstrumentationHandler (this, true);
				flags = HttpOperationFlags.DontReuseConnection;
				break;
			case HttpClientTestType.ReuseHandler:
			case HttpClientTestType.ReuseHandlerNoClose:
			case HttpClientTestType.ReuseHandlerChunked:
			case HttpClientTestType.ReuseHandlerGZip:
				handler = new HttpClientInstrumentationHandler (this, !primary);
				break;
			default:
				throw ctx.AssertFail (EffectiveType);
			}

			return (handler, flags);
		}

		void HandleRequest (
			TestContext ctx, HttpClientInstrumentationHandler handler,
			HttpConnection connection, HttpRequest request)
		{
			switch (EffectiveType) {
			case HttpClientTestType.SequentialRequests:
			case HttpClientTestType.SequentialChunked:
			case HttpClientTestType.SequentialGZip:
			case HttpClientTestType.ParallelGZip:
			case HttpClientTestType.ParallelGZipNoClose:
				MustNotReuseConnection ();
				break;
			case HttpClientTestType.ReuseHandler:
			case HttpClientTestType.ReuseHandlerNoClose:
			case HttpClientTestType.ReuseHandlerChunked:
			case HttpClientTestType.ReuseHandlerGZip:
				MustReuseConnection ();
				break;
			case HttpClientTestType.SimpleGZip:
				break;
			default:
				throw ctx.AssertFail (EffectiveType);
			}

			void MustNotReuseConnection ()
			{
				var firstHandler = (HttpClientInstrumentationHandler)PrimaryOperation.Handler;
				ctx.LogDebug (2, $"{handler.ME}: {handler == firstHandler} {handler.RemoteEndPoint}");
				if (handler == firstHandler)
					return;
				ctx.Assert (connection.RemoteEndPoint, Is.Not.EqualTo (firstHandler.RemoteEndPoint), "RemoteEndPoint");
			}

			void MustReuseConnection ()
			{
				var firstHandler = (HttpClientInstrumentationHandler)PrimaryOperation.Handler;
				ctx.LogDebug (2, $"{handler.ME}: {handler == firstHandler} {handler.RemoteEndPoint}");
				if (handler == firstHandler)
					return;
				ctx.Assert (connection.RemoteEndPoint, Is.EqualTo (firstHandler.RemoteEndPoint), "RemoteEndPoint");
			}
		}

		protected override async Task PrimaryReadHandler (TestContext ctx, CancellationToken cancellationToken)
		{
			switch (EffectiveType) {
			case HttpClientTestType.ParallelRequests:
			case HttpClientTestType.SimpleQueuedRequest:
				ctx.Assert (PrimaryOperation.HasRequest, "current request");
				await RunSimpleHello ().ConfigureAwait (false);
				break;

			case HttpClientTestType.ParallelGZip:
			case HttpClientTestType.ParallelGZipNoClose:
				ctx.Assert (PrimaryOperation.HasRequest, "current request");
				await RunParallelGZip ().ConfigureAwait (false);
				break;

			default:
				throw ctx.AssertFail (EffectiveType);
			}

			Task RunSimpleHello ()
			{
				return StartOperation (
					ctx, cancellationToken, HelloWorldHandler.GetSimple (),
					InstrumentationOperationType.Parallel, HttpOperationFlags.None).WaitForCompletion ();
			}

			Task RunParallelGZip ()
			{
				return StartOperation (
					ctx, cancellationToken, new HttpClientInstrumentationHandler (this, true),
					InstrumentationOperationType.Parallel, HttpOperationFlags.None).WaitForCompletion ();
			}
		}

		protected override async Task SecondaryReadHandler (TestContext ctx, CancellationToken cancellationToken)
		{
			switch (EffectiveType) {
			case HttpClientTestType.ParallelRequests:
			case HttpClientTestType.ParallelGZip:
				ctx.Assert (PrimaryOperation.HasRequest, "current request");
				break;

			case HttpClientTestType.SimpleQueuedRequest:
				ctx.Assert (PrimaryOperation.HasRequest, "current request");
				ctx.Assert (PrimaryOperation.ServicePoint.CurrentConnections, Is.EqualTo (2), "ServicePoint.CurrentConnections");
				break;

			case HttpClientTestType.ParallelGZipNoClose:
				ctx.Assert (PrimaryOperation.HasRequest, "current request");
				if (ReadHandlerCalled == 2)
					await RunParallelGZip ().ConfigureAwait (false);
				break;

			default:
				throw ctx.AssertFail (EffectiveType);
			}

			Task RunParallelGZip ()
			{
				return StartOperation (
					ctx, cancellationToken, new HttpClientInstrumentationHandler (this, true),
					InstrumentationOperationType.Parallel, HttpOperationFlags.None).WaitForCompletion ();
			}
		}

		protected override InstrumentationOperation CreateOperation (
			TestContext ctx, Handler handler, InstrumentationOperationType type,
			HttpOperationFlags flags)
		{
			HttpStatusCode expectedStatus;
			WebExceptionStatus expectedError;

			switch (EffectiveType) {
			case HttpClientTestType.GetError:
				expectedError = WebExceptionStatus.Success;
				expectedStatus = HttpStatusCode.InternalServerError;
				break;
			default:
				expectedError = WebExceptionStatus.Success;
				expectedStatus = HttpStatusCode.OK;
				break;
			}

			return new Operation (this, handler, type, flags, expectedStatus, expectedError);
		}

		class Operation : InstrumentationOperation
		{
			new public HttpClientTestRunner Parent => (HttpClientTestRunner)base.Parent;

			public Operation (HttpClientTestRunner parent, Handler handler,
					  InstrumentationOperationType type, HttpOperationFlags flags,
					  HttpStatusCode expectedStatus, WebExceptionStatus expectedError)
				: base (parent, $"{parent.EffectiveType}",
					handler, type, flags, expectedStatus, expectedError)
			{
			}

			bool ReuseHandler {
				get {
					switch (Parent.EffectiveType) {
					case HttpClientTestType.ReuseHandler:
					case HttpClientTestType.ReuseHandlerNoClose:
					case HttpClientTestType.ReuseHandlerChunked:
					case HttpClientTestType.ReuseHandlerGZip:
						return true;
					default:
						return false;
					}
				}
			}

			protected override Request CreateRequest (TestContext ctx, Uri uri)
			{
				var instrumentationHandler = Handler as HttpClientInstrumentationHandler;
				if (instrumentationHandler == null)
					return new HttpClientRequest (uri);

				if (Type == InstrumentationOperationType.Primary || !ReuseHandler)
					return new HttpClientInstrumentationRequest (
						this, instrumentationHandler, uri);

				var primaryRequest = (HttpClientInstrumentationRequest)Parent.PrimaryOperation.Request;
				return new HttpClientInstrumentationRequest (
					this, instrumentationHandler, primaryRequest, uri);
			}

			protected override void ConfigureRequest (TestContext ctx, Uri uri, Request request)
			{
				if (Type == InstrumentationOperationType.Primary) {
					switch (Parent.EffectiveType) {
					case HttpClientTestType.SimpleQueuedRequest:
					case HttpClientTestType.ParallelGZip:
					case HttpClientTestType.ParallelGZipNoClose:
					case HttpClientTestType.SequentialRequests:
					case HttpClientTestType.SequentialChunked:
					case HttpClientTestType.SequentialGZip:
					case HttpClientTestType.ReuseHandler:
					case HttpClientTestType.ReuseHandlerNoClose:
					case HttpClientTestType.ReuseHandlerChunked:
					case HttpClientTestType.ReuseHandlerGZip:
						ServicePoint = ServicePointManager.FindServicePoint (uri);
						ServicePoint.ConnectionLimit = 1;
						break;
					}
				}

				if (request is HttpClientInstrumentationRequest instrumentationRequest) {
					instrumentationRequest.ConfigureRequest (ctx, uri);
					return;
				}

				if (request is HttpClientRequest httpClientRequest) {
					if (Type == InstrumentationOperationType.Primary)
						ConfigurePrimaryRequest (ctx, httpClientRequest);
					else
						ConfigureParallelRequest (ctx, httpClientRequest);
				}

				Handler.ConfigureRequest (request, uri);

				request.SetProxy (Parent.Server.GetProxy ());
			}

			void ConfigurePrimaryRequest (TestContext ctx, HttpClientRequest request)
			{
				switch (Parent.EffectiveType) {
				case HttpClientTestType.Simple:
				case HttpClientTestType.GetString:
				case HttpClientTestType.SendAsyncEmptyBody:
				case HttpClientTestType.SendAsyncObscureVerb:
				case HttpClientTestType.SendAsyncGet:
				case HttpClientTestType.SendAsyncHead:
				case HttpClientTestType.PutRedirectEmptyBody:
				case HttpClientTestType.PutRedirect:
				case HttpClientTestType.PutRedirectKeepAlive:
					break;
				case HttpClientTestType.PostString:
				case HttpClientTestType.PostStringWithResult:
				case HttpClientTestType.PutString:
				case HttpClientTestType.SendLargeBlob:
				case HttpClientTestType.SendLargeBlobOddSize:
				case HttpClientTestType.ChunkSizeWithLeadingZero:
					request.Content = ((PostHandler)Handler).Content;
					break;
				case HttpClientTestType.PutChunked:
					request.Content = ((PostHandler)Handler).Content.RemoveTransferEncoding ();
					request.SendChunked ();
					break;
				case HttpClientTestType.RedirectCustomContent:
				case HttpClientTestType.GetError:
					break;
				case HttpClientTestType.ParallelRequests:
				case HttpClientTestType.SimpleQueuedRequest:
					break;
				default:
					throw ctx.AssertFail (Parent.EffectiveType);
				}
			}

			void ConfigureParallelRequest (TestContext ctx, HttpClientRequest request)
			{
				switch (Parent.EffectiveType) {
				case HttpClientTestType.ParallelRequests:
				case HttpClientTestType.SimpleQueuedRequest:
					break;
				default:
					throw ctx.AssertFail (Parent.EffectiveType);
				}
			}

			protected override async Task<Response> RunInner (TestContext ctx, Request request, CancellationToken cancellationToken)
			{
				ctx.LogDebug (2, $"{ME} RUN INNER");

				if (request is HttpClientInstrumentationRequest instrumentationRequest) {
					using (var cts = CancellationTokenSource.CreateLinkedTokenSource (cancellationToken)) {
						cts.Token.Register (() => instrumentationRequest.Abort ());
						return await instrumentationRequest.SendAsync (ctx, cts.Token).ConfigureAwait (false);
					}
				}

				var httpClientRequest = (HttpClientRequest)request;
				return await Run (ctx, httpClientRequest, cancellationToken).ConfigureAwait (false);
			}

			Task<Response> Run (TestContext ctx, HttpClientRequest request, CancellationToken cancellationToken)
			{
				switch (Parent.EffectiveType) {
				case HttpClientTestType.Simple:
				case HttpClientTestType.GetString:
					return request.GetString (ctx, cancellationToken);
				case HttpClientTestType.PostString:
				case HttpClientTestType.PostStringWithResult:
				case HttpClientTestType.ChunkSizeWithLeadingZero:
					return request.PostString (ctx, cancellationToken);
				case HttpClientTestType.PutString:
				case HttpClientTestType.PutRedirectEmptyBody:
				case HttpClientTestType.PutRedirect:
				case HttpClientTestType.PutRedirectKeepAlive:
					return request.PutString (ctx, cancellationToken);
				case HttpClientTestType.PutChunked:
				case HttpClientTestType.SendAsyncEmptyBody:
				case HttpClientTestType.SendAsyncObscureVerb:
				case HttpClientTestType.SendAsyncGet:
				case HttpClientTestType.SendAsyncHead:
				case HttpClientTestType.GetError:
					return request.SendAsync (ctx, cancellationToken);
				case HttpClientTestType.SendLargeBlob:
				case HttpClientTestType.SendLargeBlobOddSize:
					return request.PutDataAsync (ctx, cancellationToken);
				case HttpClientTestType.RedirectCustomContent:
					return request.PostString (ctx, cancellationToken);
				case HttpClientTestType.ParallelRequests:
				case HttpClientTestType.SimpleQueuedRequest:
					return request.GetString (ctx, cancellationToken);
				default:
					throw ctx.AssertFail (Parent.EffectiveType);
				}
			}

			protected override void ConfigureNetworkStream (TestContext ctx, StreamInstrumentation instrumentation)
			{
				switch (Parent.EffectiveType) {
				case HttpClientTestType.ParallelRequests:
				case HttpClientTestType.SimpleQueuedRequest:
				case HttpClientTestType.ParallelGZip:
				case HttpClientTestType.ParallelGZipNoClose:
					InstallReadHandler (ctx);
					break;
				}
			}

			protected override void Destroy ()
			{
				;
			}
		}

		class HttpClientInstrumentationRequest : Request
		{
			public Operation Operation {
				get;
			}

			public HttpClientInstrumentationHandler Parent {
				get;
			}

			public Uri RequestUri {
				get;
			}

			protected IHttpClientProvider Provider {
				get;
			}

			protected IHttpClient Client {
				get;
			}

			protected IHttpClientHandler Handler {
				get;
			}

			public HttpClientInstrumentationRequest (
				Operation operation,
				HttpClientInstrumentationHandler handler,
				Uri requestUri)
			{
				Operation = operation;
				Parent = handler;
				RequestUri = requestUri;

				Provider = DependencyInjector.Get<IHttpClientProvider> ();
				Handler = Provider.Create ();
				Client = Handler.CreateHttpClient ();
			}

			public HttpClientInstrumentationRequest (
				Operation operation,
				HttpClientInstrumentationHandler handler,
				HttpClientInstrumentationRequest primaryRequest,
				Uri requestUri)
			{
				Operation = operation;
				Parent = handler;
				RequestUri = requestUri;

				Handler = primaryRequest.Handler;
				Client = primaryRequest.Client;
			}

			public override string Method {
				get => throw new NotSupportedException ();
				set => throw new NotSupportedException ();
			}

			public override void Abort ()
			{
				Client.CancelPendingRequests ();
			}

			public void ConfigureRequest (TestContext ctx, Uri uri)
			{
				switch (Parent.TestRunner.EffectiveType) {
				case HttpClientTestType.SimpleGZip:
				case HttpClientTestType.ParallelGZip:
				case HttpClientTestType.ParallelGZipNoClose:
				case HttpClientTestType.SequentialGZip:
					Handler.AutomaticDecompression = true;
					break;
				case HttpClientTestType.ReuseHandlerGZip:
					if (Operation.Type == InstrumentationOperationType.Primary)
						Handler.AutomaticDecompression = true;
					break;
				case HttpClientTestType.SequentialRequests:
				case HttpClientTestType.SequentialChunked:
				case HttpClientTestType.ReuseHandler:
				case HttpClientTestType.ReuseHandlerNoClose:
				case HttpClientTestType.ReuseHandlerChunked:
					break;
				default:
					throw ctx.AssertFail (Parent.TestRunner.EffectiveType);
				}
			}

			public override Task<Response> SendAsync (TestContext ctx, CancellationToken cancellationToken)
			{
				switch (Parent.TestRunner.EffectiveType) {
				case HttpClientTestType.SimpleGZip:
				case HttpClientTestType.ParallelGZip:
				case HttpClientTestType.ReuseHandler:
					return GetString (ctx, cancellationToken);
				case HttpClientTestType.ParallelGZipNoClose:
				case HttpClientTestType.SequentialRequests:
				case HttpClientTestType.SequentialChunked:
				case HttpClientTestType.SequentialGZip:
				case HttpClientTestType.ReuseHandlerNoClose:
				case HttpClientTestType.ReuseHandlerChunked:
				case HttpClientTestType.ReuseHandlerGZip:
					return GetStringNoClose (ctx, cancellationToken);
				default:
					throw ctx.AssertFail (Parent.TestRunner.EffectiveType);
				}
			}

			public override void SendChunked ()
			{
				throw new NotSupportedException ();
			}

			public override void SetContentLength (long contentLength)
			{
				throw new NotSupportedException ();
			}

			public override void SetContentType (string contentType)
			{
				throw new NotSupportedException ();
			}

			public override void SetCredentials (ICredentials credentials)
			{
				throw new NotSupportedException ();
			}

			public override void SetProxy (IWebProxy proxy)
			{
				throw new NotSupportedException ();
			}

			async Task<Response> GetString (TestContext ctx, CancellationToken cancellationToken)
			{
				try {
					var body = await Client.GetStringAsync (RequestUri);
					return new SimpleResponse (this, HttpStatusCode.OK, StringContent.CreateMaybeNull (body));
				} catch (Exception ex) {
					return new SimpleResponse (this, HttpStatusCode.InternalServerError, null, ex);
				}
			}

			async Task<Response> GetStringNoClose (TestContext ctx, CancellationToken cancellationToken)
			{
				var method = Handler.CreateRequestMessage (HttpMethod.Get, RequestUri);
				method.SetKeepAlive ();
				var response = await Client.SendAsync (
					method, HttpCompletionOption.ResponseContentRead, cancellationToken).ConfigureAwait (false);
				ctx.Assert (response.IsSuccessStatusCode, "success");
				var content = await response.Content.ReadAsStringAsync ();
				return new SimpleResponse (this, HttpStatusCode.OK, StringContent.CreateMaybeNull (content));
			}
		}

		class Bug20583Content : HttpContent
		{
#region implemented abstract members of HttpContent
			public override bool HasLength => false;
			public override int Length => throw new InvalidOperationException ();
			public override string AsString ()
			{
				return "AAAA";
			}
			public override byte[] AsByteArray ()
			{
				throw new NotSupportedException ();
			}
			public override void AddHeadersTo (HttpMessage message)
			{
				message.TransferEncoding = "chunked";
				message.ContentType = "text/plain";
			}
			public override async Task WriteToAsync (TestContext ctx, Stream stream, CancellationToken cancellationToken)
			{
				await Task.Delay (500).ConfigureAwait (false);
				await stream.WriteAsync ("0");
				await Task.Delay (500);
				await stream.WriteAsync ("4\r\n");
				await stream.WriteAsync ("AAAA\r\n0\r\n\r\n");
			}
#endregion
		}

		class CustomContent : HttpContent, ICustomHttpContent
		{
			public HttpClientTestRunner Parent {
				get;
			}

			public TestContext Context {
				get;
			}

			public string ME {
				get;
			}

			public CustomContent (HttpClientTestRunner parent, TestContext ctx)
			{
				Parent = parent;
				Context = ctx;
				ME = $"{parent.ME} - CUSTOM CONTENT";
			}

			HttpContent ICustomHttpContent.Content => this;

			public override bool HasLength => true;
			public override int Length => AsByteArray ().Length;

			public override string AsString () => ConnectionHandler.TheQuickBrownFox;

			public override byte[] AsByteArray () => ConnectionHandler.TheQuickBrownFoxBuffer;

			public override void AddHeadersTo (HttpMessage message)
			{
			}

			public override async Task WriteToAsync (TestContext ctx, Stream stream, CancellationToken cancellationToken)
			{
				await stream.WriteAsync (AsString (), cancellationToken).ConfigureAwait (false);
			}

			public override HttpContent RemoveTransferEncoding ()
			{
				return this;
			}

			public Task<Stream> CreateContentReadStreamAsync ()
			{
				throw new NotImplementedException ();
			}

			bool first = true;

			public async Task SerializeToStreamAsync (Stream stream)
			{
				byte[] buffer;
				if (first) {
					buffer = AsByteArray ();
					first = false;
				} else {
					buffer = ConnectionHandler.GetTextBuffer (Parent.ME);
				}

				Context.LogDebug (5, $"{ME}: STSA");
				await stream.WriteAsync (buffer, 0, buffer.Length).ConfigureAwait (false);
			}

			public bool TryComputeLength (out long length)
			{
				Context.LogDebug (5, $"{ME}: TCL");
				if (HasLength) {
					length = Length;
					return true;
				}

				length = -1;
				return false;
			}
		}

		class HttpClientInstrumentationHandler : Handler
		{
			public HttpClientTestRunner TestRunner {
				get;
			}

			public string ME {
				get;
			}

			public bool CloseConnection {
				get;
			}

			public IPEndPoint RemoteEndPoint {
				get;
				private set;
			}

			public HttpClientInstrumentationHandler (HttpClientTestRunner parent, bool closeConnection)

				: base (parent.EffectiveType.ToString ())
			{
				TestRunner = parent;
				ME = $"{GetType ().Name}({parent.EffectiveType})";
				CloseConnection = closeConnection;

				Flags = RequestFlags.KeepAlive;
				if (CloseConnection)
					Flags |= RequestFlags.CloseConnection;
			}

			HttpClientInstrumentationHandler (HttpClientInstrumentationHandler other)
				: base (other.Value)
			{
				TestRunner = other.TestRunner;
				ME = other.ME;
				CloseConnection = other.CloseConnection;
				Flags = other.Flags;
			}

			public override object Clone ()
			{
				return new HttpClientInstrumentationHandler (this);
			}

			protected internal override async Task<HttpResponse> HandleRequest (
				TestContext ctx, HttpOperation operation, HttpConnection connection, HttpRequest request,
				RequestFlags effectiveFlags, CancellationToken cancellationToken)
			{
				await FinishedTask.ConfigureAwait (false);

				RemoteEndPoint = connection.RemoteEndPoint;

				TestRunner.HandleRequest (ctx, this, connection, request);

				HttpContent content;
				switch (TestRunner.EffectiveType) {
				case HttpClientTestType.SimpleGZip:
				case HttpClientTestType.ParallelGZip:
				case HttpClientTestType.ParallelGZipNoClose:
				case HttpClientTestType.SequentialGZip:
				case HttpClientTestType.ReuseHandlerGZip:
					content = new GZipContent (ConnectionHandler.TheQuickBrownFoxBuffer);
					break;

				case HttpClientTestType.SequentialRequests:
				case HttpClientTestType.ReuseHandler:
				case HttpClientTestType.ReuseHandlerNoClose:
					content = HttpContent.TheQuickBrownFox;
					break;

				case HttpClientTestType.ReuseHandlerChunked:
				case HttpClientTestType.SequentialChunked:
					content = new ChunkedContent (ConnectionHandler.TheQuickBrownFox);
					break;

				default:
					throw ctx.AssertFail (TestRunner.EffectiveType);
				}

				return new HttpResponse (HttpStatusCode.OK, content);
			}

			public override bool CheckResponse (TestContext ctx, Response response)
			{
				HttpContent expectedContent;
				switch (TestRunner.EffectiveType) {
				case HttpClientTestType.SimpleGZip:
				case HttpClientTestType.ParallelGZip:
				case HttpClientTestType.ParallelGZipNoClose:
				case HttpClientTestType.SequentialRequests:
				case HttpClientTestType.SequentialChunked:
				case HttpClientTestType.SequentialGZip:
				case HttpClientTestType.ReuseHandler:
				case HttpClientTestType.ReuseHandlerNoClose:
				case HttpClientTestType.ReuseHandlerChunked:
				case HttpClientTestType.ReuseHandlerGZip:
					expectedContent = HttpContent.TheQuickBrownFox;
					break;
				default:
					expectedContent = new StringContent (ME);
					break;
				}

				if (!ctx.Expect (response.Content, Is.Not.Null, "response.Content != null"))
					return false;

				return HttpContent.Compare (ctx, response.Content, expectedContent, false, "response.Content");
			}
		}
	}
}
