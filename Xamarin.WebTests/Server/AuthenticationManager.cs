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

		void OnError (string format, params object[] args)
		{
			OnError (string.Format (format, args));
		}

		protected abstract void OnError (string message);

		protected abstract void OnUnauthenticated (string token, bool omitBody);

		public bool HandleAuthentication (string authHeader)
		{
			if (authHeader == null) {
				OnUnauthenticated (AuthenticationType.ToString (), AuthenticationType == AuthenticationType.NTLM);
				return false;
			}

			int pos = authHeader.IndexOf (' ');
			var mode = authHeader.Substring (0, pos);
			var arg = authHeader.Substring (pos + 1);

			if (!mode.Equals (AuthenticationType.ToString ())) {
				OnError ("Invalid authentication scheme: {0}", mode);
				return false;
			}

			if (mode.Equals ("Basic")) {
				if (arg.Equals ("eGFtYXJpbjptb25rZXk="))
					return true;
				OnError ("Invalid Basic Authentication header");
				return false;
			} else if (!mode.Equals ("NTLM")) {
				OnError ("Invalid authentication scheme: {0}", mode);
				return false;
			}

			var bytes = Convert.FromBase64String (arg);

			if (haveChallenge) {
				// FIXME: We don't actually check the result.
				var message = new Type3Message (bytes);
				if (message.Type != 3)
					throw new InvalidOperationException ();

				return true;
			} else {
				var message = new Type1Message (bytes);
				if (message.Type != 1)
					throw new InvalidOperationException ();

				var type2 = new Type2Message ();
				var token = "NTLM " + Convert.ToBase64String (type2.GetBytes ());

				haveChallenge = true;

				OnUnauthenticated (token, false);
				return false;
			}
		}
	}
}

