//
// DeleteHandler.cs
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
using System.Text;
using System.Threading;
using System.Globalization;
using System.Collections.Generic;

namespace Xamarin.WebTests.Server
{
	using Framework;

	public class DeleteHandler : Handler
	{
		string body;

		public string Body {
			get { return body; }
			set {
				WantToModify ();
				body = value;
			}
		}

		protected internal override bool HandleRequest (Connection connection, RequestFlags effectiveFlags)
		{
			if (!connection.Method.Equals ("DELETE")) {
				WriteError (connection, "Wrong method: {0}", connection.Method);
				return false;
			}

			return CheckRequest (connection);
		}

		protected override void WriteResponse (Connection connection, RequestFlags effectiveFlags)
		{
			WriteSuccess (connection);
		}

		bool CheckRequest (Connection connection)
		{
			string value;
			var hasLength = connection.Headers.TryGetValue ("Content-Length", out value);
			var hasExplicitLength = (Flags & RequestFlags.ExplicitlySetLength) != 0;

			if (hasLength) {
				var length = int.Parse (value);

				if (Body != null)
					return ReadStaticBody (connection, length);
				else if (hasExplicitLength) {
					if (length != 0) {
						WriteError (connection, "Content-Length must be zero");
						return false;
					}
				} else {
					WriteError (connection, "Content-Length not allowed.");
					return false;
				}
			} else if (hasExplicitLength || Body != null) {
				WriteError (connection, "Missing Content-Length");
				return false;
			}

			return true;
		}

		bool ReadStaticBody (Connection connection, int length)
		{
			var buffer = new char [length];
			int offset = 0;
			while (offset < length) {
				var size = Math.Min (length - offset, 4096);
				int ret = connection.RequestReader.Read (buffer, offset, size);
				if (ret <= 0) {
					WriteError (connection, "Failed to read body.");
					return false;
				}

				offset += ret;
			}

			return true;
		}

		public override HttpWebRequest CreateRequest (Uri uri)
		{
			var request = base.CreateRequest (uri);
			request.Method = "DELETE";

			if (Flags == RequestFlags.ExplicitlySetLength)
				request.ContentLength = Body != null ? Body.Length : 0;

			if (Body != null) {
				using (var writer = new StreamWriter (request.GetRequestStream ())) {
					if (!string.IsNullOrEmpty (Body))
						writer.Write (Body);
				}
			}

			return request;
		}
	}
}
