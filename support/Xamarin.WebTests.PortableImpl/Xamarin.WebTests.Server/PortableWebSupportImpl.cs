//
// PortableSupport.cs
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
using System.Net.Http;
using System.Net.Sockets;
using System.Net.NetworkInformation;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Reflection;
using System.Diagnostics;
using System.Collections.Generic;
using System.Collections.Specialized;
using Mono.Security.Protocol.Ntlm;

using Xamarin.AsyncTests;
using Xamarin.AsyncTests.Portable;

namespace Xamarin.WebTests.Portable
{
	using Framework;
	using Server;

	public class PortableWebSupportImpl : IPortableWebSupport
	{
		PortableWebSupportImpl ()
		{
		}

		#region Misc

		static PortableWebSupportImpl ()
		{
			try {
				address = LookupAddress ();
				hasNetwork = !IPAddress.IsLoopback (address);
			} catch {
				address = IPAddress.Loopback;
				hasNetwork = false;
			}
		}

		public static void Initialize ()
		{
			if (instance == null) {
				instance = new PortableWebSupportImpl ();
				DependencyInjector.Register<IPortableWebSupport> (instance);
			}
		}

		static readonly bool hasNetwork;
		static readonly IPAddress address;

		static PortableWebSupportImpl instance;

		#endregion

		#region Networking

		public bool HasNetwork {
			get { return hasNetwork; }
		}

		public IPortableEndPoint GetLoopbackEndpoint (int port)
		{
			return new PortableEndpoint (new IPEndPoint (IPAddress.Loopback, port));
		}

		public IPortableEndPoint GetEndpoint (int port)
		{
			return new PortableEndpoint (new IPEndPoint (address, port));
		}

		public static IPEndPoint GetEndpoint (IPortableEndPoint endpoint)
		{
			return (PortableEndpoint)endpoint;
		}

		static IPAddress LookupAddress ()
		{
			try {
				#if __IOS__
				var interfaces = NetworkInterface.GetAllNetworkInterfaces ();
				foreach (var iface in interfaces) {
					if (iface.NetworkInterfaceType != NetworkInterfaceType.Ethernet && iface.NetworkInterfaceType != NetworkInterfaceType.Wireless80211)
						continue;
					foreach (var address in iface.GetIPProperties ().UnicastAddresses) {
						if (address.Address.AddressFamily == AddressFamily.InterNetwork && !IPAddress.IsLoopback (address.Address))
							return address.Address;
					}
				}
				#elif FIXME
				#else
				return IPAddress.Loopback;
				var hostname = Dns.GetHostName ();
				var hostent = Dns.GetHostEntry (hostname);
				foreach (var address in hostent.AddressList) {
				if (address.AddressFamily == AddressFamily.InterNetwork && !IPAddress.IsLoopback (address))
				return address;
				}
				#endif
			} catch {
				;
			}

			return IPAddress.Loopback;
		}

		class PortableEndpoint : IPortableEndPoint
		{
			readonly IPEndPoint endpoint;

			public PortableEndpoint (IPEndPoint endpoint)
			{
				this.endpoint = endpoint;
			}

			public int Port {
				get { return endpoint.Port; }
			}

			public string Address {
				get { return endpoint.Address.ToString (); }
			}

			public bool IsLoopback {
				get { return IPAddress.IsLoopback (endpoint.Address); }
			}

			public IPortableEndPoint CopyWithPort (int port)
			{
				return new PortableEndpoint (new IPEndPoint (endpoint.Address, port));
			}

			public static implicit operator IPEndPoint (PortableEndpoint portable)
			{
				return portable.endpoint;
			}

			public override string ToString ()
			{
				return string.Format ("[PortableEndpoint {0}]", endpoint);
			}
		}

		IPortableProxy IPortableWebSupport.CreateProxy (Uri uri)
		{
			return new PortableProxy (uri);
		}

		void IPortableWebSupport.SetProxy (HttpWebRequest request, IPortableProxy proxy)
		{
			request.Proxy = (PortableProxy)proxy;
		}

		void IPortableWebSupport.SetProxy (HttpClientHandler handler, IPortableProxy proxy)
		{
			handler.Proxy = (PortableProxy)proxy;
		}

		class PortableProxy : IPortableProxy, IWebProxy
		{
			readonly Uri uri;

			public PortableProxy (Uri uri)
			{
				this.uri = uri;
			}

			public Uri Uri {
				get { return uri; }
			}

			public ICredentials Credentials {
				get; set;
			}

			public Uri GetProxy (Uri destination)
			{
				return uri;
			}

			public bool IsBypassed (Uri host)
			{
				return false;
			}
		}

		void IPortableWebSupport.SetAllowWriteStreamBuffering (HttpWebRequest request, bool value)
		{
			request.AllowWriteStreamBuffering = value;
		}

		void IPortableWebSupport.SetKeepAlive (HttpWebRequest request, bool value)
		{
			request.KeepAlive = value;
		}

		void IPortableWebSupport.SetSendChunked (HttpWebRequest request, bool value)
		{
			request.SendChunked = value;
		}

		void IPortableWebSupport.SetContentLength (HttpWebRequest request, long length)
		{
			request.ContentLength = length;
		}

		Stream IPortableWebSupport.GetRequestStream (HttpWebRequest request)
		{
			return request.GetRequestStream ();
		}

		Task<Stream> IPortableWebSupport.GetRequestStreamAsync (HttpWebRequest request)
		{
			return request.GetRequestStreamAsync ();
		}

		HttpWebResponse IPortableWebSupport.GetResponse (HttpWebRequest request)
		{
			return (HttpWebResponse)request.GetResponse ();
		}

		async Task<HttpWebResponse> IPortableWebSupport.GetResponseAsync (HttpWebRequest request)
		{
			return (HttpWebResponse)await request.GetResponseAsync ();
		}

		IWebClient IPortableWebSupport.CreateWebClient ()
		{
			return new PortableWebClient ();
		}

		class PortableWebClient : IWebClient
		{
			WebClient client = new WebClient ();

			public Task<string> UploadStringTaskAsync (Uri uri, string data)
			{
				return client.UploadStringTaskAsync (uri, data);
			}

			public Task<Stream> OpenWriteAsync (Uri uri, string method)
			{
				return client.OpenWriteTaskAsync (uri, method);
			}

			public Task<byte[]> UploadValuesTaskAsync (
				Uri uri, string method, List<KeyValuePair<string, string>> data)
			{
				var collection = new NameValueCollection ();
				foreach (var entry in data)
					collection.Add (entry.Key, entry.Value);
				return client.UploadValuesTaskAsync (uri, method, collection);
			}

			public void SetCredentials (ICredentials credentials)
			{
				client.Credentials = credentials;
			}

			public void Dispose ()
			{
				Dispose (true);
				GC.SuppressFinalize (this);
			}

			public void Dispose (bool disposing)
			{
				if (client != null) {
					client.Dispose ();
					client = null;
				}
			}

			~PortableWebClient ()
			{
				Dispose (false);
			}
		}

		#endregion

		#region Listeners

		IListener IPortableWebSupport.CreateHttpListener (IPortableEndPoint endpoint, IHttpServer server, bool reuseConnection, bool ssl)
		{
			return new HttpListener (endpoint, server, reuseConnection, ssl);
		}

		IListener IPortableWebSupport.CreateProxyListener (IListener httpListener, IPortableEndPoint proxyEndpoint, AuthenticationType authType)
		{
			return new ProxyListener ((HttpListener)httpListener, proxyEndpoint, authType);
		}

		#endregion
	}
}

