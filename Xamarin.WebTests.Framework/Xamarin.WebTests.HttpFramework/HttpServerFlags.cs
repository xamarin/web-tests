//
// HttpServerFlags.cs
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

namespace Xamarin.WebTests.HttpFramework
{
	[Flags]
	public enum HttpServerFlags
	{
		None				= 0,
		Proxy				= 1,
		ReuseConnection			= 2,
		SSL				= 4,
		ExpectException			= 8,
		ForceTls12			= 16,
		ExternalServer			= 32,
		HttpListener			= 64,
		ProxySSL			= 128,
		ProxyAuthentication		= 256,
		ParallelListener		= 512,
		InstrumentationListener		= 1024,
		NoSSL				= 2048,
		InternalVersionTwo		= 4096,
		RequireMono			= 8192,
		RequireRenegotiation		= 16384,
		RequireCleanShutdown		= 32768,
		RequireNewWebStack		= 65536,
		RequireInstrumentation		= 131072,
		RequireGZip			= 262144
	}
}
