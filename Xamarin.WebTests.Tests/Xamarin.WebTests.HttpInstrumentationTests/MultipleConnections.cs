//
// MultipleConnections.cs
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
	using HttpFramework;
	using HttpHandlers;
	using TestRunners;

	public class MultipleConnections : HttpInstrumentationTestFixture
	{
		public sealed override HttpOperationFlags OperationFlags {
			get;
		}

		public sealed override RequestFlags RequestFlags {
			get;
		}

		public sealed override HttpContent ExpectedContent {
			get;
		}

		public ConnectionType Type {
			get;
		}

		public Handler Target {
			get;
		}

		[AsyncTest]
		public MultipleConnections (ConnectionType type)
		{
			Type = type;

			switch (type) {
			case ConnectionType.CustomConnectionGroup:
				OperationFlags = HttpOperationFlags.DontReuseConnection | HttpOperationFlags.ForceNewConnection;
				RequestFlags = RequestFlags.KeepAlive;
				ExpectedContent = new StringContent (ME);
				break;
			case ConnectionType.RedirectOnSameConnection:
				OperationFlags = HttpOperationFlags.None;
				RequestFlags = RequestFlags.KeepAlive;
				Target = HelloWorldHandler.GetSimple (ME);
				ExpectedContent = new StringContent (((HelloWorldHandler)Target).Message);
				break;
			default:
				OperationFlags = HttpOperationFlags.None;
				RequestFlags = RequestFlags.KeepAlive;
				ExpectedContent = new StringContent (ME);
				break;
			}
		}

		public enum ConnectionType
		{
			CustomConnectionGroup,
			ReuseCustomConnectionGroup,
			CloseCustomConnectionGroup,
			ReuseConnection,
			ReuseConnection2,
			RedirectOnSameConnection
		}

		protected override void ConfigurePrimaryRequest (
			TestContext ctx, InstrumentationOperation operation,
			TraditionalRequest request)
		{
			switch (Type) {
			case ConnectionType.CustomConnectionGroup:
			case ConnectionType.ReuseCustomConnectionGroup:
				request.RequestExt.ConnectionGroupName = "custom";
				break;
			case ConnectionType.ReuseConnection2:
				request.Method = "POST";
				request.SetContentType ("text/plain");
				request.Content = HttpContent.HelloWorld;
				break;
			}

			base.ConfigurePrimaryRequest (ctx, operation, request);
		}

		protected override void ConfigureSecondaryRequest (
			TestContext ctx, InstrumentationOperation operation,
			TraditionalRequest request)
		{
			switch (Type) {
			case ConnectionType.CustomConnectionGroup:
			case ConnectionType.ReuseConnection:
				break;
			case ConnectionType.ReuseConnection2:
				request.Method = "POST";
				request.SetContentType ("text/plain");
				request.Content = HttpContent.HelloWorld;
				break;
			case ConnectionType.ReuseCustomConnectionGroup:
				request.RequestExt.ConnectionGroupName = "custom";
				break;
			default:
				throw ctx.AssertFail (Type);
			}
			base.ConfigureSecondaryRequest (ctx, operation, request);
		}

		protected override bool ConfigureNetworkStream (
			TestContext ctx, StreamInstrumentation instrumentation)
		{
			switch (Type) {
			case ConnectionType.CloseCustomConnectionGroup:
				instrumentation.IgnoreErrors = true;
				break;
			}
			return false;
		}

		public override Task<HttpResponse> HandleRequest (
			TestContext ctx, InstrumentationOperation operation,
			HttpConnection connection, HttpRequest request,
			RequestFlags effectiveFlags, CancellationToken cancellationToken)
		{
			HttpResponse response;
			switch (Type) {
			case ConnectionType.CustomConnectionGroup:
				ctx.Assert (request.Method, Is.EqualTo ("GET"), "method");
				AssertNotReusingConnection (ctx, operation, connection);
				break;
			case ConnectionType.CloseCustomConnectionGroup:
				ctx.Assert (request.Method, Is.EqualTo ("GET"), "method");
				break;
			case ConnectionType.ReuseCustomConnectionGroup:
			case ConnectionType.ReuseConnection:
				ctx.Assert (request.Method, Is.EqualTo ("GET"), "method");
				AssertReusingConnection (ctx, operation, connection);
				break;
			case ConnectionType.ReuseConnection2:
				ctx.Assert (request.Method, Is.EqualTo ("POST"), "method");
				AssertReusingConnection (ctx, operation, connection);
				break;
			case ConnectionType.RedirectOnSameConnection:
				ctx.Assert (request.Method, Is.EqualTo ("GET"), "method");
				AssertReusingConnection (ctx, operation, connection);
				response = HttpResponse.CreateRedirect (
					ctx, HttpStatusCode.Redirect, operation, Target);
				response.SetBody (new StringContent ($"{ME} Redirecting"));
				response.WriteAsBlob = true;
				return Task.FromResult (response);

			default:
				throw ctx.AssertFail (Type);
			}

			response = new HttpResponse (HttpStatusCode.OK, ExpectedContent);
			if (operation.Type != InstrumentationOperationType.Primary)
				response.CloseConnection = true;
			return Task.FromResult (response);
		}

		protected override async Task<HttpOperation> RunSecondary (
			TestContext ctx, CancellationToken cancellationToken)
		{
			await FinishedTask.ConfigureAwait (false);

			switch (Type) {
			case ConnectionType.CustomConnectionGroup:
			case ConnectionType.ReuseCustomConnectionGroup:
			case ConnectionType.ReuseConnection:
			case ConnectionType.ReuseConnection2:
				return StartSecond (ctx, cancellationToken);
			case ConnectionType.CloseCustomConnectionGroup:
				ctx.LogDebug (5, $"{ME}: active connections: {PrimaryOperation.ServicePoint.CurrentConnections}");
				PrimaryOperation.ServicePoint.CloseConnectionGroup (((TraditionalRequest)PrimaryOperation.Request).RequestExt.ConnectionGroupName);
				ctx.LogDebug (5, $"{ME}: active connections #1: {PrimaryOperation.ServicePoint.CurrentConnections}");
				return null;
			case ConnectionType.RedirectOnSameConnection:
				return null;
			default:
				throw ctx.AssertFail (Type);
			}
		}

		HttpOperation StartSecond (
			TestContext ctx, CancellationToken cancellationToken)
		{
			var operation = CreateOperation (
				ctx, InstrumentationOperationType.Parallel,
				OperationFlags);
			operation.Start (ctx, cancellationToken);
			return operation;
		}
	}
}
