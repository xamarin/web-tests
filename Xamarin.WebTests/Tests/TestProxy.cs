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

		public TestProxy ()
		{
			// MS ignores all proxy request for local connections.
			IPAddress address = TestRunner.GetAddress ();

			simpleRunner = new ProxyTestRunner (new IPEndPoint (address, 9999), new IPEndPoint (address, 9998));
			authRunner = new ProxyTestRunner (new IPEndPoint (address, 9997), new IPEndPoint (address, 9996)) {
				AuthenticationType = AuthenticationType.Basic,
				Credentials = new NetworkCredential ("xamarin", "monkey")
			};
			ntlmAuthRunner = new ProxyTestRunner (new IPEndPoint (address, 9995), new IPEndPoint (address, 9994)) {
				AuthenticationType = AuthenticationType.NTLM,
				Credentials = new NetworkCredential ("xamarin", "monkey")
			};
			unauthRunner = new ProxyTestRunner (new IPEndPoint (address, 9993), new IPEndPoint (address, 9992)) {
				AuthenticationType = AuthenticationType.Basic
			};
		}

		[TestFixtureSetUp]
		public void Start ()
		{
			simpleRunner.Start ();
			authRunner.Start ();
			ntlmAuthRunner.Start ();
			unauthRunner.Start ();
		}

		[TestFixtureTearDown]
		public void Stop ()
		{
			simpleRunner.Stop ();
			authRunner.Stop ();
			ntlmAuthRunner.Stop ();
			unauthRunner.Stop ();
		}

		static IEnumerable<Handler> GetAllTests ()
		{
			foreach (var handler in TestPost.GetAllTests ())
				yield return handler;
			foreach (var handler in TestPost.GetAllTests ())
				yield return new AuthenticationHandler (AuthenticationType.Basic, handler);
			foreach (var handler in TestPost.GetAllTests ())
				yield return new AuthenticationHandler (AuthenticationType.NTLM, handler);
		}

		[TestCaseSource ("GetAllTests")]
		public void Simple (Handler handler)
		{
			simpleRunner.Run (handler);
		}

		[TestCaseSource ("GetAllTests")]
		public void ProxyAuth (Handler handler)
		{
			authRunner.Run (handler);
		}

		[TestCaseSource ("GetAllTests")]
		public void ProxyNTLM (Handler handler)
		{
			ntlmAuthRunner.Run (handler);
		}

		[Test]
		public void TestUnauthenticated ()
		{
			var handler = new HelloWorldHandler ();
			unauthRunner.Run (handler, HttpStatusCode.ProxyAuthenticationRequired);
		}
	}
}
