//
// CheckPortsRequestWorker.cs
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
using System.Collections.Generic;

namespace ConnectionReuse
{
	public class CheckPortsRequestWorker : WebRequestWorker
	{
		const int recycleTime = 180000;
		Dictionary<int,DateTime> knownPorts = new Dictionary<int,DateTime> ();

		public CheckPortsRequestWorker (Uri uri)
			: base (uri)
		{ }

		void RecyclePorts ()
		{
			var expired = new List<int> ();
			foreach (var port in knownPorts.Keys) {
				if (DateTime.UtcNow - knownPorts [port] > TimeSpan.FromMilliseconds (recycleTime))
					expired.Add (port);
			}

			foreach (var port in expired) {
				knownPorts.Remove (port);
			}
		}

		public int KnownPorts {
			get { lock (this) return knownPorts.Count; }
		}

		protected override string DoRequest (System.Threading.CancellationToken token)
		{
			RecyclePorts ();
			var response = base.DoRequest (token);
			int port = int.Parse (response);
			if (!knownPorts.ContainsKey (port))
				knownPorts.Add (port, DateTime.UtcNow);
			return response;
		}
	}
}

