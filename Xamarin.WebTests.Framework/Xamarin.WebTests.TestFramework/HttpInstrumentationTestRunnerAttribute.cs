//
// HttpInstrumentationTestRunnerAttribute.cs
//
// Author:
//       Martin Baulig <mabaul@microsoft.com>
//
// Copyright (c) 2017 Xamarin Inc. (http://www.xamarin.com)
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
using Xamarin.AsyncTests;
using Xamarin.AsyncTests.Framework;
using Xamarin.AsyncTests.Portable;
using Xamarin.AsyncTests.Constraints;

namespace Xamarin.WebTests.TestFramework
{
	using TestRunners;
	using ConnectionFramework;
	using HttpFramework;
	using Server;
	using Resources;

	[AttributeUsage (AttributeTargets.Class, AllowMultiple = false)]
	public sealed class HttpInstrumentationTestRunnerAttribute : TestHostAttribute, ITestHost<HttpInstrumentationTestRunner>
	{
		public HttpServerFlags ServerFlags {
			get;
		}

		public HttpInstrumentationTestRunnerAttribute (HttpServerFlags serverFlags = HttpServerFlags.None)
			: base (typeof (HttpInstrumentationTestRunnerAttribute))
		{
			ServerFlags = serverFlags;
		}

		public HttpInstrumentationTestRunner CreateInstance (TestContext ctx)
		{
			var provider = ctx.GetParameter<ConnectionTestProvider> ();

			var parameters = ctx.GetParameter<HttpInstrumentationTestParameters> ();

			ProtocolVersions protocolVersion;
			if (ctx.TryGetParameter<ProtocolVersions> (out protocolVersion))
				parameters.ProtocolVersion = protocolVersion;

			IPortableEndPoint serverEndPoint;

			if (parameters.ListenAddress != null)
				serverEndPoint = parameters.ListenAddress;
			else if (parameters.EndPoint != null)
				serverEndPoint = parameters.EndPoint;
			else
				serverEndPoint = ConnectionTestHelper.GetEndPoint (ctx);

			if (parameters.EndPoint == null)
				parameters.EndPoint = serverEndPoint;
			if (parameters.ListenAddress == null)
				parameters.ListenAddress = serverEndPoint;

			var flags = ServerFlags | HttpServerFlags.SSL;

			bool reuseConnection;
			if (ctx.TryGetParameter<bool> (out reuseConnection, "ReuseConnection") && reuseConnection)
				flags |= HttpServerFlags.ReuseConnection;

			Uri uri;
			if (parameters.TargetHost == null) {
				parameters.TargetHost = parameters.EndPoint.HostName;
				uri = new Uri (string.Format ("https://{0}:{1}/", parameters.EndPoint.Address, parameters.EndPoint.Port));
			} else {
				uri = new Uri (string.Format ("https://{0}/", parameters.TargetHost));
			}

			return new HttpInstrumentationTestRunner (parameters.EndPoint, parameters, provider, uri, flags);
		}
	}
}
