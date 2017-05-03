//
// ServerParameters.cs
//
// Author:
//       Martin Baulig <martin.baulig@xamarin.com>
//
// Copyright (c) 2015 Xamarin, Inc.
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
using Xamarin.AsyncTests.Portable;

namespace Xamarin.AsyncTests.MacUI
{
	public class ServerParameters
	{
		public ServerMode Mode {
			get;
			private set;
		}

		public IPortableEndPoint EndPoint {
			get;
			private set;
		}

		public PipeArguments Arguments {
			get;
			private set;
		}

		public ServerParameters (ServerMode mode = ServerMode.Local)
		{
			Mode = mode;
		}

		public static ServerParameters CreatePipe (IPortableEndPoint endpoint, PipeArguments arguments)
		{
			return new ServerParameters (ServerMode.Local) { EndPoint = endpoint, Arguments = arguments };
		}

		public static ServerParameters Android (IPortableEndPoint endpoint)
		{
			return new ServerParameters (ServerMode.Android) { EndPoint = endpoint };
		}

		public static ServerParameters IOS (IPortableEndPoint endpoint)
		{
			return new ServerParameters (ServerMode.iOS) { EndPoint = endpoint };
		}

		public static ServerParameters WaitForConnection (IPortableEndPoint endpoint)
		{
			return new ServerParameters (ServerMode.WaitForConnection) { EndPoint = endpoint };
		}
	}
}

