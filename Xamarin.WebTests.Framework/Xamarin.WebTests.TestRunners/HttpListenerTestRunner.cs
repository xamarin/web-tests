//
// HttpListenerTestRunner.cs
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
	using Server;

	[HttpListenerTestRunner]
	public class HttpListenerTestRunner : InstrumentationTestRunner
	{
		public HttpListenerTestType Type {
			get;
		}

		public HttpListenerTestType EffectiveType => GetEffectiveType (Type);

		static HttpListenerTestType GetEffectiveType (HttpListenerTestType type)
		{
			if (type == HttpListenerTestType.MartinTest)
				return MartinTest;
			return type;
		}

		public HttpListenerTestRunner (HttpServerProvider provider, HttpListenerTestType type)
			: base (provider, type.ToString ())
		{
			Type = type;
		}

		const HttpListenerTestType MartinTest = HttpListenerTestType.SimpleInstrumentation;

		static readonly (HttpListenerTestType type, HttpListenerTestFlags flags)[] TestRegistration = {
			(HttpListenerTestType.Simple, HttpListenerTestFlags.Working),
			(HttpListenerTestType.SimpleInstrumentation, HttpListenerTestFlags.Ignore),
		};

		public static IList<HttpListenerTestType> GetTestTypes (TestContext ctx, HttpServerTestCategory category)
		{
			if (category == HttpServerTestCategory.MartinTest)
				return new[] { MartinTest };

			var setup = DependencyInjector.Get<IConnectionFrameworkSetup> ();
			return TestRegistration.Where (t => Filter (t.flags)).Select (t => t.type).ToList ();

			bool Filter (HttpListenerTestFlags flags)
			{
				switch (category) {
				case HttpServerTestCategory.MartinTest:
					return false;
				case HttpServerTestCategory.HttpListener:
					if (flags == HttpListenerTestFlags.Working)
						return true;
					return false;
				default:
					throw ctx.AssertFail (category);
				}
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
			case HttpListenerTestType.Simple:
				handler = hello;
				break;
			case HttpListenerTestType.SimpleInstrumentation:
				handler = new HttpListenerInstrumentationHandler (this, true);
				break;
			default:
				throw ctx.AssertFail (EffectiveType);
			}

			return (handler, flags);
		}

		internal override InstrumentationOperation CreateOperation (
			TestContext ctx, Handler handler,
			InstrumentationOperationType type,
			HttpOperationFlags flags)
		{
			return new Operation (
				this, handler, type, flags,
				HttpStatusCode.OK, WebExceptionStatus.Success);
		}

		class Operation : InstrumentationOperation
		{
			new public HttpListenerTestRunner Parent => (HttpListenerTestRunner)base.Parent;

			public Operation (HttpListenerTestRunner parent, Handler handler,
					  InstrumentationOperationType type, HttpOperationFlags flags,
					  HttpStatusCode expectedStatus, WebExceptionStatus expectedError)
				: base (parent, $"{parent.EffectiveType}",
					handler, type, flags, expectedStatus, expectedError)
			{
			}

			protected override Request CreateRequest (TestContext ctx, Uri uri)
			{
				return new TraditionalRequest (uri);
			}

			protected override void ConfigureRequest (TestContext ctx, Uri uri, Request request)
			{
				switch (Parent.EffectiveType) {
				case HttpListenerTestType.SimpleInstrumentation:
					((TraditionalRequest)request).RequestExt.Host = "customhost";
					break;
				}

				Handler.ConfigureRequest (ctx, request, uri);

				request.SetProxy (Parent.Server.GetProxy ());
			}

			protected override Task<Response> RunInner (TestContext ctx, Request request, CancellationToken cancellationToken)
			{
				ctx.LogDebug (2, $"{ME} RUN INNER");
				return request.SendAsync (ctx, cancellationToken);
			}

			protected override void ConfigureNetworkStream (TestContext ctx, StreamInstrumentation instrumentation)
			{
			}

			protected override void Destroy ()
			{
				;
			}
		}

		class HttpListenerInstrumentationHandler : Handler
		{
			public HttpListenerTestRunner TestRunner {
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

			public HttpListenerInstrumentationHandler (HttpListenerTestRunner parent, bool closeConnection)

				: base (parent.EffectiveType.ToString ())
			{
				TestRunner = parent;
				ME = $"{GetType ().Name}({parent.EffectiveType})";
				CloseConnection = closeConnection;

				Flags = RequestFlags.KeepAlive;
				if (CloseConnection)
					Flags |= RequestFlags.CloseConnection;
			}

			HttpListenerInstrumentationHandler (HttpListenerInstrumentationHandler other)
				: base (other.Value)
			{
				TestRunner = other.TestRunner;
				ME = other.ME;
				CloseConnection = other.CloseConnection;
				Flags = other.Flags;
			}

			public override object Clone ()
			{
				return new HttpListenerInstrumentationHandler (this);
			}

			protected internal override async Task<HttpResponse> HandleRequest (
				TestContext ctx, HttpOperation operation, HttpConnection connection, HttpRequest request,
				RequestFlags effectiveFlags, CancellationToken cancellationToken)
			{
				await FinishedTask.ConfigureAwait (false);

				RemoteEndPoint = connection.RemoteEndPoint;

				var listenerContext = ((HttpListenerConnection)connection).Context;
				var listenerRequest = listenerContext.Request;

				HttpContent content;
				switch (TestRunner.EffectiveType) {
				case HttpListenerTestType.SimpleInstrumentation:
					var host = listenerRequest.Headers["Host"];
					ctx.LogMessage ($"{ME} - TEST: {host}");
					content = HttpContent.TheQuickBrownFox;
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
				case HttpListenerTestType.SimpleInstrumentation:
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
