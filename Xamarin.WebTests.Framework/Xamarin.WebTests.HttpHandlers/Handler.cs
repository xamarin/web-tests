//
// Handler.cs
//
// Author:
//       Martin Baulig <martin.baulig@xamarin.com>
//
// Copyright (c) 2014 Xamarin Inc. (http://www.xamarin.com)
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
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using Xamarin.AsyncTests;
using Xamarin.AsyncTests.Constraints;

namespace Xamarin.WebTests.HttpHandlers
{
	using ConnectionFramework;
	using HttpFramework;

	public abstract class Handler : Xamarin.AsyncTests.ICloneable, ITestParameter
	{
		static int next_id;
		public int ID => Interlocked.Increment (ref next_id);

		public RequestFlags Flags {
			get { return flags; }
			set {
				flags = value;
			}
		}

		public string Value {
			get;
		}

		string ITestParameter.FriendlyValue => Value;

		RequestFlags flags;

		public Handler (Handler parent, string identifier = null)
		{
			Value = identifier;
			Parent = parent;
		}

		public Handler (string identifier)
			: this (null, identifier)
		{
		}

		public Handler Parent {
			get;
		}

		protected internal static readonly Task CompletedTask = Task.FromResult (0);

		protected void Debug (TestContext ctx, int level, string message, params object[] args)
		{
			var sb = new StringBuilder ();
			sb.AppendFormat ("{0}: {1}", this, message);
			for (int i = 0; i < args.Length; i++) {
				sb.Append (" ");
				sb.Append (args [i] != null ? args [i].ToString () : "<null>");
			}
			ctx.LogDebug (level, sb.ToString ());
		}

		void DumpHeaders (TestContext ctx, HttpMessage message)
		{
			var sb = new StringBuilder ();
			foreach (var header in message.Headers) {
				sb.AppendFormat ("  {0} = {1}", header.Key, header.Value);
				sb.AppendLine ();
			}
			ctx.LogDebug (2, sb.ToString ());
		}

		public async Task<HttpResponse> HandleRequest (TestContext ctx, HttpOperation operation,
		                                               HttpConnection connection, HttpRequest request,
		                                               CancellationToken cancellationToken)
		{
			Exception originalError;
			HttpResponse response;

			if (operation == null)
				throw new ArgumentNullException (nameof (operation));
			if (connection == null)
				throw new ArgumentNullException (nameof (connection));

			var expectServerError = operation.HasAnyFlags (HttpOperationFlags.ExpectServerException);

			try {
				Debug (ctx, 1, $"HANDLE REQUEST: {connection.RemoteEndPoint}");
				DumpHeaders (ctx, request);
				connection.Server.CheckEncryption (ctx, connection.SslStream);
				response = await HandleRequest (ctx, operation, connection, request, Flags, cancellationToken);

				if (response == null)
					response = HttpResponse.CreateSuccess ();
				if ((Flags & RequestFlags.CloseConnection) != 0)
					response.CloseConnection = true;
				if (!response.KeepAlive.HasValue && ((Flags & RequestFlags.KeepAlive) != 0))
					response.KeepAlive = true;
				response.ResolveHeaders ();

				Debug (ctx, 1, $"HANDLE REQUEST DONE: {connection.RemoteEndPoint}", response);
				DumpHeaders (ctx, response);
				return response;
			} catch (AssertionException ex) {
				originalError = ex;
				response = HttpResponse.CreateError (ex.Message);
			} catch (OperationCanceledException) {
				throw;
			} catch (Exception ex) {
				originalError = ex;
				response = HttpResponse.CreateError ("Caught unhandled exception", ex);
			}

			if (ctx.IsCanceled || cancellationToken.IsCancellationRequested) {
				Debug (ctx, 1, "HANDLE REQUEST - CANCELED");
				throw new OperationCanceledException ();
			}

			if (originalError is AssertionException)
				Debug (ctx, 1, "HANDLE REQUEST - ASSERTION FAILED", originalError);
			else if (expectServerError)
				Debug (ctx, 1, "HANDLE REQUEST - EXPECTED ERROR", originalError.GetType ());
			else
				Debug (ctx, 1, "HANDLE REQUEST - ERROR", originalError);

			return response;
		}

		[StackTraceEntryPoint]
		protected internal abstract Task<HttpResponse> HandleRequest (
			TestContext ctx, HttpOperation operation, HttpConnection connection,
			HttpRequest request, RequestFlags effectiveFlags,
			CancellationToken cancellationToken);

		public virtual void ConfigureRequest (TestContext ctx, Request request, Uri uri)
		{
		}

		public abstract object Clone ();

		public virtual async Task CheckResponse (
			TestContext ctx, Response response, CancellationToken cancellationToken,
			HttpStatusCode expectedStatus = HttpStatusCode.OK, WebExceptionStatus expectedError = WebExceptionStatus.Success)
		{
			Debug (ctx, 1, "GOT RESPONSE", response.Status, response.IsSuccess, response.Error?.Message);

			await CompletedTask.ConfigureAwait (false);

			if (ctx.HasPendingException)
				return;

			if (cancellationToken.IsCancellationRequested) {
				ctx.OnTestCanceled ();
				return;
			}

			if (expectedError != WebExceptionStatus.Success) {
				ctx.Expect (response.Error, Is.Not.Null, "expecting exception");
				ctx.Expect (response.Status, Is.EqualTo (expectedStatus));
				var wexc = response.Error as WebException;
				ctx.Expect (wexc, Is.Not.Null, "WebException");
				if (expectedError != WebExceptionStatus.AnyErrorStatus)
					ctx.Expect ((WebExceptionStatus)wexc.Status, Is.EqualTo (expectedError));
				return;
			}

			if (response.Error != null) {
				if (response.Content != null)
					ctx.OnError (new WebException (response.Content.AsString (), response.Error));
				else
					ctx.OnError (response.Error);
			} else {
				var ok = ctx.Expect (response.Status, Is.EqualTo (expectedStatus), "status code");
				if (ok)
					ok &= ctx.Expect (response.IsSuccess, Is.True, "success status");

				if (ok)
					ok &= CheckResponse (ctx, response);
			}

			if (response.Content != null)
				Debug (ctx, 5, "GOT RESPONSE BODY", response.Content);
		}

		public abstract bool CheckResponse (TestContext ctx, Response response);

		public override string ToString ()
		{
			var padding = string.IsNullOrEmpty (Value) ? string.Empty : ": ";
			return $"[{GetType ().Name}:{ID}{padding}{Value}]";
		}
	}
}

