//
// Listener.cs
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
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using Xamarin.AsyncTests;

namespace Xamarin.WebTests.Server
{
	using ConnectionFramework;
	using HttpFramework;
	using TestFramework;

	abstract class Listener : IDisposable
	{
		Dictionary<string, ListenerOperation> registry;
		volatile bool disposed;

		static int nextID;
		static long nextRequestID;

		public readonly int ID = ++nextID;

		internal TestContext TestContext {
			get;
		}

		internal ListenerBackend Backend {
			get;
		}

		internal HttpServer Server {
			get;
		}

		internal string ME {
			get;
		}

		public Listener (TestContext ctx, HttpServer server, ListenerBackend backend)
		{
			TestContext = ctx;
			Server = server;
			Backend = backend;
			ME = $"{GetType ().Name}({ID})";
			registry = new Dictionary<string, ListenerOperation> ();
		}

		protected internal string FormatConnection (HttpConnection connection)
		{
			return $"[{ME}:{connection.ME}]";
		}

		protected void Debug (string message)
		{
			TestContext.LogDebug (5, $"{ME}: {message}");
		}

		protected abstract ListenerOperation CreateOperation (HttpOperation operation, Uri uri);

		public ListenerOperation RegisterOperation (TestContext ctx, HttpOperation operation)
		{
			lock (this) {
				var id = Interlocked.Increment (ref nextRequestID);
				var path = $"/id/{operation.ID}/{operation.Handler.GetType ().Name}/";
				var uri = new Uri (Server.TargetUri, path);
				var listenerOperation = CreateOperation (operation, uri);
				registry.Add (path, listenerOperation);
				return listenerOperation;
			}
		}

		protected ListenerOperation GetOperation (ListenerContext context, HttpRequest request)
		{
			lock (this) {
				var me = $"{nameof (GetOperation)}({context.Connection.ME})";
				Debug ($"{me} {request.Method} {request.Path} {request.Protocol}");

				var operation = registry[request.Path];
				if (operation == null) {
					Debug ($"{me} INVALID PATH: {request.Path}!");
					return null;
				}

				registry.Remove (request.Path);
				return operation;
			}
		}

		protected abstract void Close ();

		public void Dispose ()
		{
			lock (this) {
				if (disposed)
					return;
				disposed = true;
				Close ();
				Backend.Dispose ();
			}
		}

	}
}
