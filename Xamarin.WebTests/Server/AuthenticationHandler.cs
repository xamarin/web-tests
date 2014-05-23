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
using Mono.Security.Protocol.Ntlm;

namespace Xamarin.WebTests.Server
{
	using Framework;

	public enum AuthenticationType {
		Basic,
		NTLM
	}

	public class AuthenticationHandler : AbstractRedirectHandler
	{
		public AuthenticationType AuthenticationType {
			get;
			private set;
		}

		public AuthenticationHandler (AuthenticationType type, Handler target)
			: base (target)
		{
			AuthenticationType = type;
		}

		bool haveChallenge;

		protected override void WriteResponse (HttpConnection connection, RequestFlags effectiveFlags)
		{
			var handler = new AuthenticationHandler (AuthenticationType, Target);

			string authHeader;
			if (!connection.Headers.TryGetValue ("Authorization", out authHeader)) {
				if (AuthenticationType == AuthenticationType.NTLM)
					handler.Flags |= RequestFlags.NoBody;
				WriteUnauthorized (connection, handler, AuthenticationType.ToString ());
				return;
			}

			int pos = authHeader.IndexOf (' ');
			var mode = authHeader.Substring (0, pos);
			var arg = authHeader.Substring (pos + 1);

			if (!mode.Equals (AuthenticationType.ToString ())) {
				WriteError (connection, "Invalid authentication scheme: {0}", mode);
				return;
			}

			if (mode.Equals ("Basic")) {
				if (arg.Equals ("eGFtYXJpbjptb25rZXk="))
					WriteSuccess (connection);
				else
					WriteError (connection, "Invalid Basic Authentication header");
				return;
			} else if (!mode.Equals ("NTLM")) {
				WriteError (connection, "Invalid authentication scheme: {0}", mode);
				return;
			}

			var bytes = Convert.FromBase64String (arg);

			if (haveChallenge) {
				// FIXME: We don't actually check the result.
				var message = new Type3Message (bytes);
				if (message.Type != 3)
					throw new InvalidOperationException ();

				WriteSuccess (connection);
			} else {
				var message = new Type1Message (bytes);
				if (message.Type != 1)
					throw new InvalidOperationException ();

				var type2 = new Type2Message ();
				var token = "NTLM " + Convert.ToBase64String (type2.GetBytes ());

				handler.haveChallenge = true;
				// handler.Flags |= RequestFlags.NoBody;

				WriteUnauthorized (connection, handler, token);
			}
		}

		protected void WriteUnauthorized (HttpConnection connection, Handler handler, string token)
		{
			handler.Flags |= RequestFlags.Redirected;
			connection.Server.RegisterHandler (connection.RequestUri.AbsolutePath, handler);

			connection.ResponseWriter.WriteLine ("HTTP/1.1 401 Unauthorized");
			connection.ResponseWriter.WriteLine ("WWW-Authenticate: {0}", token);
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

