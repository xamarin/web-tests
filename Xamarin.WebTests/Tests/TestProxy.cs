//
// TestProxy.cs
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
using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;

namespace Xamarin.WebTests.Tests
{
	using Server;
	using Framework;

	[TestFixture]
	public class TestProxy : HttpTest
	{
		HttpListener httpListener;
		ProxyListener proxyListener;

		[TestFixtureSetUp]
		public void Start ()
		{
			httpListener = new HttpListener (IPAddress.Loopback, 9999);
			proxyListener = new ProxyListener (httpListener, IPAddress.Loopback, 9998);
		}

		[TestFixtureTearDown]
		public void Stop ()
		{
			proxyListener.Stop ();
			httpListener.Stop ();
		}

		IWebProxy CreateProxy ()
		{
			return new WebProxy (proxyListener.Uri, false);
		}

		public static IEnumerable<Handler> GetAllTests ()
		{
			return TestPost.GetAllTests ();
		}

		protected override HttpWebRequest CreateRequest (Handler handler)
		{
			var request = handler.CreateRequest (httpListener);
			request.Proxy = CreateProxy ();
			return request;
		}

		[TestCaseSource ("GetAllTests")]
		public void ProxyPost (Handler handler)
		{
			Run (handler);
		}
	}
}
