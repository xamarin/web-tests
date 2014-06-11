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
using System.Net.Sockets;
using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;

namespace Xamarin.WebTests.Tests
{
	using Runners;
	using Handlers;
	using Framework;

	[TestFixture]
	public class TestProxy
	{
		ProxyTestRunner simpleRunner;
		ProxyTestRunner authRunner;
		ProxyTestRunner ntlmAuthRunner;
		ProxyTestRunner unauthRunner;
		#if ALPHA
		ProxyTestRunner sslRunner;
		#endif
		bool noNetwork;

		[TestFixtureSetUp]
		public void Start ()
		{
			// MS ignores all proxy request for local connections.
			IPAddress address = TestRunner.GetAddress ();
			if (IPAddress.IsLoopback (address)) {
				noNetwork = true;
				return;
			}

			simpleRunner = new ProxyTestRunner (address, 9999, 9998);
			authRunner = new ProxyTestRunner (address, 9997, 9996) {
				AuthenticationType = AuthenticationType.Basic,
				Credentials = new NetworkCredential ("xamarin", "monkey")
			};
			ntlmAuthRunner = new ProxyTestRunner (address, 9995, 9994) {
				AuthenticationType = AuthenticationType.NTLM,
				Credentials = new NetworkCredential ("xamarin", "monkey")
			};
			unauthRunner = new ProxyTestRunner (address, 9993, 9992) {
				AuthenticationType = AuthenticationType.Basic
			};

			#if ALPHA
			sslRunner = new ProxyTestRunner (address, 9991, 9990) {
				UseSSL = true
			};
			sslRunner.Start ();
			#endif

			simpleRunner.Start ();
			authRunner.Start ();
			ntlmAuthRunner.Start ();
			unauthRunner.Start ();
		}

		[TestFixtureTearDown]
		public void Stop ()
		{
			if (noNetwork)
				return;
			simpleRunner.Stop ();
			authRunner.Stop ();
			ntlmAuthRunner.Stop ();
			unauthRunner.Stop ();
			#if ALPHA
			sslRunner.Start ();
			#endif
		}

		IEnumerable<Handler> GetAllTests ()
		{
			if (IPAddress.IsLoopback (TestRunner.GetAddress ()))
				yield break;
			foreach (var handler in TestPost.GetAllTests ())
				yield return handler;
			foreach (var handler in TestPost.GetAllTests ())
				yield return new AuthenticationHandler (AuthenticationType.Basic, handler);
			foreach (var handler in TestPost.GetAllTests ())
				yield return new AuthenticationHandler (AuthenticationType.NTLM, handler);
		}

		[Test]
		public void CheckNetwork ()
		{
			if (noNetwork)
				Assert.Fail ("TestProxy needs networking access; all proxy tests will be ignored.");
		}

		[TestCaseSource ("GetAllTests")]
		public void Simple (Handler handler)
		{
			if (noNetwork)
				return;
			simpleRunner.Run (handler);
		}

		[TestCaseSource ("GetAllTests")]
		public void ProxyAuth (Handler handler)
		{
			if (noNetwork)
				return;
			authRunner.Run (handler);
		}

		[TestCaseSource ("GetAllTests")]
		public void ProxyNTLM (Handler handler)
		{
			if (noNetwork)
				return;
			ntlmAuthRunner.Run (handler);
		}

		[Test]
		public void TestUnauthenticated ()
		{
			if (noNetwork)
				return;
			var handler = new HelloWorldHandler ();
			unauthRunner.Run (handler, HttpStatusCode.ProxyAuthenticationRequired);
		}

		#if ALPHA
		[TestCaseSource ("GetAllTests")]
		public void TestSSL (Handler handler)
		{
			sslRunner.Run (handler);
		}
		#endif
	}
}
