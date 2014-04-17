//
// AuthenticatedPostHandler.cs
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

namespace Xamarin.WebTests.Server
{
	using Framework;

	public class AuthenticationHandler : AbstractRedirectHandler
	{
		public AuthenticationHandler (Handler target)
			: base (target)
		{
		}

		protected override bool HandleRedirect (Connection connection, RequestFlags effectiveFlags)
		{
			string authHeader;
			if (connection.Headers.TryGetValue ("Authorization", out authHeader)) {
				if (!authHeader.Equals ("Basic eGFtYXJpbjptb25rZXk=")) {
					WriteError (connection, "Invalid Authorization header!");
					return false;
				}

				WriteSuccess (connection);
				return true;
			}

			var handler = new AuthenticationHandler (Target);
			connection.Server.RegisterHandler (connection.RequestUri.AbsolutePath, handler);
			WriteUnauthorized (connection);
			return true;
		}

		protected void WriteUnauthorized (Connection connection)
		{
			connection.ResponseWriter.WriteLine ("HTTP/1.1 401 Unauthorized");
			connection.ResponseWriter.WriteLine ("WWW-Authenticate: Basic");
			connection.ResponseWriter.WriteLine ();
		}

		public override HttpWebRequest CreateRequest (Uri uri)
		{
			var request = base.CreateRequest (uri);
			request.Credentials = new NetworkCredential ("xamarin", "monkey");
			return request;
		}
	}
}

