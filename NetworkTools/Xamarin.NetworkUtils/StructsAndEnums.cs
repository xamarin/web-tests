//
// StructsAndEnums.cs
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
using System.Runtime.InteropServices;

namespace Xamarin.NetworkUtils
{
	public enum TcpState {
		Closed,
		Listen,
		SynSent,
		SynRecvd,
		Established,
		CloseWait,
		FinWait1,
		Closing,
		LastAck,
		FinWait2,
		TimeWait
	}

	public struct NetstatEntry {
		public IPEndPoint LocalEndpoint;
		public IPEndPoint RemoteEndpoint;
		public TcpState State;

		public override bool Equals (object obj)
		{
			var other = (NetstatEntry)obj;
			if (!other.LocalEndpoint.Equals (LocalEndpoint))
				return false;
			if (!other.RemoteEndpoint.Equals (RemoteEndpoint))
				return false;
			if (other.State != State)
				return false;
			return true;
		}

		public override int GetHashCode ()
		{
			return LocalEndpoint.GetHashCode ();
		}

		public override string ToString ()
		{
			return String.Format ("[NetstatEntry: {0} - {1} - {2}", State, LocalEndpoint, RemoteEndpoint);
		}
	}

	[StructLayout (LayoutKind.Sequential)]
	public struct NativeNetstatEntry {
		public long laddr, raddr;
		public int lport, rport;
		public int flags;
		public TcpState state;
	}
}

