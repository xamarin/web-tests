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
	using Resources;
	using Xamarin.WebTests.Server;

	[HttpInstrumentationTestRunner]
	public class HttpInstrumentationTestRunner : InstrumentationTestRunner
	{
		new public HttpInstrumentationTestParameters Parameters {
			get { return (HttpInstrumentationTestParameters)base.Parameters; }
		}

		public HttpInstrumentationTestType EffectiveType => GetEffectiveType (Parameters.Type);

		static HttpInstrumentationTestType GetEffectiveType (HttpInstrumentationTestType type)
		{
			if (type == HttpInstrumentationTestType.MartinTest)
				return MartinTest;
			return type;
		}

		public sealed override string ME {
			get;
		}

		public HttpInstrumentationTestRunner (IPortableEndPoint endpoint, HttpInstrumentationTestParameters parameters,
						      ConnectionTestProvider provider, Uri uri, HttpServerFlags flags)
			: base (endpoint, parameters, provider, uri, flags)
		{
			ME = $"{GetType ().Name}({EffectiveType})";
		}

		const HttpInstrumentationTestType MartinTest = HttpInstrumentationTestType.ResponseStreamCheckLength;

		static readonly (HttpInstrumentationTestType type, HttpInstrumentationTestFlags flags) [] TestRegistration = {
			(HttpInstrumentationTestType.Simple, HttpInstrumentationTestFlags.Working),
			(HttpInstrumentationTestType.InvalidDataDuringHandshake, HttpInstrumentationTestFlags.WorkingRequireSSL),
			(HttpInstrumentationTestType.AbortDuringHandshake, HttpInstrumentationTestFlags.WorkingRequireSSL),
			(HttpInstrumentationTestType.ParallelRequests, HttpInstrumentationTestFlags.Working),
			(HttpInstrumentationTestType.ThreeParallelRequests, HttpInstrumentationTestFlags.Stress),
			(HttpInstrumentationTestType.ParallelRequestsSomeQueued, HttpInstrumentationTestFlags.Stress),
			(HttpInstrumentationTestType.ManyParallelRequests, HttpInstrumentationTestFlags.Stress),
			(HttpInstrumentationTestType.ManyParallelRequestsStress, HttpInstrumentationTestFlags.Stress),
			(HttpInstrumentationTestType.SimpleQueuedRequest, HttpInstrumentationTestFlags.Working),
			(HttpInstrumentationTestType.CancelQueuedRequest, HttpInstrumentationTestFlags.Working),
			(HttpInstrumentationTestType.CancelMainWhileQueued, HttpInstrumentationTestFlags.WorkingRequireSSL),
			(HttpInstrumentationTestType.SimpleNtlm, HttpInstrumentationTestFlags.Working),
			(HttpInstrumentationTestType.NtlmWhileQueued, HttpInstrumentationTestFlags.NewWebStackRequireSSL),
			(HttpInstrumentationTestType.NtlmWhileQueued2, HttpInstrumentationTestFlags.NewWebStackRequireSSL),
			(HttpInstrumentationTestType.ReuseConnection, HttpInstrumentationTestFlags.Working),
			(HttpInstrumentationTestType.SimplePost, HttpInstrumentationTestFlags.Working),
			(HttpInstrumentationTestType.SimpleRedirect, HttpInstrumentationTestFlags.Working),
			(HttpInstrumentationTestType.PostRedirect, HttpInstrumentationTestFlags.Working),
			(HttpInstrumentationTestType.PostNtlm, HttpInstrumentationTestFlags.Working),
			(HttpInstrumentationTestType.NtlmChunked, HttpInstrumentationTestFlags.Working),
			(HttpInstrumentationTestType.ReuseConnection2, HttpInstrumentationTestFlags.Working),
			(HttpInstrumentationTestType.Get404, HttpInstrumentationTestFlags.Working),
			(HttpInstrumentationTestType.CloseIdleConnection, HttpInstrumentationTestFlags.NewWebStack),
			(HttpInstrumentationTestType.NtlmInstrumentation, HttpInstrumentationTestFlags.NewWebStack),
			(HttpInstrumentationTestType.NtlmClosesConnection, HttpInstrumentationTestFlags.NewWebStack),
			(HttpInstrumentationTestType.NtlmReusesConnection, HttpInstrumentationTestFlags.NewWebStack),
			(HttpInstrumentationTestType.ParallelNtlm, HttpInstrumentationTestFlags.NewWebStack),
			(HttpInstrumentationTestType.LargeHeader, HttpInstrumentationTestFlags.Working),
			(HttpInstrumentationTestType.LargeHeader2, HttpInstrumentationTestFlags.Working),
			(HttpInstrumentationTestType.SendResponseAsBlob, HttpInstrumentationTestFlags.Working),
			(HttpInstrumentationTestType.ReuseAfterPartialRead, HttpInstrumentationTestFlags.WorkingRequireSSL),
			(HttpInstrumentationTestType.CustomConnectionGroup, HttpInstrumentationTestFlags.Working),
			(HttpInstrumentationTestType.ReuseCustomConnectionGroup, HttpInstrumentationTestFlags.Working),
			(HttpInstrumentationTestType.CloseCustomConnectionGroup, HttpInstrumentationTestFlags.Working),
			(HttpInstrumentationTestType.CloseRequestStream, HttpInstrumentationTestFlags.Working),
			(HttpInstrumentationTestType.ReadTimeout, HttpInstrumentationTestFlags.NewWebStack),
			(HttpInstrumentationTestType.AbortResponse, HttpInstrumentationTestFlags.Working),
			(HttpInstrumentationTestType.RedirectOnSameConnection, HttpInstrumentationTestFlags.Working),
			(HttpInstrumentationTestType.RedirectNoReuse, HttpInstrumentationTestFlags.Working),
			(HttpInstrumentationTestType.RedirectNoLength, HttpInstrumentationTestFlags.NewWebStack),
			(HttpInstrumentationTestType.PutChunked, HttpInstrumentationTestFlags.Working),
			(HttpInstrumentationTestType.PutChunkDontCloseRequest, HttpInstrumentationTestFlags.NewWebStack),
			(HttpInstrumentationTestType.ServerAbortsRedirect, HttpInstrumentationTestFlags.Unstable),
			(HttpInstrumentationTestType.ServerAbortsPost, HttpInstrumentationTestFlags.NewWebStack),
			(HttpInstrumentationTestType.PostChunked, HttpInstrumentationTestFlags.Working),
			(HttpInstrumentationTestType.EntityTooBig, HttpInstrumentationTestFlags.NewWebStack),
			(HttpInstrumentationTestType.PostContentLength, HttpInstrumentationTestFlags.Working),
			(HttpInstrumentationTestType.ClientAbortsPost, HttpInstrumentationTestFlags.NewWebStack),
			(HttpInstrumentationTestType.GetChunked, HttpInstrumentationTestFlags.Working),
			(HttpInstrumentationTestType.SimpleGZip, HttpInstrumentationTestFlags.Working),
			(HttpInstrumentationTestType.TestResponseStream, HttpInstrumentationTestFlags.Working),
			(HttpInstrumentationTestType.LargeChunkRead, HttpInstrumentationTestFlags.Working),
			(HttpInstrumentationTestType.LargeGZipRead, HttpInstrumentationTestFlags.GZip),
			(HttpInstrumentationTestType.GZipWithLength, HttpInstrumentationTestFlags.GZip),
			(HttpInstrumentationTestType.ResponseStreamCheckLength2, HttpInstrumentationTestFlags.GZip),
			(HttpInstrumentationTestType.ResponseStreamCheckLength, HttpInstrumentationTestFlags.GZip),
		};

		public static IList<HttpInstrumentationTestType> GetInstrumentationTypes (TestContext ctx, ConnectionTestCategory category)
		{
			if (category == ConnectionTestCategory.MartinTest)
				return new[] { MartinTest };

			var setup = DependencyInjector.Get<IConnectionFrameworkSetup> ();
			return TestRegistration.Where (t => Filter (t.flags)).Select (t => t.type).ToList ();

			bool Filter (HttpInstrumentationTestFlags flags)
			{
				if (flags == HttpInstrumentationTestFlags.GZip) {
					if (!setup.SupportsGZip)
						return false;
					flags = HttpInstrumentationTestFlags.Working;
				}

				switch (category) {
				case ConnectionTestCategory.MartinTest:
					return false;
				case ConnectionTestCategory.HttpInstrumentation:
					return flags == HttpInstrumentationTestFlags.Working ||
						flags == HttpInstrumentationTestFlags.WorkingRequireSSL;
				case ConnectionTestCategory.HttpInstrumentationNoSSL:
					return flags == HttpInstrumentationTestFlags.Working;
				case ConnectionTestCategory.HttpInstrumentationStress:
					return flags == HttpInstrumentationTestFlags.Stress;
				case ConnectionTestCategory.HttpInstrumentationNewWebStack:
					if (!setup.UsingDotNet &&
					    (flags == HttpInstrumentationTestFlags.NewWebStackMono ||
					     flags == HttpInstrumentationTestFlags.NewWebStackRequireSSL))
						return true;
					return flags == HttpInstrumentationTestFlags.NewWebStack;
				case ConnectionTestCategory.HttpInstrumentationNewWebStackNoSSL:
					if (flags == HttpInstrumentationTestFlags.NewWebStackMono && !setup.UsingDotNet)
						return true;
					return flags == HttpInstrumentationTestFlags.NewWebStack;
				case ConnectionTestCategory.HttpInstrumentationExperimental:
					return flags == HttpInstrumentationTestFlags.Unstable;
				default:
					throw ctx.AssertFail (category);
				}
			}
		}

		static string GetTestName (ConnectionTestCategory category, HttpInstrumentationTestType type, params object[] args)
		{
			var sb = new StringBuilder ();
			sb.Append (type);
			foreach (var arg in args) {
				sb.AppendFormat (":{0}", arg);
			}
			return sb.ToString ();
		}

		public static HttpInstrumentationTestParameters GetParameters (TestContext ctx, ConnectionTestCategory category,
									       HttpInstrumentationTestType type)
		{
			var certificateProvider = DependencyInjector.Get<ICertificateProvider> ();
			var acceptAll = certificateProvider.AcceptAll ();

			var name = GetTestName (category, type);

			var parameters = new HttpInstrumentationTestParameters (category, type, name, ResourceManager.SelfSignedServerCertificate) {
				ClientCertificateValidator = acceptAll
			};

			parameters.ExpectedStatus = HttpStatusCode.OK;
			parameters.ExpectedError = WebExceptionStatus.Success;

			switch (GetEffectiveType (type)) {
			case HttpInstrumentationTestType.InvalidDataDuringHandshake:
				parameters.ExpectedStatus = HttpStatusCode.InternalServerError;
				parameters.ExpectedError = WebExceptionStatus.SecureChannelFailure;
				parameters.HasReadHandler = true;
				break;
			case HttpInstrumentationTestType.AbortDuringHandshake:
				parameters.ExpectedStatus = HttpStatusCode.InternalServerError;
				parameters.ExpectedError = WebExceptionStatus.RequestCanceled;
				parameters.HasReadHandler = true;
				break;
			case HttpInstrumentationTestType.AbortResponse:
				parameters.ExpectedStatus = HttpStatusCode.InternalServerError;
				parameters.ExpectedError = WebExceptionStatus.RequestCanceled;
				break;
			case HttpInstrumentationTestType.SimpleQueuedRequest:
			case HttpInstrumentationTestType.CancelQueuedRequest:
				parameters.HasReadHandler = true;
				parameters.ConnectionLimit = 1;
				break;
			case HttpInstrumentationTestType.CancelMainWhileQueued:
			case HttpInstrumentationTestType.NtlmWhileQueued:
				parameters.ConnectionLimit = 1;
				parameters.ExpectedStatus = HttpStatusCode.InternalServerError;
				parameters.ExpectedError = WebExceptionStatus.RequestCanceled;
				parameters.HasReadHandler = true;
				break;
			case HttpInstrumentationTestType.NtlmWhileQueued2:
				parameters.ConnectionLimit = 1;
				parameters.HasReadHandler = true;
				break;
			case HttpInstrumentationTestType.ThreeParallelRequests:
				parameters.ConnectionLimit = 5;
				parameters.HasReadHandler = true;
				break;
			case HttpInstrumentationTestType.ParallelRequestsSomeQueued:
				parameters.CountParallelRequests = 5;
				parameters.ConnectionLimit = 3;
				parameters.HasReadHandler = true;
				break;
			case HttpInstrumentationTestType.ManyParallelRequests:
				parameters.CountParallelRequests = 10;
				parameters.ConnectionLimit = 5;
				parameters.HasReadHandler = true;
				break;
			case HttpInstrumentationTestType.ManyParallelRequestsStress:
				parameters.CountParallelRequests = 100;
				parameters.ConnectionLimit = 25;
				parameters.HasReadHandler = true;
				break;
			case HttpInstrumentationTestType.CloseIdleConnection:
				parameters.IdleTime = 750;
				break;
			case HttpInstrumentationTestType.ReadTimeout:
				parameters.ExpectedStatus = HttpStatusCode.InternalServerError;
				parameters.ExpectedError = WebExceptionStatus.Timeout;
				break;
			case HttpInstrumentationTestType.Get404:
				parameters.ExpectedStatus = HttpStatusCode.NotFound;
				parameters.ExpectedError = WebExceptionStatus.ProtocolError;
				break;
			case HttpInstrumentationTestType.CloseRequestStream:
				parameters.ExpectedStatus = HttpStatusCode.InternalServerError;
				parameters.ExpectedError = WebExceptionStatus.RequestCanceled;
				break;
			case HttpInstrumentationTestType.ParallelRequests:
				parameters.HasReadHandler = true;
				break;
			case HttpInstrumentationTestType.CloseCustomConnectionGroup:
				parameters.IgnoreStreamErrors = true;
				break;
			case HttpInstrumentationTestType.Simple:
			case HttpInstrumentationTestType.SimplePost:
			case HttpInstrumentationTestType.LargeHeader:
			case HttpInstrumentationTestType.LargeHeader2:
			case HttpInstrumentationTestType.SendResponseAsBlob:
			case HttpInstrumentationTestType.SimpleNtlm:
			case HttpInstrumentationTestType.PostNtlm:
			case HttpInstrumentationTestType.SimpleRedirect:
			case HttpInstrumentationTestType.PostRedirect:
			case HttpInstrumentationTestType.NtlmChunked:
			case HttpInstrumentationTestType.NtlmInstrumentation:
			case HttpInstrumentationTestType.NtlmClosesConnection:
			case HttpInstrumentationTestType.NtlmReusesConnection:
			case HttpInstrumentationTestType.ReuseConnection:
			case HttpInstrumentationTestType.ReuseConnection2:
			case HttpInstrumentationTestType.ReuseAfterPartialRead:
			case HttpInstrumentationTestType.CustomConnectionGroup:
			case HttpInstrumentationTestType.ReuseCustomConnectionGroup:
			case HttpInstrumentationTestType.ParallelNtlm:
			case HttpInstrumentationTestType.RedirectOnSameConnection:
			case HttpInstrumentationTestType.RedirectNoReuse:
			case HttpInstrumentationTestType.RedirectNoLength:
			case HttpInstrumentationTestType.PutChunked:
			case HttpInstrumentationTestType.PutChunkDontCloseRequest:
			case HttpInstrumentationTestType.ServerAbortsRedirect:
			case HttpInstrumentationTestType.PostChunked:
			case HttpInstrumentationTestType.PostContentLength:
				break;
			case HttpInstrumentationTestType.ServerAbortsPost:
				parameters.ExpectedStatus = HttpStatusCode.BadRequest;
				parameters.ExpectedError = WebExceptionStatus.ProtocolError;
				break;
			case HttpInstrumentationTestType.EntityTooBig:
			case HttpInstrumentationTestType.ClientAbortsPost:
				parameters.ExpectedStatus = HttpStatusCode.InternalServerError;
				parameters.ExpectedError = WebExceptionStatus.AnyErrorStatus;
				break;
			case HttpInstrumentationTestType.GetChunked:
			case HttpInstrumentationTestType.SimpleGZip:
			case HttpInstrumentationTestType.TestResponseStream:
			case HttpInstrumentationTestType.LargeChunkRead:
			case HttpInstrumentationTestType.LargeGZipRead:
			case HttpInstrumentationTestType.GZipWithLength:
			case HttpInstrumentationTestType.ResponseStreamCheckLength2:
			case HttpInstrumentationTestType.ResponseStreamCheckLength:
				parameters.ExpectedStatus = HttpStatusCode.OK;
				parameters.ExpectedError = WebExceptionStatus.Success;
				break;
			default:
				throw ctx.AssertFail (GetEffectiveType (type));
			}

			return parameters;
		}

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
				await Task.Delay ((int)(Parameters.IdleTime * 2.5)).ConfigureAwait (false);
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

			HttpOperationFlags flags = HttpOperationFlags.None;

			switch (EffectiveType) {
			case HttpInstrumentationTestType.InvalidDataDuringHandshake:
			case HttpInstrumentationTestType.AbortDuringHandshake:
			case HttpInstrumentationTestType.CancelMainWhileQueued:
				return (hello, HttpOperationFlags.ServerAbortsHandshake | HttpOperationFlags.AbortAfterClientExits);
			case HttpInstrumentationTestType.SimpleNtlm:
				return (new AuthenticationHandler (AuthenticationType.NTLM, hello), flags);
			case HttpInstrumentationTestType.ReuseConnection:
				return (new HttpInstrumentationHandler (this, null, null, !primary), flags);
			case HttpInstrumentationTestType.ReuseAfterPartialRead:
				return (new HttpInstrumentationHandler (
					this, null, ConnectionHandler.GetLargeStringContent (2500), !primary),
					HttpOperationFlags.ClientUsesNewConnection);
			case HttpInstrumentationTestType.ReuseConnection2:
				if (primary)
					return (new HttpInstrumentationHandler (this, null, HttpContent.HelloWorld, false), flags);
				return (new HttpInstrumentationHandler (this, null, HttpContent.HelloWorld, true), flags);
			case HttpInstrumentationTestType.SimplePost:
				return (postHello, flags);
			case HttpInstrumentationTestType.SimpleRedirect:
				return (new RedirectHandler (hello, HttpStatusCode.Redirect), flags);
			case HttpInstrumentationTestType.PostNtlm:
				return (new AuthenticationHandler (AuthenticationType.NTLM, postHello), flags);
			case HttpInstrumentationTestType.PostRedirect:
				return (new RedirectHandler (postHello, HttpStatusCode.TemporaryRedirect), flags);
			case HttpInstrumentationTestType.NtlmChunked:
				return (new AuthenticationHandler (AuthenticationType.NTLM, chunkedPost), flags);
			case HttpInstrumentationTestType.Get404:
				return (new GetHandler (EffectiveType.ToString (), null, HttpStatusCode.NotFound), flags);
			case HttpInstrumentationTestType.CloseIdleConnection:
			case HttpInstrumentationTestType.CloseCustomConnectionGroup:
				return (new HttpInstrumentationHandler (this, null, null, false), flags);
			case HttpInstrumentationTestType.NtlmClosesConnection:
				return (new HttpInstrumentationHandler (this, GetAuthenticationManager (), null, true), flags);
			case HttpInstrumentationTestType.NtlmReusesConnection:
				return (new HttpInstrumentationHandler (this, GetAuthenticationManager (), null, false), flags);
			case HttpInstrumentationTestType.ParallelNtlm:
			case HttpInstrumentationTestType.NtlmInstrumentation:
			case HttpInstrumentationTestType.NtlmWhileQueued:
			case HttpInstrumentationTestType.NtlmWhileQueued2:
				return (new HttpInstrumentationHandler (this, GetAuthenticationManager (), null, false), flags);
			case HttpInstrumentationTestType.LargeHeader:
			case HttpInstrumentationTestType.LargeHeader2:
			case HttpInstrumentationTestType.SendResponseAsBlob:
				return (new HttpInstrumentationHandler (this, null, ConnectionHandler.TheQuickBrownFoxContent, true), flags);
			case HttpInstrumentationTestType.CustomConnectionGroup:
				return (new HttpInstrumentationHandler (this, null, null, !primary),
					HttpOperationFlags.DontReuseConnection | HttpOperationFlags.ForceNewConnection);
			case HttpInstrumentationTestType.ReuseCustomConnectionGroup:
			case HttpInstrumentationTestType.ReadTimeout:
			case HttpInstrumentationTestType.AbortResponse:
				return (new HttpInstrumentationHandler (this, null, null, !primary), flags);
			case HttpInstrumentationTestType.CloseRequestStream:
				return (new HttpInstrumentationHandler (this, null, null, !primary), HttpOperationFlags.AbortAfterClientExits);
			case HttpInstrumentationTestType.RedirectOnSameConnection:
				return (new HttpInstrumentationHandler (this, null, null, false), flags);
			case HttpInstrumentationTestType.RedirectNoReuse:
				return (new RedirectHandler (hello, HttpStatusCode.Redirect), flags);
			case HttpInstrumentationTestType.RedirectNoLength:
				return (new HttpInstrumentationHandler (this, null, null, false), flags | HttpOperationFlags.RedirectOnNewConnection);
			case HttpInstrumentationTestType.PutChunked:
			case HttpInstrumentationTestType.PutChunkDontCloseRequest:
				return (new HttpInstrumentationHandler (this, null, null, true), flags);
			case HttpInstrumentationTestType.ServerAbortsRedirect:
				return (new HttpInstrumentationHandler (this, null, null, false), HttpOperationFlags.ServerAbortsRedirection);
			case HttpInstrumentationTestType.ServerAbortsPost:
				return (new HttpInstrumentationHandler (this, null, null, true), HttpOperationFlags.ServerAbortsRedirection);
			case HttpInstrumentationTestType.PostChunked:
				return (new HttpInstrumentationHandler (this, null, null, false), HttpOperationFlags.DontReadRequestBody);
			case HttpInstrumentationTestType.EntityTooBig:
			case HttpInstrumentationTestType.ClientAbortsPost:
				return (new HttpInstrumentationHandler (this, null, null, false),
					HttpOperationFlags.AbortAfterClientExits | HttpOperationFlags.DontReadRequestBody);
			case HttpInstrumentationTestType.PostContentLength:
				return (new HttpInstrumentationHandler (this, null, null, false),
					HttpOperationFlags.DontReadRequestBody);
			case HttpInstrumentationTestType.GetChunked:
				return (new GetHandler (EffectiveType.ToString (), HttpContent.HelloChunked), flags);
			case HttpInstrumentationTestType.SimpleGZip:
				return (new HttpInstrumentationHandler (this, null, HttpContent.TheQuickBrownFox, true), flags);
			case HttpInstrumentationTestType.TestResponseStream:
				return (new HttpInstrumentationHandler (this, null, new StringContent ("AAAA"), true), flags);
			case HttpInstrumentationTestType.LargeChunkRead:
				return (new HttpInstrumentationHandler (this, null, HttpContent.TheQuickBrownFoxChunked, false), flags);
			case HttpInstrumentationTestType.LargeGZipRead:
				return (new HttpInstrumentationHandler (this, null, ConnectionHandler.GetLargeChunkedContent (16384), false), flags);
			case HttpInstrumentationTestType.GZipWithLength:
				return (new HttpInstrumentationHandler (this, null, ConnectionHandler.GetLargeStringContent (16384), false), flags);
			case HttpInstrumentationTestType.ResponseStreamCheckLength2:
				return (new HttpInstrumentationHandler (this, null, HttpContent.HelloChunked, false), flags);
			case HttpInstrumentationTestType.ResponseStreamCheckLength:
				return (new HttpInstrumentationHandler (this, null, HttpContent.HelloWorld, false), flags);
			default:
				return (hello, flags);
			}

			AuthenticationManager GetAuthenticationManager ()
			{
				var manager = new AuthenticationManager (AuthenticationType.NTLM, AuthenticationHandler.GetCredentials ());
				var old = Interlocked.CompareExchange (ref authManager, manager, null);
				return old ?? manager;
			}
		}

		protected override InstrumentationOperation CreateOperation (
			TestContext ctx, Handler handler, InstrumentationOperationType type, HttpOperationFlags flags,
			HttpStatusCode expectedStatus, WebExceptionStatus expectedError)
		{
			return new Operation (this, handler, type, flags, expectedStatus, expectedError);
		}

		async Task HandleRequest (
			TestContext ctx, HttpInstrumentationHandler handler,
			HttpConnection connection, HttpRequest request,
			AuthenticationState state, CancellationToken cancellationToken)
		{
			switch (EffectiveType) {
			case HttpInstrumentationTestType.ReuseConnection:
			case HttpInstrumentationTestType.ReuseCustomConnectionGroup:
			case HttpInstrumentationTestType.RedirectOnSameConnection:
				MustReuseConnection ();
				break;

			case HttpInstrumentationTestType.ParallelNtlm:
				await ParallelNtlm ().ConfigureAwait (false);
				break;

			case HttpInstrumentationTestType.ReuseAfterPartialRead:
				// We can't reuse the connection because we did not read the entire response.
				MustNotReuseConnection ();
				break;

			case HttpInstrumentationTestType.CustomConnectionGroup:
				// We can't reuse the connection because we're in a different connection group.
				MustNotReuseConnection ();
				break;

			case HttpInstrumentationTestType.NtlmInstrumentation:
				break;
			}

			async Task ParallelNtlm ()
			{
				var firstHandler = (HttpInstrumentationHandler)PrimaryOperation.Handler;
				ctx.LogDebug (2, $"{handler.ME}: {handler == firstHandler} {state}");
				if (handler != firstHandler || state != AuthenticationState.Challenge)
					return;

				var newHandler = (HttpInstrumentationHandler)firstHandler.Clone ();
				var flags = PrimaryOperation.Flags;

				var operation = StartOperation (ctx, cancellationToken, newHandler, InstrumentationOperationType.Queued, flags);
				await operation.WaitForRequest ();
			}

			void MustNotReuseConnection ()
			{
				var firstHandler = (HttpInstrumentationHandler)PrimaryOperation.Handler;
				ctx.LogDebug (2, $"{handler.ME}: {handler == firstHandler} {handler.RemoteEndPoint}");
				if (handler == firstHandler)
					return;
				ctx.Assert (connection.RemoteEndPoint, Is.Not.EqualTo (firstHandler.RemoteEndPoint), "RemoteEndPoint");
			}

			void MustReuseConnection ()
			{
				var firstHandler = (HttpInstrumentationHandler)PrimaryOperation.Handler;
				ctx.LogDebug (2, $"{handler.ME}: {handler == firstHandler} {handler.RemoteEndPoint}");
				if (handler == firstHandler)
					return;
				ctx.Assert (connection.RemoteEndPoint, Is.EqualTo (firstHandler.RemoteEndPoint), "RemoteEndPoint");
			}
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
				var parallelTasks = new Task [Parameters.CountParallelRequests];
				var parallelOperations = new InstrumentationOperation [Parameters.CountParallelRequests];
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
					InstrumentationOperationType.Queued, HttpOperationFlags.AbortAfterClientExits,
					HttpStatusCode.InternalServerError, WebExceptionStatus.RequestCanceled);
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
				switch (EffectiveType) {
				case HttpInstrumentationTestType.ReuseAfterPartialRead:
				case HttpInstrumentationTestType.CloseRequestStream:
				case HttpInstrumentationTestType.ReadTimeout:
				case HttpInstrumentationTestType.AbortResponse:
					return new HttpInstrumentationRequest (InstrumentationHandler, uri);
				case HttpInstrumentationTestType.NtlmWhileQueued:
				case HttpInstrumentationTestType.NtlmWhileQueued2:
					if (Type == InstrumentationOperationType.Primary)
						return new HttpInstrumentationRequest (InstrumentationHandler, uri);
					return new TraditionalRequest (uri);

				case HttpInstrumentationTestType.PutChunked:
				case HttpInstrumentationTestType.PutChunkDontCloseRequest:
					return new HttpInstrumentationRequest (InstrumentationHandler, uri) {
						Content = ConnectionHandler.GetLargeStringContent (50)
					};
				case HttpInstrumentationTestType.PostChunked:
				case HttpInstrumentationTestType.EntityTooBig:
				case HttpInstrumentationTestType.PostContentLength:
				case HttpInstrumentationTestType.ClientAbortsPost:
				case HttpInstrumentationTestType.SimpleGZip:
				case HttpInstrumentationTestType.TestResponseStream:
				case HttpInstrumentationTestType.LargeChunkRead:
				case HttpInstrumentationTestType.LargeGZipRead:
				case HttpInstrumentationTestType.GZipWithLength:
				case HttpInstrumentationTestType.ResponseStreamCheckLength2:
				case HttpInstrumentationTestType.ResponseStreamCheckLength:
					return new HttpInstrumentationRequest (InstrumentationHandler, uri);
				default:
					return new TraditionalRequest (uri);
				}
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
				if (Parent.Parameters.ConnectionLimit != 0)
					ServicePoint.ConnectionLimit = Parent.Parameters.ConnectionLimit;
				if (Parent.Parameters.IdleTime != 0)
					ServicePoint.MaxIdleTime = Parent.Parameters.IdleTime;
				request.RequestExt.ReadWriteTimeout = int.MaxValue;
				request.RequestExt.Timeout = int.MaxValue;

				switch (EffectiveType) {
				case HttpInstrumentationTestType.SimplePost:
					request.SetContentLength (((PostHandler)Handler).Content.Length);
					break;
				case HttpInstrumentationTestType.CustomConnectionGroup:
				case HttpInstrumentationTestType.ReuseCustomConnectionGroup:
					request.RequestExt.ConnectionGroupName = "custom";
					break;
				}
			}

			protected override Task<Response> RunInner (TestContext ctx, Request request, CancellationToken cancellationToken)
			{
				ctx.LogDebug (2, $"{ME} RUN INNER");
				switch (EffectiveType) {
				case HttpInstrumentationTestType.ServerAbortsPost:
					return ((TraditionalRequest)request).Send (ctx, cancellationToken);
				default:
					return ((TraditionalRequest)request).SendAsync (ctx, cancellationToken);
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

		class HttpInstrumentationRequest : TraditionalRequest
		{
			public HttpInstrumentationHandler Handler {
				get;
			}

			public HttpInstrumentationTestRunner TestRunner {
				get;
			}

			public string ME {
				get;
			}

			TaskCompletionSource<bool> finishedTcs;

			public Task WaitForCompletion ()
			{
				return finishedTcs.Task;
			}

			public HttpInstrumentationRequest (HttpInstrumentationHandler handler, Uri uri)
				: base (uri)
			{
				Handler = handler;
				TestRunner = handler.TestRunner;
				finishedTcs = new TaskCompletionSource<bool> ();
				ME = $"{GetType ().Name}({TestRunner.EffectiveType})";

				switch (TestRunner.EffectiveType) {
				case HttpInstrumentationTestType.PostChunked:
					Content = new HttpInstrumentationContent (TestRunner, this);
					Method = "POST";
					SendChunked ();
					break;
				case HttpInstrumentationTestType.EntityTooBig:
				case HttpInstrumentationTestType.PostContentLength:
				case HttpInstrumentationTestType.ClientAbortsPost:
					Content = new HttpInstrumentationContent (TestRunner, this);
					Method = "POST";
					break;
				case HttpInstrumentationTestType.SimpleGZip:
				case HttpInstrumentationTestType.LargeGZipRead:
				case HttpInstrumentationTestType.GZipWithLength:
					RequestExt.AutomaticDecompression = true;
					break;
				}
			}

			protected override Task WriteBody (TestContext ctx, CancellationToken cancellationToken)
			{
				switch (TestRunner.EffectiveType) {
				case HttpInstrumentationTestType.PutChunked:
				case HttpInstrumentationTestType.PutChunkDontCloseRequest:
					return PutChunked ();

				case HttpInstrumentationTestType.EntityTooBig:
					return EntityTooBig ();

				case HttpInstrumentationTestType.PostContentLength:
					return PostContentLength ();

				case HttpInstrumentationTestType.ClientAbortsPost:
					return ClientAbortsPost ();

				default:
					return base.WriteBody (ctx, cancellationToken);
				}

				async Task EntityTooBig ()
				{
					var stream = await RequestExt.GetRequestStreamAsync ().ConfigureAwait (false);
					await Content.WriteToAsync (ctx, stream, cancellationToken).ConfigureAwait (false);
					// This throws on .NET
					try { stream.Dispose (); } catch { }
				}

				async Task PostContentLength ()
				{
					using (var stream = await RequestExt.GetRequestStreamAsync ().ConfigureAwait (false)) {
						await WaitWithTimeout (ctx, 1500, Handler.WaitUntilReady ());
						await Content.WriteToAsync (ctx, stream, cancellationToken);
						stream.Flush ();
					}
				}

				async Task PutChunked ()
				{
					var stream = await RequestExt.GetRequestStreamAsync ().ConfigureAwait (false);
					try {
						await Content.WriteToAsync (ctx, stream, cancellationToken).ConfigureAwait (false);
						await stream.FlushAsync ();
					} finally {
						if (TestRunner.EffectiveType == HttpInstrumentationTestType.PutChunked)
							stream.Dispose ();
					}
				}

				async Task ClientAbortsPost ()
				{
					var stream = await RequestExt.GetRequestStreamAsync ().ConfigureAwait (false);
					try {
						stream.Dispose ();
					} catch (Exception ex) {
						ctx.LogMessage ($"{ME} GOT EX: {ex.Message}");
					}
				}
			}

			public override async Task<Response> SendAsync (TestContext ctx, CancellationToken cancellationToken)
			{
				var portable = DependencyInjector.Get<IPortableSupport> ();
				if (TestRunner.EffectiveType == HttpInstrumentationTestType.CloseRequestStream) {
					Request.Method = "POST";
					RequestExt.SetContentLength (16384);
					var stream = await RequestExt.GetRequestStreamAsync ().ConfigureAwait (false);
					try {
						portable.Close (stream);
						throw ctx.AssertFail ("Expected exception.");
					} catch (Exception ex) {
						return new SimpleResponse (this, HttpStatusCode.InternalServerError, null, ex);
					}
				}

				return await base.SendAsync (ctx, cancellationToken).ConfigureAwait (false);
			}

			protected override async Task<Response> GetResponseFromHttp (
				TestContext ctx, HttpWebResponse response, WebException error, CancellationToken cancellationToken)
			{
				cancellationToken.ThrowIfCancellationRequested ();
				HttpContent content = null;

				ctx.LogDebug (4, $"{ME} GET RESPONSE FROM HTTP");

				switch (TestRunner.EffectiveType) {
				case HttpInstrumentationTestType.ReadTimeout:
					return await ReadWithTimeout (5000, WebExceptionStatus.Timeout).ConfigureAwait (false);

				case HttpInstrumentationTestType.AbortResponse:
				case HttpInstrumentationTestType.NtlmWhileQueued:
					return await ReadWithTimeout (0, WebExceptionStatus.RequestCanceled).ConfigureAwait (false);
				}

				using (var stream = response.GetResponseStream ()) {
					switch (TestRunner.EffectiveType) {
					case HttpInstrumentationTestType.ReuseAfterPartialRead:
						content = await ReadStringAsBuffer (stream, 1234).ConfigureAwait (false);
						break;

					case HttpInstrumentationTestType.TestResponseStream:
						content = await TestResponseStream (stream).ConfigureAwait (false);
						break;

					case HttpInstrumentationTestType.LargeChunkRead:
						content = await LargeChunkRead (stream).ConfigureAwait (false);
						break;

					case HttpInstrumentationTestType.LargeGZipRead:
						content = await ReadAsString (stream).ConfigureAwait (false);
						break;

					case HttpInstrumentationTestType.GZipWithLength:
						content = await GZipWithLength (stream).ConfigureAwait (false);
						break;

					case HttpInstrumentationTestType.ResponseStreamCheckLength2:
						content = await ResponseStreamCheckLength (stream, true).ConfigureAwait (false);
						break;

					case HttpInstrumentationTestType.ResponseStreamCheckLength:
						content = await ResponseStreamCheckLength (stream, false).ConfigureAwait (false);
						break;

					default:
						content = await ReadAsString (stream).ConfigureAwait (false);
						break;
					}
				}

				var status = response.StatusCode;

				response.Dispose ();
				finishedTcs.TrySetResult (true);
				return new SimpleResponse (this, status, content, error);

				async Task<Response> ReadWithTimeout (int timeout, WebExceptionStatus expectedStatus)
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
						return new SimpleResponse (this, HttpStatusCode.InternalServerError, null, wexc);
					} finally {
						finishedTcs.TrySetResult (true);
					}
				}

				async Task<HttpContent> ReadStringAsBuffer (Stream stream, int size)
				{
					var buffer = new byte[size];
					var ret = await stream.ReadAsync (buffer, 0, buffer.Length).ConfigureAwait (false);
					ctx.Assert (ret, Is.EqualTo (buffer.Length));
					return StringContent.CreateMaybeNull (new ASCIIEncoding ().GetString (buffer, 0, ret));
				}

				async Task<HttpContent> ReadAsString (Stream stream)
				{
					using (var reader = new StreamReader (stream)) {
						string text = null;
						if (!reader.EndOfStream)
							text = await reader.ReadToEndAsync ().ConfigureAwait (false);
						return StringContent.CreateMaybeNull (text);
					}
				}

				async Task<HttpContent> TestResponseStream (Stream stream)
				{
					var buffer = new byte[5];
					var ret = await stream.ReadAsync (buffer, 4, 1).ConfigureAwait (false);
					ctx.Assert (ret, Is.EqualTo (1), "#A1");
					ctx.Assert (buffer[4], Is.EqualTo ((byte)65), "#A2");
					ret = await stream.ReadAsync (buffer, 0, 2);
					ctx.Assert (ret, Is.EqualTo (2), "#B1");
					return Handler.Content;
				}

				async Task<HttpContent> LargeChunkRead (Stream stream)
				{
					var buffer = new byte[43];
					var ret = await stream.ReadAsync (buffer, 0, buffer.Length).ConfigureAwait (false);
					ctx.Assert (ret, Is.EqualTo (ConnectionHandler.TheQuickBrownFox.Length), "#A1");
					var text = Encoding.UTF8.GetString (buffer, 0, ret);
					return new StringContent (text);
				}

				async Task<HttpContent> GZipWithLength (Stream stream)
				{
					using (var ms = new MemoryStream ()) {
						await stream.CopyToAsync (ms, 16384).ConfigureAwait (false);
						var bytes = ms.ToArray ();
						var text = Encoding.UTF8.GetString (bytes, 0, bytes.Length);
						return new StringContent (text);
					}
				}

				async Task<HttpContent> ResponseStreamCheckLength (Stream stream, bool chunked)
				{
					await ctx.AssertException<NotSupportedException> (() => Task.FromResult (stream.Length), "Length should throw");
					if (chunked) {
						ctx.Assert (response.ContentLength, Is.EqualTo (-1L), "ContentLength");
						ctx.Assert (response.Headers["Transfer-Encoding"], Is.EqualTo ("chunked"), "chunked encoding");
					} else {
						ctx.Assert (response.ContentLength, Is.EqualTo ((long)Handler.Content.Length), "ContentLength");
						ctx.Assert (response.Headers["Content-Length"], Is.EqualTo (Handler.Content.Length.ToString ()), "Content-Length header");
					}
					return await GZipWithLength (stream).ConfigureAwait (false);
				}
			}
		}

		class HttpInstrumentationContent : HttpContent
		{
			public HttpInstrumentationTestRunner TestRunner {
				get;
			}

			public HttpInstrumentationRequest Request {
				get;
			}

			public string ME {
				get;
			}

			public HttpInstrumentationContent (HttpInstrumentationTestRunner runner, HttpInstrumentationRequest request)
			{
				TestRunner = runner;
				Request = request;
				ME = $"{GetType ().Name}({runner.EffectiveType})";

				switch (runner.EffectiveType) {
				case HttpInstrumentationTestType.EntityTooBig:
				case HttpInstrumentationTestType.ClientAbortsPost:
					HasLength = true;
					Length = 16;
					break;
				case HttpInstrumentationTestType.PostContentLength:
					HasLength = true;
					Length = ConnectionHandler.TheQuickBrownFoxBuffer.Length;
					break;
				case HttpInstrumentationTestType.NtlmWhileQueued2:
					HasLength = true;
					Length = ((HelloWorldHandler)request.Handler.Target).Message.Length;
					break;
				case HttpInstrumentationTestType.LargeChunkRead:
					break;
				default:
					HasLength = true;
					Length = 4096;
					break;
				}
			}

			public sealed override bool HasLength {
				get;
			}

			public sealed override int Length {
				get;
			}

			public override void AddHeadersTo (HttpMessage message)
			{
				if (HasLength) {
					message.ContentLength = Length;
					message.ContentType = "text/plain";
				} else {
					message.TransferEncoding = "chunked";
				}
			}

			public override byte[] AsByteArray ()
			{
				throw new NotImplementedException ();
			}

			public override string AsString ()
			{
				throw new NotImplementedException ();
			}

			public override async Task WriteToAsync (TestContext ctx, Stream stream, CancellationToken cancellationToken)
			{
				ctx.LogDebug (4, $"{ME} WRITE BODY");

				switch (TestRunner.EffectiveType) {
				case HttpInstrumentationTestType.NtlmWhileQueued:
					await HandleNtlmWhileQueued ().ConfigureAwait (false);
					break;

				case HttpInstrumentationTestType.NtlmWhileQueued2:
					await HandleNtlmWhileQueued2 ().ConfigureAwait (false);
					break;

				case HttpInstrumentationTestType.ReadTimeout:
					await stream.WriteAsync (ConnectionHandler.TheQuickBrownFoxBuffer, cancellationToken).ConfigureAwait (false);
					await stream.FlushAsync (cancellationToken);
					await Task.WhenAny (Request.WaitForCompletion (), Task.Delay (10000));
					break;

				case HttpInstrumentationTestType.AbortResponse:
					await stream.WriteAsync (ConnectionHandler.TheQuickBrownFoxBuffer, cancellationToken).ConfigureAwait (false);
					await stream.FlushAsync (cancellationToken);
					await Task.Delay (500).ConfigureAwait (false);
					TestRunner.PrimaryOperation.Request.Abort ();
					await Task.WhenAny (Request.WaitForCompletion (), Task.Delay (10000));
					break;

				case HttpInstrumentationTestType.PostChunked:
					await HandlePostChunked ().ConfigureAwait (false);
					break;

				case HttpInstrumentationTestType.EntityTooBig:
					await ctx.AssertException<ProtocolViolationException> (() =>
						stream.WriteAsync (ConnectionHandler.TheQuickBrownFoxBuffer, cancellationToken),
						"writing too many bytes").ConfigureAwait (false);
					break;

				case HttpInstrumentationTestType.PostContentLength:
					await stream.WriteAsync (ConnectionHandler.TheQuickBrownFoxBuffer, cancellationToken).ConfigureAwait (false);
					await stream.FlushAsync (cancellationToken);
					break;

				case HttpInstrumentationTestType.LargeChunkRead:
					await HandleLargeChunkRead ().ConfigureAwait (false);
					break;

				default:
					throw ctx.AssertFail (TestRunner.EffectiveType);
				}

				async Task HandleNtlmWhileQueued ()
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

					await TestRunner.QueuedOperation.StartDelayedListener (ctx);

					/*
					 * Then we abort the client-side NTLM request and wait for it to complete.
					 * This will eventually close the connection, so the ServicePoint scheduler will
					 * start the "Hello World" request.
					 */

					TestRunner.PrimaryOperation.Request.Abort ();
					await Task.WhenAny (Request.WaitForCompletion (), Task.Delay (10000));
				}

				async Task HandleNtlmWhileQueued2 ()
				{
					/*
					 * Similar to NtlmWhileQueued, but we now complete both requests.
					 */
					await Task.Delay (500).ConfigureAwait (false);
					await TestRunner.QueuedOperation.StartDelayedListener (ctx);

					var message = ((HelloWorldHandler)Request.Handler.Target).Message;
					await stream.WriteAsync (message, cancellationToken).ConfigureAwait (false);
					await stream.FlushAsync (cancellationToken);
				}

				async Task HandlePostChunked ()
				{
					await stream.WriteAsync (ConnectionHandler.TheQuickBrownFoxBuffer, cancellationToken).ConfigureAwait (false);
					await stream.FlushAsync (cancellationToken);

					await WaitWithTimeout (ctx, 1500, Request.Handler.WaitUntilReady ());

					await stream.WriteAsync (ConnectionHandler.GetLargeTextBuffer (50), cancellationToken);
				}

				async Task HandleLargeChunkRead ()
				{
					await ChunkedContent.WriteChunkAsBlob (
						stream, ConnectionHandler.TheQuickBrownFoxBuffer,
						cancellationToken).ConfigureAwait (false);
					await stream.FlushAsync (cancellationToken);

					await ChunkedContent.WriteChunkAsBlob (
						stream, ConnectionHandler.GetLargeTextBuffer (50),
						cancellationToken);
					await ChunkedContent.WriteChunkTrailer (stream, cancellationToken);
					await stream.FlushAsync (cancellationToken);
				}
			}
		}

		class HttpInstrumentationResponse : Response
		{
			public HttpInstrumentationTestRunner TestRunner {
				get;
			}

			public HttpWebResponse Response {
				get;
			}

			public string ME {
				get;
			}

			TaskCompletionSource<bool> finishedTcs;

			public Task WaitForCompletion ()
			{
				return finishedTcs.Task;
			}

			public HttpInstrumentationResponse (HttpInstrumentationRequest request, HttpWebResponse response)
				: base (request)
			{
				TestRunner = request.TestRunner;
				Response = response;
				finishedTcs = new TaskCompletionSource<bool> ();
				ME = $"{GetType ().Name}({TestRunner.EffectiveType})";
			}

			public HttpInstrumentationResponse (HttpInstrumentationRequest request, WebException error)
				: base (request)
			{
				TestRunner = request.TestRunner;
				Response = (HttpWebResponse)error.Response;
				Error = error;
				finishedTcs = new TaskCompletionSource<bool> ();
				ME = $"{GetType ().Name}({TestRunner.EffectiveType})";
			}

			public override bool IsSuccess => false;

			public override HttpStatusCode Status => Response.StatusCode;

			public sealed override Exception Error {
				get;
			}

			public override HttpContent Content => null;

			internal async Task CheckResponse (TestContext ctx, CancellationToken cancellationToken)
			{
				ctx.LogMessage ("CHECK RESPONSE");
				cancellationToken.ThrowIfCancellationRequested ();

				var stream = Response.GetResponseStream ();

				var buffer = new byte[1024];
				var ret = await stream.ReadAsync (buffer, 0, buffer.Length, cancellationToken).ConfigureAwait (false);

				ctx.LogMessage ($"CHECK RESPONSE #1: {ret}");
			}
		}

		class HttpInstrumentationHandler : Handler
		{
			public HttpInstrumentationTestRunner TestRunner {
				get;
			}

			public bool CloseConnection {
				get;
			}

			public HttpContent Content {
				get;
			}

			public IPEndPoint RemoteEndPoint {
				get;
				private set;
			}

			public Handler Target {
				get;
			}

			public AuthenticationManager AuthManager {
				get;
			}

			public bool IsSecondRequest {
				get;
				private set;
			}

			public string ME {
				get;
			}

			TaskCompletionSource<bool> readyTcs;

			public HttpInstrumentationHandler (HttpInstrumentationTestRunner parent, AuthenticationManager authManager,
							   HttpContent content, bool closeConnection)
				: base (parent.EffectiveType.ToString ())
			{
				TestRunner = parent;
				AuthManager = authManager;
				Content = content;
				CloseConnection = closeConnection;
				ME = $"{GetType ().Name}({parent.EffectiveType})";
				readyTcs = new TaskCompletionSource<bool> ();
				Flags = RequestFlags.KeepAlive;
				if (closeConnection)
					Flags |= RequestFlags.CloseConnection;

				if (AuthManager != null)
					Target = new HelloWorldHandler (ME);

				switch (parent.EffectiveType) {
				case HttpInstrumentationTestType.RedirectOnSameConnection:
				case HttpInstrumentationTestType.RedirectNoLength:
					Target = new HelloWorldHandler (ME);
					break;
				}
			}

			HttpInstrumentationHandler (HttpInstrumentationHandler other)
				: base (other.Value)
			{
				TestRunner = other.TestRunner;
				Content = other.Content;
				CloseConnection = CloseConnection;
				ME = other.ME;
				Flags = other.Flags;
				Target = other.Target;
				AuthManager = other.AuthManager;
				readyTcs = new TaskCompletionSource<bool> ();
			}

			HttpInstrumentationRequest currentRequest;

			public override object Clone ()
			{
				return new HttpInstrumentationHandler (this);
			}

			public override void ConfigureRequest (Request request, Uri uri)
			{
				if (AuthManager != null)
					AuthManager.ConfigureRequest (request);

				if (request is HttpInstrumentationRequest instrumentationRequest) {
					if (Interlocked.CompareExchange (ref currentRequest, instrumentationRequest, null) != null)
						throw new InvalidOperationException ();
				}

				switch (TestRunner.EffectiveType) {
				case HttpInstrumentationTestType.ReuseConnection2:
					request.Method = "POST";
					if (Content != null) {
						request.SetContentType ("text/plain");
						request.Content = Content.RemoveTransferEncoding ();
					}
					break;

				case HttpInstrumentationTestType.ReadTimeout:
					currentRequest.RequestExt.ReadWriteTimeout = 100;
					break;

				case HttpInstrumentationTestType.PutChunked:
				case HttpInstrumentationTestType.PutChunkDontCloseRequest:
					request.Method = "PUT";
					request.SetContentType ("application/octet-stream");
					request.SetContentLength (request.Content.Length);
					request.SendChunked ();
					break;

				case HttpInstrumentationTestType.ServerAbortsPost:
					request.Method = "POST";
					request.SetContentType ("application/x-www-form-urlencoded");
					request.Content = new FormContent (("foo", "bar"), ("hello", "world"), ("escape", "this needs % escaping"));
					break;

				case HttpInstrumentationTestType.EntityTooBig:
				case HttpInstrumentationTestType.PostContentLength:
				case HttpInstrumentationTestType.ClientAbortsPost:
					request.Method = "POST";
					request.SetContentType ("text/plain");
					request.SetContentLength (request.Content.Length);
					break;

				case HttpInstrumentationTestType.SimpleGZip:
					break;
				}

				base.ConfigureRequest (request, uri);
			}

			async Task<HttpResponse> HandleNtlmRequest (
				TestContext ctx, HttpOperation operation, HttpConnection connection, HttpRequest request,
				RequestFlags effectiveFlags, CancellationToken cancellationToken)
			{
				var me = $"{ME}.{nameof (HandleNtlmRequest)}";
				ctx.LogDebug (3, $"{me}: {connection.RemoteEndPoint}");

				AuthenticationState state;
				var response = AuthManager.HandleAuthentication (ctx, connection, request, out state);
				ctx.LogDebug (3, $"{me}: {connection.RemoteEndPoint} - {state} {response}");

				if (state == AuthenticationState.Unauthenticated) {
					ctx.Assert (RemoteEndPoint, Is.Null, "first request");
					RemoteEndPoint = connection.RemoteEndPoint;
				} else if (TestRunner.EffectiveType == HttpInstrumentationTestType.NtlmInstrumentation) {
					if (state == AuthenticationState.Challenge) {
						ctx.LogDebug (3, $"{me}: {connection.RemoteEndPoint} {RemoteEndPoint}");
						RemoteEndPoint = connection.RemoteEndPoint;
					} else
						ctx.Assert (connection.RemoteEndPoint, Is.EqualTo (RemoteEndPoint), "must reuse connection");
				}

				await TestRunner.HandleRequest (
					ctx, this, connection, request, state, cancellationToken).ConfigureAwait (false);

				var keepAlive = !CloseConnection && (effectiveFlags & (RequestFlags.KeepAlive | RequestFlags.CloseConnection)) == RequestFlags.KeepAlive;
				if (response != null) {
					response.Redirect = operation.RegisterRedirect (ctx, this, request.Path);
					return response;
				}

				cancellationToken.ThrowIfCancellationRequested ();

				HttpInstrumentationContent content;
				switch (TestRunner.EffectiveType) {
				case HttpInstrumentationTestType.NtlmWhileQueued:
					content = new HttpInstrumentationContent (TestRunner, currentRequest);
					return new HttpResponse (HttpStatusCode.OK, content);
				case HttpInstrumentationTestType.NtlmWhileQueued2:
					content = new HttpInstrumentationContent (TestRunner, currentRequest);
					return new HttpResponse (HttpStatusCode.OK, content) { CloseConnection = true };
				}

				var ret = await Target.HandleRequest (ctx, operation, connection, request, effectiveFlags, cancellationToken);
				ctx.LogDebug (3, $"{me} target done: {Target} {ret}");
				ret.KeepAlive = false;
				return ret;
			}

			async Task<HttpResponse> HandlePostChunked (
				TestContext ctx, HttpOperation operation, HttpConnection connection, HttpRequest request,
				RequestFlags effectiveFlags, CancellationToken cancellationToken)
			{
				var me = $"{ME}.{nameof (HandlePostChunked)}";
				ctx.LogDebug (3, $"{me}: {connection.RemoteEndPoint}");

				var firstChunk = await ChunkedContent.ReadChunk (ctx, request.Reader, cancellationToken).ConfigureAwait (false);
				ctx.LogDebug (3, $"{me} got first chunk: {firstChunk.Length}");

				ctx.Assert (firstChunk, Is.EqualTo (ConnectionHandler.TheQuickBrownFoxBuffer), "first chunk");

				readyTcs.TrySetResult (true);

				ctx.LogDebug (3, $"{me} reading remaining body");

				await ChunkedContent.Read (ctx, request.Reader, cancellationToken).ConfigureAwait (false);

				await TestRunner.HandleRequest (
					ctx, this, connection, request, AuthenticationState.None, cancellationToken).ConfigureAwait (false);

				return HttpResponse.CreateSuccess (ME);
			}

			internal Task WaitUntilReady ()
			{
				return readyTcs.Task;
			}

			protected internal override async Task<HttpResponse> HandleRequest (
				TestContext ctx, HttpOperation operation, HttpConnection connection, HttpRequest request,
				RequestFlags effectiveFlags, CancellationToken cancellationToken)
			{
				switch (TestRunner.EffectiveType) {
				case HttpInstrumentationTestType.ReuseConnection:
				case HttpInstrumentationTestType.CloseIdleConnection:
				case HttpInstrumentationTestType.CloseCustomConnectionGroup:
				case HttpInstrumentationTestType.LargeHeader:
				case HttpInstrumentationTestType.LargeHeader2:
				case HttpInstrumentationTestType.SendResponseAsBlob:
				case HttpInstrumentationTestType.ReuseAfterPartialRead:
				case HttpInstrumentationTestType.CustomConnectionGroup:
				case HttpInstrumentationTestType.ReuseCustomConnectionGroup:
				case HttpInstrumentationTestType.CloseRequestStream:
				case HttpInstrumentationTestType.ReadTimeout:
				case HttpInstrumentationTestType.AbortResponse:
				case HttpInstrumentationTestType.RedirectOnSameConnection:
				case HttpInstrumentationTestType.SimpleGZip:
				case HttpInstrumentationTestType.TestResponseStream:
				case HttpInstrumentationTestType.LargeChunkRead:
				case HttpInstrumentationTestType.LargeGZipRead:
				case HttpInstrumentationTestType.GZipWithLength:
				case HttpInstrumentationTestType.ResponseStreamCheckLength2:
				case HttpInstrumentationTestType.ResponseStreamCheckLength:
					ctx.Assert (request.Method, Is.EqualTo ("GET"), "method");
					break;

				case HttpInstrumentationTestType.ReuseConnection2:
				case HttpInstrumentationTestType.ServerAbortsPost:
					ctx.Assert (request.Method, Is.EqualTo ("POST"), "method");
					break;

				case HttpInstrumentationTestType.NtlmInstrumentation:
				case HttpInstrumentationTestType.NtlmClosesConnection:
				case HttpInstrumentationTestType.NtlmReusesConnection:
				case HttpInstrumentationTestType.ParallelNtlm:
				case HttpInstrumentationTestType.NtlmWhileQueued:
				case HttpInstrumentationTestType.NtlmWhileQueued2:
					return await HandleNtlmRequest (
						ctx, operation, connection, request, effectiveFlags, cancellationToken).ConfigureAwait (false);

				case HttpInstrumentationTestType.RedirectNoLength:
				case HttpInstrumentationTestType.PutChunked:
				case HttpInstrumentationTestType.PutChunkDontCloseRequest:
				case HttpInstrumentationTestType.ServerAbortsRedirect:
					break;

				case HttpInstrumentationTestType.EntityTooBig:
					await EntityTooBig ().ConfigureAwait (false);
					return null;

				case HttpInstrumentationTestType.PostChunked:
					return await HandlePostChunked (
						ctx, operation, connection, request, effectiveFlags, cancellationToken).ConfigureAwait (false);

				case HttpInstrumentationTestType.PostContentLength:
					await PostContentLength ().ConfigureAwait (false);
					break;

				case HttpInstrumentationTestType.ClientAbortsPost:
					await ClientAbortsPost ().ConfigureAwait (false);
					return null;

				default:
					throw ctx.AssertFail (TestRunner.EffectiveType);
				}

				RemoteEndPoint = connection.RemoteEndPoint;

				await TestRunner.HandleRequest (
					ctx, this, connection, request, AuthenticationState.None, cancellationToken).ConfigureAwait (false);

				HttpResponse response;
				HttpInstrumentationContent content;
				ListenerOperation redirect;

				switch (TestRunner.EffectiveType) {
				case HttpInstrumentationTestType.LargeHeader:
					response = new HttpResponse (HttpStatusCode.OK, Content);
					response.AddHeader ("LargeTest", ConnectionHandler.GetLargeText (100));
					return response;

				case HttpInstrumentationTestType.LargeHeader2:
					response = new HttpResponse (HttpStatusCode.OK, Content);
					response.AddHeader ("LargeTest", ConnectionHandler.GetLargeText (100));
					response.WriteAsBlob = true;
					return response;

				case HttpInstrumentationTestType.SendResponseAsBlob:
					return new HttpResponse (HttpStatusCode.OK, Content) {
						WriteAsBlob = true
					};

				case HttpInstrumentationTestType.ReuseAfterPartialRead:
					return new HttpResponse (HttpStatusCode.OK, Content) {
						WriteAsBlob = true
					};

				case HttpInstrumentationTestType.ReadTimeout:
				case HttpInstrumentationTestType.AbortResponse:
					content = new HttpInstrumentationContent (TestRunner, currentRequest);
					return new HttpResponse (HttpStatusCode.OK, content);

				case HttpInstrumentationTestType.ReuseConnection2:
					return new HttpResponse (HttpStatusCode.OK, Content);

				case HttpInstrumentationTestType.RedirectOnSameConnection:
					redirect = operation.RegisterRedirect (ctx, Target);
					response = HttpResponse.CreateRedirect (HttpStatusCode.Redirect, redirect);
					response.SetBody (new StringContent ($"{ME} Redirecting"));
					response.WriteAsBlob = true;
					return response;

				case HttpInstrumentationTestType.RedirectNoLength:
					redirect = operation.RegisterRedirect (ctx, Target);
					response = HttpResponse.CreateRedirect (HttpStatusCode.Redirect, redirect);
					response.NoContentLength = true;
					return response;

				case HttpInstrumentationTestType.ServerAbortsRedirect:
					if (IsSecondRequest)
						throw ctx.AssertFail ("Should never happen.");
					var cloned = new HttpInstrumentationHandler (this);
					cloned.IsSecondRequest = true;
					redirect = operation.RegisterRedirect (ctx, cloned);
					response = HttpResponse.CreateRedirect (HttpStatusCode.Redirect, redirect);
					return response;

				case HttpInstrumentationTestType.ServerAbortsPost:
					return new HttpResponse (HttpStatusCode.BadRequest, Content);

				case HttpInstrumentationTestType.SimpleGZip:
					var gzipContent = new GZipContent (ConnectionHandler.TheQuickBrownFoxBuffer);
					return new HttpResponse (HttpStatusCode.OK, gzipContent);

				case HttpInstrumentationTestType.TestResponseStream:
					response = new HttpResponse (HttpStatusCode.OK, Content);
					response.WriteAsBlob = true;
					return response;

				case HttpInstrumentationTestType.LargeChunkRead:
					response = new HttpResponse (HttpStatusCode.OK, Content);
					response.WriteBodyAsBlob = true;
					return response;

				case HttpInstrumentationTestType.LargeGZipRead:
					gzipContent = new GZipContent ((ChunkedContent)Content);
					response = new HttpResponse (HttpStatusCode.OK, gzipContent);
					return response;

				case HttpInstrumentationTestType.GZipWithLength:
					gzipContent = new GZipContent ((StringContent)Content);
					response = new HttpResponse (HttpStatusCode.OK, gzipContent);
					return response;

				case HttpInstrumentationTestType.ResponseStreamCheckLength2:
				case HttpInstrumentationTestType.ResponseStreamCheckLength:
					response = new HttpResponse (HttpStatusCode.OK, Content);
					return response;

				default:
					return HttpResponse.CreateSuccess (ME);
				}

				async Task EntityTooBig ()
				{
					await request.ReadHeaders (ctx, cancellationToken).ConfigureAwait (false);
					await ctx.AssertException<IOException> (() => request.Read (ctx, cancellationToken), "client doesn't send any body");
				}

				async Task ClientAbortsPost ()
				{
					await request.ReadHeaders (ctx, cancellationToken).ConfigureAwait (false);
					await ctx.AssertException<IOException> (() => request.Read (ctx, cancellationToken), "client doesn't send any body");
				}

				async Task PostContentLength ()
				{
					await request.ReadHeaders (ctx, cancellationToken).ConfigureAwait (false);
					ctx.Assert (request.ContentLength, Is.EqualTo (currentRequest.Content.Length), "request.ContentLength");
					readyTcs.TrySetResult (true);
					await request.Read (ctx, cancellationToken);
				}
			}

			public override bool CheckResponse (TestContext ctx, Response response)
			{
				if (Target != null)
					return Target.CheckResponse (ctx, response);

				HttpContent expectedContent = null;

				switch (TestRunner.EffectiveType) {
				case HttpInstrumentationTestType.ReadTimeout:
				case HttpInstrumentationTestType.AbortResponse:
					return ctx.Expect (response.Status, Is.EqualTo (HttpStatusCode.OK), "response.StatusCode");

				case HttpInstrumentationTestType.ReuseAfterPartialRead:
					if (!ctx.Expect (response.Content, Is.Not.Null, "response.Content != null"))
						return false;

					return ctx.Expect (response.Content.Length, Is.GreaterThan (0), "response.Content.Length");

				case HttpInstrumentationTestType.PutChunked:
				case HttpInstrumentationTestType.PutChunkDontCloseRequest:
					break;

				case HttpInstrumentationTestType.LargeChunkRead:
					expectedContent = Content.RemoveTransferEncoding ();
					break;

				case HttpInstrumentationTestType.LargeGZipRead:
				case HttpInstrumentationTestType.ResponseStreamCheckLength2:
					expectedContent = Content.RemoveTransferEncoding ();
					break;

				case HttpInstrumentationTestType.ResponseStreamCheckLength:
					expectedContent = Content;
					break;

				case HttpInstrumentationTestType.GZipWithLength:
					expectedContent = Content;
					break;
				}

				if (expectedContent == null)
					expectedContent = Content ?? new StringContent (ME);
				if (!ctx.Expect (response.Content, Is.Not.Null, "response.Content != null"))
					return false;

				return HttpContent.Compare (ctx, response.Content, expectedContent, false, "response.Content");
			}

			public override Task CheckResponse (TestContext ctx, Response response,
							    CancellationToken cancellationToken,
							    HttpStatusCode expectedStatus = HttpStatusCode.OK,
							    WebExceptionStatus expectedError = WebExceptionStatus.Success)
			{
				switch (TestRunner.EffectiveType) {
				case HttpInstrumentationTestType.EntityTooBig:
				default:
					return base.CheckResponse (ctx, response, cancellationToken, expectedStatus, expectedError);
				}
			}
		}
	}
}
