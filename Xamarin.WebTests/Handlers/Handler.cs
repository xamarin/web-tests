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
// using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;
using Xamarin.AsyncTests;

namespace Xamarin.WebTests.Handlers
{
	using Framework;
	using Server;

	public abstract class Handler : Xamarin.AsyncTests.ICloneable
	{
		static int next_id;
		public readonly int ID = ++next_id;

		static int debugLevel = 10;

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

		public string Description {
			get; set;
		}

		public Task<bool> Task {
			get { return tcs.Task; }
		}

		bool hasRequest;
		RequestFlags flags;
		TaskCompletionSource<bool> tcs;

		protected void WantToModify ()
		{
			if (hasRequest)
				throw new InvalidOperationException ();
		}

		public Handler ()
		{
			tcs = new TaskCompletionSource<bool> ();
		}

		public InvocationContext Context {
			get;
			private set;
		}

		protected void Debug (int level, string message, params object[] args)
		{
			if (DebugLevel < level)
				return;
			if (Context == null)
				throw new InvalidOperationException ();
			var sb = new StringBuilder ();
			sb.AppendFormat ("{0}: {1}", this, message);
			for (int i = 0; i < args.Length; i++) {
				sb.Append (" ");
				sb.Append (args [i] != null ? args [i].ToString () : "<null>");
			}
			Context.LogDebug (level, sb.ToString ());
		}

		protected void DumpHeaders (HttpMessage message)
		{
			if (DebugLevel < 2)
				return;
			foreach (var header in message.Headers) {
				Context.LogMessage (string.Format ("  {0} = {1}", header.Key, header.Value));
			}
		}

		public bool HandleRequest (HttpConnection connection, HttpRequest request)
		{
			try {
				Debug (1, "HANDLE REQUEST");
				DumpHeaders (request);
				var response = HandleRequest (connection, request, Flags);
				if (response == null)
					response = HttpResponse.CreateSuccess ();
				if (!response.KeepAlive.HasValue && ((Flags & RequestFlags.KeepAlive) != 0))
					response.KeepAlive = true;
				request.ReadBody ();
				connection.WriteResponse (response);
				Debug (1, "HANDLE REQUEST DONE", response);
				DumpHeaders (response);
				tcs.SetResult (response.IsSuccess);
				return response.KeepAlive ?? false;
			} catch (Exception ex) {
				Debug (0, "HANDLE REQUEST EX", ex);
				var response = HttpResponse.CreateError ("Caught unhandled exception", ex);
				connection.WriteResponse (response);
				tcs.SetException (ex);
				return false;
			}
		}

		protected internal abstract HttpResponse HandleRequest (HttpConnection connection, HttpRequest request, RequestFlags effectiveFlags);

		public virtual void Register (InvocationContext context)
		{
			lock (this) {
				if (Context != null && Context != context)
					throw new InvalidOperationException ();
				Context = context;
			}
		}

		internal Uri RegisterRequest (HttpListener listener)
		{
			lock (this) {
				if (hasRequest)
					throw new InvalidOperationException ();
				hasRequest = true;

				return listener.RegisterHandler (this);
			}
		}

		internal virtual void Reset ()
		{
			Context = null;
			hasRequest = false;
			tcs = new TaskCompletionSource<bool> ();
		}

		public abstract Request CreateRequest (Uri uri);

		public Request CreateRequest (HttpListener listener)
		{
			var uri = RegisterRequest (listener);
			return CreateRequest (uri);
		}

		public abstract object Clone ();

		public override string ToString ()
		{
			var padding = string.IsNullOrEmpty (Description) ? string.Empty : ": ";
			return string.Format ("[{0}{1}{2}]", GetType ().Name, padding, Description);
		}
	}
}

