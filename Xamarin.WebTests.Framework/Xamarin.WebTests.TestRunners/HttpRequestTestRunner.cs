//
// HttpRequestTestRunner.cs
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
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using Xamarin.AsyncTests;
using Xamarin.AsyncTests.Constraints;

namespace Xamarin.WebTests.TestRunners
{
	using ConnectionFramework;
	using HttpFramework;
	using HttpHandlers;
	using TestFramework;
	using TestAttributes;

	[HttpRequestTestRunner]
	public class HttpRequestTestRunner : InstrumentationTestRunner
	{
		public HttpRequestTestType Type {
			get;
		}

		public HttpRequestTestType EffectiveType => GetEffectiveType (Type);

		static HttpRequestTestType GetEffectiveType (HttpRequestTestType type)
		{
			if (type == HttpRequestTestType.MartinTest)
				return MartinTest;
			return type;
		}

		public HttpRequestTestRunner (HttpServerProvider provider, HttpRequestTestType type)
			: base (provider, type.ToString ())
		{
			Type = type;
		}

		const HttpRequestTestType MartinTest = HttpRequestTestType.Simple;

		static readonly (HttpRequestTestType type, HttpRequestTestFlags flags)[] TestRegistration = {
			(HttpRequestTestType.Simple, HttpRequestTestFlags.Working),
			(HttpRequestTestType.SimplePost, HttpRequestTestFlags.Working),
			(HttpRequestTestType.SimpleRedirect, HttpRequestTestFlags.Working),
			(HttpRequestTestType.PostRedirect, HttpRequestTestFlags.Working),
			(HttpRequestTestType.Get404, HttpRequestTestFlags.Working),
			(HttpRequestTestType.LargeHeader, HttpRequestTestFlags.Working),
			(HttpRequestTestType.LargeHeader2, HttpRequestTestFlags.Working),
			(HttpRequestTestType.SendResponseAsBlob, HttpRequestTestFlags.Working),
			(HttpRequestTestType.CloseRequestStream, HttpRequestTestFlags.Working),
			(HttpRequestTestType.ReadTimeout, HttpRequestTestFlags.NewWebStack),
			(HttpRequestTestType.RedirectNoReuse, HttpRequestTestFlags.Working),
			(HttpRequestTestType.RedirectNoLength, HttpRequestTestFlags.NewWebStack),
			(HttpRequestTestType.PutChunked, HttpRequestTestFlags.Working),
			(HttpRequestTestType.PutChunkDontCloseRequest, HttpRequestTestFlags.NewWebStack),
			(HttpRequestTestType.ServerAbortsRedirect, HttpRequestTestFlags.Unstable),
			(HttpRequestTestType.ServerAbortsPost, HttpRequestTestFlags.NewWebStack),
			(HttpRequestTestType.PostChunked, HttpRequestTestFlags.Working),
			(HttpRequestTestType.EntityTooBig, HttpRequestTestFlags.NewWebStack),
			(HttpRequestTestType.PostContentLength, HttpRequestTestFlags.Working),
			(HttpRequestTestType.ClientAbortsPost, HttpRequestTestFlags.NewWebStack),
			(HttpRequestTestType.GetChunked, HttpRequestTestFlags.Working),
			(HttpRequestTestType.SimpleGZip, HttpRequestTestFlags.Working),
			(HttpRequestTestType.TestResponseStream, HttpRequestTestFlags.Working),
			(HttpRequestTestType.LargeChunkRead, HttpRequestTestFlags.Working),
			(HttpRequestTestType.LargeGZipRead, HttpRequestTestFlags.GZip),
			(HttpRequestTestType.GZipWithLength, HttpRequestTestFlags.GZip),
			(HttpRequestTestType.ResponseStreamCheckLength2, HttpRequestTestFlags.GZip),
			(HttpRequestTestType.ResponseStreamCheckLength, HttpRequestTestFlags.GZip),
			(HttpRequestTestType.GetNoLength, HttpRequestTestFlags.Working),
			(HttpRequestTestType.ImplicitHost, HttpRequestTestFlags.Working),
			(HttpRequestTestType.CustomHost, HttpRequestTestFlags.Working),
			(HttpRequestTestType.CustomHostWithPort, HttpRequestTestFlags.Working),
			(HttpRequestTestType.CustomHostDefaultPort, HttpRequestTestFlags.Working),
		};

		public static IList<HttpRequestTestType> GetInstrumentationTypes (TestContext ctx, HttpServerTestCategory category)
		{
			if (category == HttpServerTestCategory.MartinTest)
				return new[] { MartinTest };

			var setup = DependencyInjector.Get<IConnectionFrameworkSetup> ();
			return TestRegistration.Where (t => Filter (t.flags)).Select (t => t.type).ToList ();

			bool Filter (HttpRequestTestFlags flags)
			{
				if (flags == HttpRequestTestFlags.GZip) {
					if (!setup.SupportsGZip)
						return false;
					flags = HttpRequestTestFlags.Working;
				}

				switch (category) {
				case HttpServerTestCategory.MartinTest:
					return false;
				case HttpServerTestCategory.Default:
					return flags == HttpRequestTestFlags.Working;
				case HttpServerTestCategory.Stress:
					return flags == HttpRequestTestFlags.Stress;
				case HttpServerTestCategory.NewWebStack:
					return flags == HttpRequestTestFlags.NewWebStack;
				case HttpServerTestCategory.Experimental:
					return flags == HttpRequestTestFlags.Unstable;
				default:
					throw ctx.AssertFail (category);
				}
			}
		}

		const int IdleTime = 750;

		protected override (Handler handler, HttpOperationFlags flags) CreateHandler (TestContext ctx, bool primary)
		{
			var hello = new HelloWorldHandler (EffectiveType.ToString ());
			var helloKeepAlive = new HelloWorldHandler (EffectiveType.ToString ()) {
				Flags = RequestFlags.KeepAlive
			};
			var postHello = new PostHandler (EffectiveType.ToString (), HttpContent.HelloWorld);
			var chunkedPost = new PostHandler (EffectiveType.ToString (), HttpContent.HelloChunked, TransferMode.Chunked);

			if (!primary)
				ctx.AssertFail ("Should never happen.");

			switch (EffectiveType) {
			case HttpRequestTestType.SimplePost:
				return (postHello, HttpOperationFlags.None);
			case HttpRequestTestType.SimpleRedirect:
				return (new RedirectHandler (hello, HttpStatusCode.Redirect), HttpOperationFlags.None);
			case HttpRequestTestType.PostRedirect:
				return (new RedirectHandler (postHello, HttpStatusCode.TemporaryRedirect), HttpOperationFlags.None);
			case HttpRequestTestType.Get404:
				return (new GetHandler (EffectiveType.ToString (), null, HttpStatusCode.NotFound), HttpOperationFlags.None);
			case HttpRequestTestType.RedirectNoReuse:
				return (new RedirectHandler (hello, HttpStatusCode.Redirect), HttpOperationFlags.None);
			case HttpRequestTestType.GetChunked:
				return (new GetHandler (EffectiveType.ToString (), HttpContent.HelloChunked), HttpOperationFlags.None);
			case HttpRequestTestType.Simple:
				return (hello, HttpOperationFlags.None);
			default:
				var handler = new HttpRequestHandler (this);
				return (handler, handler.OperationFlags);
			}
		}

		internal AuthenticationManager GetAuthenticationManager ()
		{
			var manager = new AuthenticationManager (AuthenticationType.NTLM, AuthenticationHandler.GetCredentials ());
			var old = Interlocked.CompareExchange (ref authManager, manager, null);
			return old ?? manager;
		}

		protected override InstrumentationOperation CreateOperation (
			TestContext ctx, Handler handler, InstrumentationOperationType type, HttpOperationFlags flags)
		{
			HttpStatusCode expectedStatus;
			WebExceptionStatus expectedError;

			switch (EffectiveType) {
			case HttpRequestTestType.CloseRequestStream:
				expectedStatus = HttpStatusCode.InternalServerError;
				expectedError = WebExceptionStatus.RequestCanceled;
				break;
			case HttpRequestTestType.ReadTimeout:
				expectedStatus = HttpStatusCode.InternalServerError;
				expectedError = WebExceptionStatus.Timeout;
				break;
			case HttpRequestTestType.Get404:
				expectedStatus = HttpStatusCode.NotFound;
				expectedError = WebExceptionStatus.ProtocolError;
				break;
			case HttpRequestTestType.ServerAbortsPost:
				expectedStatus = HttpStatusCode.BadRequest;
				expectedError = WebExceptionStatus.ProtocolError;
				break;
			case HttpRequestTestType.EntityTooBig:
			case HttpRequestTestType.ClientAbortsPost:
				expectedStatus = HttpStatusCode.InternalServerError;
				expectedError = WebExceptionStatus.AnyErrorStatus;
				break;
			default:
				expectedStatus = HttpStatusCode.OK;
				expectedError = WebExceptionStatus.Success;
				break;
			}

			return new Operation (this, handler, type, flags, expectedStatus, expectedError);
		}

		class Operation : InstrumentationOperation
		{
			new public HttpRequestTestRunner Parent => (HttpRequestTestRunner)base.Parent;

			public HttpRequestTestType EffectiveType => Parent.EffectiveType;

			public HttpRequestHandler InstrumentationHandler => (HttpRequestHandler)base.Handler;

			public Operation (HttpRequestTestRunner parent, Handler handler,
					  InstrumentationOperationType type, HttpOperationFlags flags,
					  HttpStatusCode expectedStatus, WebExceptionStatus expectedError)
				: base (parent, $"{parent.EffectiveType}:{type}",
					handler, type, flags, expectedStatus, expectedError)
			{
			}

			protected override Request CreateRequest (TestContext ctx, Uri uri)
			{
				var primary = Type == InstrumentationOperationType.Primary;
				if (Handler is HttpRequestHandler instrumentationHandler)
					return instrumentationHandler.CreateRequest (uri);

				return new TraditionalRequest (uri);
			}

			protected override void ConfigureRequest (TestContext ctx, Uri uri, Request request)
			{
				var traditionalRequest = (TraditionalRequest)request;

				traditionalRequest.RequestExt.ReadWriteTimeout = int.MaxValue;
				traditionalRequest.RequestExt.Timeout = int.MaxValue;

				switch (EffectiveType) {
				case HttpRequestTestType.SimplePost:
					request.SetContentLength(((PostHandler)Handler).Content.Length);
					break;
				}

				Handler.ConfigureRequest (request, uri);

				request.SetProxy (Parent.Server.GetProxy ());
			}

			protected override Task<Response> RunInner (TestContext ctx, Request request, CancellationToken cancellationToken)
			{
				ctx.LogDebug (2, $"{ME} RUN INNER");
				switch (EffectiveType) {
				case HttpRequestTestType.ServerAbortsPost:
					return ((TraditionalRequest)request).Send (ctx, cancellationToken);
				default:
					return ((TraditionalRequest)request).SendAsync (ctx, cancellationToken);
				}
			}

			protected override void ConfigureNetworkStream (TestContext ctx, StreamInstrumentation instrumentation)
			{
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
