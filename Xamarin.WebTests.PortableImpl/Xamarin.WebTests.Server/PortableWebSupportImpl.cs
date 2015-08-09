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
using Http = System.Net.Http;
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

using Xamarin.AsyncTests;
using Xamarin.AsyncTests.Portable;

namespace Xamarin.WebTests.Server
{
	using HttpClient;
	using Portable;
	using Providers;
	using HttpFramework;
	using Resources;
	using Server;

	class PortableWebSupportImpl : IPortableWebSupport
	{
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

		static readonly bool hasNetwork;
		static readonly IPAddress address;

		#endregion

		#region Networking

		internal static IPAddress Address {
			get { return address; }
		}

		public bool HasNetwork {
			get { return hasNetwork; }
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
				#else
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

		IPortableProxy IPortableWebSupport.CreateProxy (Uri uri)
		{
			return new PortableProxy (uri);
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

			public void Cancel ()
			{
				if (client != null)
					client.CancelAsync ();
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

		IServerCertificate IPortableWebSupport.GetDefaultServerCertificate ()
		{
			return ResourceManager.SelfSignedServerCertificate;
		}

		IListener IPortableWebSupport.CreateHttpListener (HttpServer server)
		{
			return new HttpListener (server);
		}

		IListener IPortableWebSupport.CreateProxyListener (IListener httpListener, IPortableEndPoint proxyEndpoint, AuthenticationType authType)
		{
			return new ProxyListener ((HttpListener)httpListener, proxyEndpoint, authType);
		}

		#endregion

		#region Certificate Validation

		public bool SupportsPerRequestCertificateValidator {
			get { return HttpWebRequestImpl.SupportsCertificateValidator; }
		}

		public void InstallCertificateValidator (IHttpWebRequest request, ICertificateValidator validator)
		{
			((HttpWebRequestImpl)request).InstallCertificateValidator (validator);
		}

		public void InstallDefaultCertificateValidator (ICertificateValidator validator)
		{
			ServicePointManager.ServerCertificateValidationCallback = ((CertificateValidator)validator).ValidationCallback;
		}

		#endregion
	}
}

