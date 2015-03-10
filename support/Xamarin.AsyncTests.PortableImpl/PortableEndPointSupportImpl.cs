//
// PortableEndPointSupport.cs
//
// Author:
//       Martin Baulig <martin.baulig@xamarin.com>
//
// Copyright (c) 2015 Xamarin, Inc.
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

namespace Xamarin.AsyncTests.Portable
{
	class PortableEndPointSupportImpl : IPortableEndPointSupport
	{
		public IPortableEndPoint GetLoopbackEndpoint (int port)
		{
			return new PortableEndpoint (new IPEndPoint (IPAddress.Loopback, port));
		}

		public IPortableEndPoint GetEndpoint (int port)
		{
			return new PortableEndpoint (new IPEndPoint (PortableSupportImpl.LocalAddress, port));
		}

		public IPortableEndPoint GetEndpoint (string address, int port)
		{
			var ip = IPAddress.Parse (address);
			return new PortableEndpoint (new IPEndPoint (ip, port));
		}

		public static IPEndPoint GetEndpoint (IPortableEndPoint endpoint)
		{
			return (PortableEndpoint)endpoint;
		}

		public IPortableEndPoint ParseEndpoint (string address)
		{
			int port;
			string host;
			var pos = address.IndexOf (":");
			if (pos < 0) {
				host = address;
				port = 8888;
			} else {
				host = address.Substring (0, pos);
				port = int.Parse (address.Substring (pos + 1));
			}

			var ip = IPAddress.Parse (host);
			var endpoint = new IPEndPoint (ip, port);
			return new PortableEndpoint (endpoint);
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
	}
}

