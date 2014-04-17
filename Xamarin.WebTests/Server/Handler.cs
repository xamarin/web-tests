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
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace Xamarin.WebTests.Server
{
	using Framework;

	public abstract class Handler
	{
		static int debugLevel = 2;

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

		protected void WriteError (Connection connection, string message, params object[] args)
		{
			WriteSimpleResponse (connection, 500, "ERROR", string.Format (message, args));
		}

		protected void WriteSuccess (Connection connection, string body = null)
		{
			WriteSimpleResponse (connection, 200, "OK", body);
		}

		protected void Debug (int level, string message, params object[] args)
		{
			if (DebugLevel < level)
				return;
			var sb = new StringBuilder ();
			sb.AppendFormat ("{0}: {1}", this, message);
			for (int i = 0; i < args.Length; i++) {
				sb.Append (" ");
				sb.Append (args [i] != null ? args [i].ToString () : "<null>");
			}
			Console.Error.WriteLine (sb.ToString ());
		}

		protected void DumpHeaders (Connection connection)
		{
			if (DebugLevel < 2)
				return;
			foreach (var header in connection.Headers) {
				Console.Error.WriteLine ("  {0} = {1}", header.Key, header.Value);
			}
		}

		protected void WriteSimpleResponse (Connection connection, int status, string message, string body)
		{
			Debug (0, "RESPONSE", status, message, body);

			connection.ResponseWriter.WriteLine ("HTTP/1.1 {0} {1}", status, message);
			connection.ResponseWriter.WriteLine ("Content-Type: text/plain");
			if (body != null) {
				connection.ResponseWriter.WriteLine ("Content-Length: {0}", body.Length);
				connection.ResponseWriter.WriteLine ("");
				connection.ResponseWriter.WriteLine (body);
			} else {
				connection.ResponseWriter.WriteLine ("");
			}
		}

		protected void WriteRedirect (Connection connection, int code, Uri uri)
		{
			Debug (0, "REDIRECT", code, uri);

			connection.ResponseWriter.WriteLine ("HTTP/1.1 {0}", code);
			connection.ResponseWriter.WriteLine ("Location: {0}", uri);
			connection.ResponseWriter.WriteLine ();
		}

		public void HandleRequest (Connection connection)
		{
			try {
				Debug (0, "HANDLE REQUEST");
				DumpHeaders (connection);
				var success = HandleRequest (connection, Flags);
				if (success)
					WriteResponse (connection, Flags);
				Debug (0, "HANDLE REQUEST DONE", success);
				tcs.SetResult (success);
			} catch (Exception ex) {
				Debug (0, "HANDLE REQUEST EX", ex);
				WriteError (connection, "Caught unhandled exception", ex);
				tcs.SetException (ex);
			}
		}

		protected abstract void WriteResponse (Connection connection, RequestFlags effectiveFlags);

		protected internal abstract bool HandleRequest (Connection connection, RequestFlags effectiveFlags);

		internal Uri RegisterRequest (Listener listener)
		{
			lock (this) {
				if (hasRequest)
					throw new InvalidOperationException ();
				hasRequest = true;

				return listener.RegisterHandler (this);
			}
		}

		public virtual HttpWebRequest CreateRequest (Uri uri)
		{
			return (HttpWebRequest)HttpWebRequest.Create (uri);
		}

		public HttpWebRequest CreateRequest (Listener listener)
		{
			var uri = RegisterRequest (listener);
			return CreateRequest (uri);
		}

		public override string ToString ()
		{
			var padding = string.IsNullOrEmpty (Description) ? string.Empty : ": ";
			return string.Format ("[{0}{1}{2}]", GetType ().Name, padding, Description);
		}
	}
}

