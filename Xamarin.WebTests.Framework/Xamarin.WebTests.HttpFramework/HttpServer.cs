//
// HttpServer.cs
//
// Author:
//       Martin Baulig <martin.baulig@xamarin.com>
//
// Copyright (c) 2017 Xamarin Inc. (http://www.xamarin.com)
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
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Security;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xamarin.AsyncTests;
using Xamarin.AsyncTests.Portable;
using Xamarin.AsyncTests.Constraints;
using Xamarin.WebTests.ConnectionFramework;
using Xamarin.WebTests.TestFramework;
using Xamarin.WebTests.HttpHandlers;
using Xamarin.WebTests.Server;

namespace Xamarin.WebTests.HttpFramework {
	[HttpServer]
	[FriendlyName ("HttpServer")]
	public abstract class HttpServer : ITestInstance {
		public HttpServerFlags Flags {
			get;
		}

		public abstract Uri Uri {
			get;
		}

		public abstract Uri TargetUri {
			get;
		}

		public IHttpServerDelegate Delegate {
			get; set;
		}

		public IPortableEndPoint ListenAddress {
			get;
		}

		public ConnectionParameters Parameters {
			get;
		}

		public ISslStreamProvider SslStreamProvider {
			get;
		}

		public bool UseSSL => SslStreamProvider != null;

		public HttpServer (IPortableEndPoint listenAddress, HttpServerFlags flags,
		                   ConnectionParameters parameters, ISslStreamProvider sslStreamProvider)
		{
			ListenAddress = listenAddress;
			Flags = flags;
			Parameters = parameters;
			SslStreamProvider = sslStreamProvider;

			if (Parameters != null)
				Flags |= HttpServerFlags.SSL;

			if ((Flags & HttpServerFlags.SSL) != 0) {
				if (SslStreamProvider == null) {
					var factory = DependencyInjector.Get<ConnectionProviderFactory> ();
					SslStreamProvider = factory.DefaultSslStreamProvider;
				}
			}
		}

		public abstract IWebProxy GetProxy ();

		public bool ReuseConnection {
			get { return (Flags & HttpServerFlags.ReuseConnection) != 0; }
		}

		#region ITestInstance implementation

		int initialized;

		public async Task Initialize (TestContext ctx, CancellationToken cancellationToken)
		{
			if (Interlocked.CompareExchange (ref initialized, 1, 0) != 0)
				throw new InternalErrorException ();

			ctx.LogDebug (5, "Initialize {0}: {1}", this, Flags);

			if (ReuseConnection)
				await Start (ctx, cancellationToken);
		}

		public async Task PreRun (TestContext ctx, CancellationToken cancellationToken)
		{
			if (!ReuseConnection)
				await Start (ctx, cancellationToken);
		}

		public async Task PostRun (TestContext ctx, CancellationToken cancellationToken)
		{
			if (!ReuseConnection)
				await Stop (ctx, cancellationToken);
		}

		public async Task Destroy (TestContext ctx, CancellationToken cancellationToken)
		{
			if (ReuseConnection)
				await Stop (ctx, cancellationToken);

			ctx.LogDebug (5, "Destroy {0}: {1}", this, Flags);

			Interlocked.Exchange (ref initialized, 0);
		}

		public abstract Task Start (TestContext ctx, CancellationToken cancellationToken);

		public abstract Task Stop (TestContext ctx, CancellationToken cancellationToken);

		#endregion

		internal async Task<bool> InitializeConnection (TestContext ctx, HttpConnection connection, CancellationToken cancellationToken)
		{
			++countRequests;
			var initTask = connection.Initialize (ctx, cancellationToken);
			if (Delegate == null) {
				await initTask.ConfigureAwait (false);
				return true;
			}

			return await Delegate.CheckCreateConnection (ctx, connection, initTask, cancellationToken);
		}

		public int CountRequests => countRequests;

		static long nextId;
		int countRequests;

		public Uri RegisterHandler (Handler handler)
		{
			var path = string.Format ("/{0}/{1}/", handler.GetType (), ++nextId);
			RegisterHandler (path, handler);
			return new Uri (TargetUri, path);
		}

		public abstract void RegisterHandler (string path, Handler handler);

		protected internal abstract Handler GetHandler (string path);

		public async Task<bool> HandleConnection (TestContext ctx, HttpConnection connection,
		                                          HttpRequest request, CancellationToken cancellationToken)
		{
			var handler = GetHandler (request.Path);
			if (Delegate != null && !Delegate.HandleConnection (ctx, connection, request, handler))
				return false;

			return await handler.HandleRequest (ctx, connection, request, cancellationToken).ConfigureAwait (false);
		}

		public void CheckEncryption (TestContext ctx, SslStream sslStream)
		{
			if ((Flags & (HttpServerFlags.SSL | HttpServerFlags.ForceTls12)) == 0)
				return;
			if ((Flags & HttpServerFlags.HttpListener) != 0) {
				// FIXME
				if (!SslStreamProvider.SupportsHttpListenerContext) {
					ctx.LogMessage ("FIXME: Can't check ISslStream with HttpListener yet.");
					return;
				}
			}

			ctx.Assert (sslStream, Is.Not.Null, "Needs SslStream");
			ctx.Assert (sslStream.IsAuthenticated, "Must be authenticated");

			var setup = DependencyInjector.Get<IConnectionFrameworkSetup> ();
			if (((Flags & HttpServerFlags.ForceTls12) != 0) || setup.SupportsTls12)
				ctx.Assert ((ProtocolVersions)sslStream.SslProtocol, Is.EqualTo (ProtocolVersions.Tls12), "Needs TLS 1.2");
		}

		public abstract Task<T> RunWithContext<T> (TestContext ctx, Func<CancellationToken, Task<T>> func, CancellationToken cancellationToken);

		protected virtual string MyToString ()
		{
			var sb = new StringBuilder ();
			if ((Flags & HttpServerFlags.ReuseConnection) != 0)
				sb.Append ("shared");
			if (UseSSL) {
				if (sb.Length > 0)
					sb.Append (",");
				sb.Append ("ssl");
			}
			return sb.ToString ();
		}

		public override string ToString ()
		{
			var description = MyToString ();
			var padding = string.IsNullOrEmpty (description) ? string.Empty : ": ";
			return string.Format ("[{0}{1}{2}]", GetType ().Name, padding, description);
		}
	}
}
