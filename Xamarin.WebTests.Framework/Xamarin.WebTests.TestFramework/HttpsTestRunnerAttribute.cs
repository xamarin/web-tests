//
// HttpsServerAttribute.cs
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
using Xamarin.AsyncTests;
using Xamarin.AsyncTests.Framework;
using Xamarin.AsyncTests.Portable;
using Xamarin.AsyncTests.Constraints;

namespace Xamarin.WebTests.TestFramework
{
	using TestRunners;
	using ConnectionFramework;
	using HttpFramework;
	using Portable;
	using Providers;
	using Resources;

	[AttributeUsage (AttributeTargets.Class, AllowMultiple = false)]
	public class HttpsTestRunnerAttribute : TestHostAttribute, ITestHost<HttpsTestRunner>
	{
		public HttpsTestRunnerAttribute ()
			: base (typeof (HttpsTestRunnerAttribute), TestFlags.Hidden | TestFlags.PathHidden)
		{
		}

		protected HttpsTestRunnerAttribute (Type type, TestFlags flags = TestFlags.Hidden | TestFlags.PathHidden)
			: base (type, flags)
		{
		}

		public HttpsTestRunner CreateInstance (TestContext ctx)
		{
			var httpProvider = ConnectionTestHelper.GetHttpProvider (ctx);

			var parameters = ctx.GetParameter<HttpsTestParameters> ();

			ProtocolVersions protocolVersion;
			if (ctx.TryGetParameter<ProtocolVersions> (out protocolVersion))
				parameters.ProtocolVersion = protocolVersion;

			if (parameters.EndPoint != null) {
				if (parameters.TargetHost == null)
					parameters.TargetHost = parameters.EndPoint.HostName;
			} else if (parameters.ListenAddress != null)
				parameters.EndPoint = parameters.ListenAddress;
			else
				parameters.EndPoint = ConnectionTestHelper.GetEndPoint (ctx);

			var listenerFlags = ListenerFlags.SSL;

			return new HttpsTestRunner (httpProvider, parameters.EndPoint, listenerFlags, parameters);
		}
	}
}

