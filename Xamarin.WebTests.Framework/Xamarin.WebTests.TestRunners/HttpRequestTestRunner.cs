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
		public IHttpRequestTestInstance Instance {
			get;
		}

		public HttpRequestTestRunner (HttpServerProvider provider, IHttpRequestTestInstance instance)
			: base (provider, instance.Value)
		{
			Instance = instance;
		}

		const int IdleTime = 750;

		protected override (Handler handler, HttpOperationFlags flags) CreateHandler (TestContext ctx, bool primary)
		{
			var instanceHandler = Instance.CreateHandler (ctx, this);
			return (instanceHandler, Instance.OperationFlags);
		}

		internal AuthenticationManager GetAuthenticationManager ()
		{
			var manager = new AuthenticationManager (AuthenticationType.NTLM, AuthenticationHandler.GetCredentials ());
			var old = Interlocked.CompareExchange (ref authManager, manager, null);
			return old ?? manager;
		}

		internal override InstrumentationOperation CreateOperation (
			TestContext ctx, Handler handler,
			InstrumentationOperationType type,
			HttpOperationFlags flags)
		{
			return new Operation (
				this, handler, type, flags,
				Instance.ExpectedStatus, Instance.ExpectedError);
		}

		class Operation : InstrumentationOperation
		{
			new public HttpRequestTestRunner Parent => (HttpRequestTestRunner)base.Parent;

			public Operation (HttpRequestTestRunner parent, Handler handler,
					  InstrumentationOperationType type, HttpOperationFlags flags,
					  HttpStatusCode expectedStatus, WebExceptionStatus expectedError)
				: base (parent, parent.ME, handler, type, flags,
				        expectedStatus, expectedError)
			{
			}

			protected override Request CreateRequest (TestContext ctx, Uri uri)
			{
				return Parent.Instance.CreateRequest (ctx, uri, Handler);
			}

			protected override void ConfigureRequest (TestContext ctx, Uri uri, Request request)
			{
				var traditionalRequest = (TraditionalRequest)request;

				traditionalRequest.RequestExt.ReadWriteTimeout = int.MaxValue;
				traditionalRequest.RequestExt.Timeout = int.MaxValue;

				Parent.Instance?.ConfigureRequest (ctx, uri, Handler, request);

				Handler.ConfigureRequest (ctx, request, uri);

				request.SetProxy (Parent.Server.GetProxy ());
			}

			protected override Task<Response> RunInner (TestContext ctx, Request request, CancellationToken cancellationToken)
			{
				ctx.LogDebug (2, $"{ME} RUN INNER");
				return ((TraditionalRequest)request).SendAsync (ctx, cancellationToken);
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
