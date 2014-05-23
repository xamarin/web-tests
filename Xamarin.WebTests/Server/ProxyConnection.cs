//
// ProxyConnection.cs
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
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Globalization;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Xamarin.WebTests.Server
{
	public class ProxyConnection : Connection
	{
		Connection proxy;

		public ProxyConnection (Socket socket, Connection proxy)
			: base (socket, proxy.Method, proxy.Path, proxy.Protocol)
		{
			this.proxy = proxy;
		}

		public void HandleRequest ()
		{
			var response = Task.Factory.StartNew (() => CopyResponse ());

			WriteRequest ();

			response.Wait ();
		}

		void CopyResponse ()
		{
			ReadResponse ();
			proxy.ResponseWriter.WriteLine ("{0} {1} {2}", Protocol, StatusCode, StatusMessage);
			CopyHeaders (this, proxy);
			CopyBody (this, proxy);
		}

		void WriteRequest ()
		{
			ResponseWriter.WriteLine ("{0} {1} {2}", Method, Path, Protocol);
			CopyHeaders (proxy, this);
			CopyBody (proxy, this);
		}

		static void CopyHeaders (Connection input, Connection output)
		{
			foreach (var entry in input.Headers)
				output.ResponseWriter.WriteLine ("{0}: {1}", entry.Key, entry.Value);
			output.ResponseWriter.WriteLine ();
		}

		static void CopyBody (Connection input, Connection output)
		{
			string value;
			if (input.Headers.TryGetValue ("Content-Length", out value)) {
				var contentLength = int.Parse (value);
				CopyStaticBody (input.RequestReader, output.ResponseWriter, contentLength);
			} else if (input.Headers.TryGetValue ("Transfer-Encoding", out value)) {
				if (!value.Equals ("chunked"))
					throw new InvalidOperationException ();
				CopyChunkedBody (input.RequestReader, output.ResponseWriter);
			}
		}

		static void CopyStaticBody (StreamReader input, StreamWriter output, int length)
		{
			var buffer = new char [length];
			int offset = 0;
			while (offset < length) {
				var size = Math.Min (length - offset, 4096);
				int ret = input.Read (buffer, offset, size);
				if (ret <= 0)
					throw new InvalidOperationException ();

				offset += ret;
			}

			output.WriteLine (buffer);
		}

		static void CopyChunkedBody (StreamReader input, StreamWriter output)
		{
			do {
				var header = input.ReadLine ();
				var length = int.Parse (header, NumberStyles.HexNumber);
				output.WriteLine (header);
				if (length == 0)
					break;

				var buffer = new char [length];
				var ret = input.Read (buffer, 0, length);
				if (ret != length)
					throw new InvalidOperationException ();

				output.Write (buffer, 0, length);

				var empty = input.ReadLine ();
				if (!string.IsNullOrEmpty (empty))
					throw new InvalidOperationException ();
				output.WriteLine ();
			} while (true);
		}

	}
}

