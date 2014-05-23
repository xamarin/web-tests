//
// AuthenticationManager.cs
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
	public enum AuthenticationType {
		None,
		Basic,
		NTLM
	}

	public enum AuthenticationState {
		Authenticated,
		ResendRequest,
		ResendRequestWithoutBody,
		Error
	}

	public abstract class AuthenticationManager
	{
		public AuthenticationType AuthenticationType {
			get;
			private set;
		}

		public AuthenticationManager (AuthenticationType type)
		{
			AuthenticationType = type;
		}

		bool haveChallenge;

		HttpResponse OnError (string format, params object[] args)
		{
			return OnError (string.Format (format, args));
		}

		protected virtual HttpResponse OnError (string message)
		{
			return HttpResponse.CreateError (message);
		}

		protected abstract HttpResponse OnUnauthenticated (HttpRequest request, string token, bool omitBody);

		public HttpResponse HandleAuthentication (HttpRequest request, string authHeader)
		{
			if (authHeader == null) {
				haveChallenge = false;
				return OnUnauthenticated (request, AuthenticationType.ToString (), AuthenticationType == AuthenticationType.NTLM);
			}

			int pos = authHeader.IndexOf (' ');
			var mode = authHeader.Substring (0, pos);
			var arg = authHeader.Substring (pos + 1);

			if (!mode.Equals (AuthenticationType.ToString ()))
				return OnError ("Invalid authentication scheme: {0}", mode);

			if (mode.Equals ("Basic")) {
				if (arg.Equals ("eGFtYXJpbjptb25rZXk="))
					return null;
				return OnError ("Invalid Basic Authentication header");
			} else if (!mode.Equals ("NTLM")) {
				return OnError ("Invalid authentication scheme: {0}", mode);
			}

			var bytes = Convert.FromBase64String (arg);

			if (haveChallenge) {
				// FIXME: We don't actually check the result.
				var message = new Type3Message (bytes);
				if (message.Type != 3)
					throw new InvalidOperationException ();

				return null;
			} else {
				var message = new Type1Message (bytes);
				if (message.Type != 1)
					throw new InvalidOperationException ();

				var type2 = new Type2Message ();
				var token = "NTLM " + Convert.ToBase64String (type2.GetBytes ());

				haveChallenge = true;

				return OnUnauthenticated (request, token, false);
			}
		}
	}
}

