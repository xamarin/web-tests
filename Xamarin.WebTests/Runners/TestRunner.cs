//
// HttpTest.cs
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
using System.Threading;
using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;

namespace Xamarin.WebTests.Runners
{
	using Handlers;
	using Framework;

	public abstract class TestRunner
	{
		public abstract void Start ();

		public abstract void Stop ();

		protected abstract HttpWebRequest CreateRequest (Handler handler);

		public static IPAddress GetAddress ()
		{
			try {
				var hostname = Dns.GetHostName ();
				var hostent = Dns.GetHostEntry (hostname);
				foreach (var address in hostent.AddressList) {
					if (address.AddressFamily == AddressFamily.InterNetwork)
						return address;
				}
			} catch {
				;
			}

			return IPAddress.Loopback;
		}

		public void Run (Handler handler, HttpStatusCode expectedStatus = HttpStatusCode.OK)
		{
			Run (handler, expectedStatus, expectedStatus != HttpStatusCode.OK);
		}

		public void Run (Handler handler, HttpStatusCode expectedStatus, bool expectException)
		{
			handler.Debug (0, "RUN");

			var request = CreateRequest (handler);

			handler.SendRequest (request);

			try {
				var response = (HttpWebResponse)request.GetResponse ();
				handler.Debug (1, "GOT RESPONSE", response.StatusCode, response.StatusDescription);
				Assert.AreEqual (expectedStatus, response.StatusCode, "status code");
				Assert.IsFalse (expectException, "success status");

				using (var reader = new StreamReader (response.GetResponseStream ())) {
					var content = reader.ReadToEnd ();
					handler.Debug (5, "GOT RESPONSE BODY", content);
				}

				response.Close ();
			} catch (WebException wexc) {
				var response = (HttpWebResponse)wexc.Response;
				if (expectException) {
					Assert.AreEqual (expectedStatus, response.StatusCode, "error status code");
					response.Close ();
					return;
				}

				Console.Error.WriteLine ("GOT WEB EXCEPTION: {0}", wexc);

				using (var reader = new StreamReader (response.GetResponseStream ())) {
					var content = reader.ReadToEnd ();
					Console.Error.WriteLine ("RUN - GOT WEB ERROR: {0} {1} {2}\n{3}\n{4}", handler,
						wexc.Status, response.StatusCode, content, wexc);
					Assert.Fail ("{0}: {1}", handler, content);
				}
				response.Close ();
				throw;
			} catch (Exception ex) {
				Console.Error.WriteLine ("RUN - GOT EXCEPTION: {0}", ex);
				throw;
			} finally {
				handler.Debug (0, "RUN DONE");
			}
		}
	}
}

