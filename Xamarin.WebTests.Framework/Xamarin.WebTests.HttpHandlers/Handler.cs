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
	using Server;

	public abstract class Handler
		: Xamarin.AsyncTests.ICloneable, ListenerHandler, ITestParameter
	{
		static int next_id;
		public int ID => Interlocked.Increment (ref next_id);

		RequestFlags ListenerHandler.RequestFlags => Flags;

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

		[StackTraceEntryPoint]
		public abstract Task<HttpResponse> HandleRequest (
			TestContext ctx, HttpOperation operation, HttpConnection connection,
			HttpRequest request, RequestFlags effectiveFlags,
			CancellationToken cancellationToken);

		void ListenerHandler.ConfigureRequest (
			TestContext ctx, HttpOperation operation,
			Request request, Uri uri)
		{
			ConfigureRequest (ctx, request, uri);
		}

		public virtual void ConfigureRequest (
			TestContext ctx, Request request, Uri uri)
		{
		}

		public abstract object Clone ();

		public abstract bool CheckResponse (TestContext ctx, Response response);

		public override string ToString ()
		{
			var padding = string.IsNullOrEmpty (Value) ? string.Empty : ": ";
			return $"[{GetType ().Name}:{ID}{padding}{Value}]";
		}
	}
}

