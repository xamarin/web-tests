//
// IPortableWebSupport.cs
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
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Xamarin.WebTests.Portable
{
	public interface IPortableWebSupport
	{
		bool HasNetwork {
			get;
		}

		IPortableEndPoint GetLoopbackEndpoint (int port);

		IPortableEndPoint GetEndpoint (int port);

		IPortableProxy CreateProxy (Uri uri);

		IListener CreateHttpListener (IPortableEndPoint endpoint, IHttpServer server, bool reuseConnection, bool ssl);

		IListener CreateProxyListener (IListener httpListener, IPortableEndPoint proxyEndpoint, AuthenticationType authType);

		void SetProxy (HttpWebRequest request, IPortableProxy proxy);

		void SetProxy (HttpClientHandler handler, IPortableProxy proxy);

		void SetAllowWriteStreamBuffering (HttpWebRequest request, bool value);

		void SetKeepAlive (HttpWebRequest request, bool value);

		void SetSendChunked (HttpWebRequest request, bool value);

		void SetContentLength (HttpWebRequest request, long length);

		Stream GetRequestStream (HttpWebRequest request);

		Task<Stream> GetRequestStreamAsync (HttpWebRequest request);

		HttpWebResponse GetResponse (HttpWebRequest request);

		Task<HttpWebResponse> GetResponseAsync (HttpWebRequest request);

		bool HandleNTLM (ref byte[] bytes, ref bool haveChallenge);

		IWebClient CreateWebClient ();
	}
}

