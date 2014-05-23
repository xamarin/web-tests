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

	public class AuthenticationHandler : AbstractRedirectHandler
	{
		public AuthenticationHandler (AuthenticationType type, Handler target)
			: base (target)
		{
			manager = new HttpAuthManager (type, target);
		}

		AuthenticationHandler (HttpAuthManager manager)
			: base (manager.Target)
		{
			this.manager = manager;
		}

		class HttpAuthManager : AuthenticationManager
		{
			public HttpConnection Connection;
			public readonly Handler Target;

			public HttpAuthManager (AuthenticationType type, Handler target)
				: base (type)
			{
				Target = target;
			}

			protected override void OnError (string message)
			{
				Connection.WriteError (message);
			}

			protected override void OnUnauthenticated (string token, bool omitBody)
			{
				var handler = new AuthenticationHandler (this);
				if (omitBody)
					handler.Flags |= RequestFlags.NoBody;
				handler.Flags |= RequestFlags.Redirected;
				Connection.Server.RegisterHandler (Connection.RequestUri.AbsolutePath, handler);
	
				Connection.ResponseWriter.WriteLine ("HTTP/1.1 401 Unauthorized");
				Connection.ResponseWriter.WriteLine ("WWW-Authenticate: {0}", token);
				Connection.ResponseWriter.WriteLine ();
			}
		}

		readonly HttpAuthManager manager;

		protected internal override bool HandleRequest (HttpConnection connection, RequestFlags effectiveFlags)
		{
			string authHeader;
			if (!connection.Headers.TryGetValue ("Authorization", out authHeader))
				authHeader = null;

			manager.Connection = connection;
			if (!manager.HandleAuthentication (authHeader))
				return false;

			return Target.HandleRequest (connection, effectiveFlags);
		}

		protected internal override void WriteResponse (HttpConnection connection, RequestFlags effectiveFlags)
		{
			Target.WriteResponse (connection, effectiveFlags);
		}

		public override HttpWebRequest CreateRequest (Uri uri)
		{
			var request = base.CreateRequest (uri);
			request.Credentials = new NetworkCredential ("xamarin", "monkey");
			return request;
		}
	}
}

