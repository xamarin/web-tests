//
// ProxyServer.cs
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
using System.Net;
using System.Threading;
using System.Threading.Tasks;

using Xamarin.AsyncTests;

namespace Xamarin.WebTests.Framework
{
	using Framework;
	using Handlers;
	using Portable;

	public class ProxyServer : HttpServer
	{
		Uri proxyUri;
		IPortableEndPoint proxyEndpoint;
		AuthenticationType authType = AuthenticationType.None;
		IListener proxyListener;

		public ProxyServer (IPortableEndPoint endpoint, IPortableEndPoint proxyEndpoint, bool ssl = false)
			: base (endpoint, false, ssl)
		{
			this.proxyEndpoint = proxyEndpoint;

			proxyUri = new Uri (string.Format ("http://{0}:{1}/", proxyEndpoint.Address, proxyEndpoint.Port));
		}

		public AuthenticationType AuthenticationType {
			get { return authType; }
			set { authType = value; }
		}

		public ICredentials Credentials {
			get; set;
		}

		public override async Task Start (CancellationToken cancellationToken)
		{
			await base.Start (cancellationToken);

			proxyListener = PortableSupport.Web.CreateProxyListener (Listener, proxyEndpoint, authType);
			await proxyListener.Start ();
		}

		public override async Task Stop (CancellationToken cancellationToken)
		{
			await proxyListener.Stop ();
			proxyListener = null;

			await base.Stop (cancellationToken);
		}

		public override IPortableProxy GetProxy ()
		{
			var proxy = PortableSupport.Web.CreateProxy (proxyUri);
			if (Credentials != null)
				proxy.Credentials = Credentials;
			return proxy;
		}

		protected override string MyToString ()
		{
			return string.Format ("SSL={0}, AuthenticationType={1}", UseSSL, AuthenticationType);
		}
	}
}

