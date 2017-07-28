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

		public string ME {
			get;
		}

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

			var description = FormatFlags (Flags);
			if (!string.IsNullOrEmpty (description))
				description = ": " + description;
			var identifier = parameters?.Identifier;
			if (identifier != null)
				identifier = ": " + identifier;

			ME = $"[{GetType ().Name}:{ID}{identifier}{description}]";
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

		public abstract void CloseAll ();

		#endregion

		internal abstract Listener Listener {
			get;
		}

		public int CountRequests => countRequests;

		static int nextServerId;
		public readonly int ID = ++nextServerId;

		volatile int countRequests;

		internal void BumpRequestCount ()
		{
			Interlocked.Increment (ref countRequests);
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

		static string FormatFlags (HttpServerFlags flags)
		{
			var sb = new StringBuilder ();
			Append ("shared", HttpServerFlags.ReuseConnection);
			Append ("ssl", HttpServerFlags.SSL);
			Append ("proxy", HttpServerFlags.Proxy);
			Append ("ssl-proxy", HttpServerFlags.ProxySSL);
			Append ("proxy-auth", HttpServerFlags.ProxyAuthentication);
			return sb.ToString ();

			void Append (string name, HttpServerFlags flag)
			{
				if ((flags & flag) == 0)
					return;
				if (sb.Length > 0)
					sb.Append (",");
				sb.Append (name);
			}
		}

		public override string ToString ()
		{
			return ME;
		}
	}
}
