//
// ProxyTestRunner.cs
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

namespace Xamarin.WebTests.Runners
{
	using Framework;
	using Handlers;
	using Server;

	public class ProxyTestRunner : TestRunner
	{
		IPEndPoint endpoint, proxyEndpoint;
		AuthenticationType authType = AuthenticationType.None;
		HttpListener httpListener;
		ProxyListener proxyListener;

		public ProxyTestRunner (IPEndPoint endpoint, IPEndPoint proxyEndpoint)
		{
			this.endpoint = endpoint;
			this.proxyEndpoint = proxyEndpoint;
		}

		public ProxyTestRunner (IPAddress address, int port, int proxyPort)
			: this (new IPEndPoint (address, port), new IPEndPoint (address,proxyPort))
		{
		}

		public AuthenticationType AuthenticationType {
			get { return authType; }
			set { authType = value; }
		}

		public ICredentials Credentials {
			get; set;
		}

		public bool UseSSL {
			get; set;
		}

		public override Task Start (CancellationToken cancellationToken)
		{
			return Task.Run (() => {
				httpListener = new HttpListener (endpoint.Address, endpoint.Port, UseSSL);
				proxyListener = new ProxyListener (httpListener, proxyEndpoint.Address, proxyEndpoint.Port, authType);
			});
		}

		public override Task Stop (CancellationToken cancellationToken)
		{
			return Task.Run (() => {
				proxyListener.Stop ();
				httpListener.Stop ();
				proxyListener = null;
				httpListener = null;
			});
		}

		IWebProxy CreateProxy ()
		{
			var proxy = new WebProxy (proxyListener.Uri, false);
			if (Credentials != null)
				proxy.Credentials = Credentials;
			return proxy;
		}

		protected override HttpWebRequest CreateRequest (Handler handler)
		{
			var request = handler.CreateRequest (httpListener);
			request.Proxy = CreateProxy ();
			return request;
		}

		protected override string MyToString ()
		{
			return string.Format ("SSL={0}, AuthenticationType={1}", UseSSL, AuthenticationType);
		}
	}
}

