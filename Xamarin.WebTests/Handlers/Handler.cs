//
// Behavior.cs
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
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;
using Xamarin.AsyncTests;

namespace Xamarin.WebTests.Handlers
{
	using Framework;

	public abstract class Handler : Xamarin.AsyncTests.ICloneable, ITestFilter
	{
		static int next_id;
		public readonly int ID = ++next_id;

		static int debugLevel = 0;

		public static int DebugLevel {
			get { return debugLevel; }
			set { debugLevel = value; }
		}

		public RequestFlags Flags {
			get { return flags; }
			set {
				WantToModify ();
				flags = value;
			}
		}

		public Func<TestContext, bool> Filter {
			get { return filter; }
			set {
				WantToModify ();
				filter = value;
			}
		}

		bool ITestFilter.Filter (TestContext ctx, out bool enabled)
		{
			if (filter == null) {
				enabled = true;
				return false;
			}

			enabled = filter (ctx);
			return true;
		}

		public string Description {
			get; set;
		}

		public Task<bool> Task {
			get { return tcs.Task; }
		}

		Func<TestContext, bool> filter;

		bool hasRequest;
		RequestFlags flags;
		TaskCompletionSource<bool> tcs;

		Handler parent;
		TestContext context;

		protected void WantToModify ()
		{
			if (hasRequest)
				throw new InvalidOperationException ();
		}

		public Handler (Handler parent = null)
		{
			this.parent = parent;

			tcs = new TaskCompletionSource<bool> ();
		}

		public Handler Parent {
			get { return parent; }
		}

		TestContext GetContext ()
		{
			if (parent != null)
				return parent.GetContext ();
			else if (context == null)
				throw new InvalidOperationException ();
			return context;
		}
			
		protected void Debug (TestContext ctx, int level, string message, params object[] args)
		{
			if (DebugLevel < level)
				return;
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
			if (DebugLevel < 2)
				return;
			var sb = new StringBuilder ();
			foreach (var header in message.Headers) {
				sb.AppendFormat ("  {0} = {1}", header.Key, header.Value);
				sb.AppendLine ();
			}
			ctx.LogDebug (2, sb.ToString ());
		}

		public bool HandleRequest (HttpConnection connection, HttpRequest request)
		{
			var ctx = GetContext ();
			if (ctx == null)
				throw new InvalidOperationException ();

			try {
				Debug (ctx, 1, "HANDLE REQUEST");
				DumpHeaders (ctx, request);
				var response = HandleRequest (ctx, connection, request, Flags);
				if (response == null)
					response = HttpResponse.CreateSuccess ();
				if (!response.KeepAlive.HasValue && ((Flags & RequestFlags.KeepAlive) != 0))
					response.KeepAlive = true;
				request.ReadBody ();
				connection.WriteResponse (response);
				Debug (ctx, 1, "HANDLE REQUEST DONE", response);
				DumpHeaders (ctx, response);
				tcs.SetResult (response.IsSuccess);
				return response.KeepAlive ?? false;
			} catch (AssertionException ex) {
				Debug (ctx, 1, "HANDLE REQUEST - ASSERTION FAILED", ex);
				var response = HttpResponse.CreateError (ex.Message);
				connection.WriteResponse (response);
				tcs.SetException (ex);
				return false;
			} catch (Exception ex) {
				Debug (ctx, 0, "HANDLE REQUEST EX", ex);
				var response = HttpResponse.CreateError ("Caught unhandled exception", ex);
				connection.WriteResponse (response);
				tcs.SetException (ex);
				return false;
			}
		}

		[StackTraceEntryPoint]
		protected internal abstract HttpResponse HandleRequest (TestContext ctx, HttpConnection connection, HttpRequest request, RequestFlags effectiveFlags);

		public Task<T> RunWithContext<T> (TestContext ctx, HttpServer server, Func<Uri, Task<T>> func)
		{
			var uri = RegisterRequest (server);
			return RunWithContext (ctx, uri, func);
		}

		async Task<T> RunWithContext<T> (TestContext ctx, Uri uri, Func<Uri, Task<T>> func)
		{
			if (parent != null)
				return await parent.RunWithContext (ctx, uri, func);

			try {
				this.context = ctx;
				return await func (uri);
			} finally {
				this.context = null;
			}
		}

		internal Uri RegisterRequest (HttpServer server)
		{
			lock (this) {
				if (hasRequest)
					throw new InvalidOperationException ();
				hasRequest = true;

				return server.RegisterHandler (this);
			}
		}

		public virtual void ConfigureRequest (Request request, Uri uri)
		{
		}

		public abstract object Clone ();

		public override string ToString ()
		{
			var padding = string.IsNullOrEmpty (Description) ? string.Empty : ": ";
			return string.Format ("[{0}{1}{2}]", GetType ().Name, padding, Description);
		}
	}
}

