﻿//
// HttpValidationTestRunnerAttribute.cs
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
using System.Net;
using Xamarin.AsyncTests;
using Xamarin.AsyncTests.Framework;
using Xamarin.AsyncTests.Portable;
using Xamarin.AsyncTests.Constraints;

namespace Xamarin.WebTests.TestAttributes
{
	using TestRunners;
	using ConnectionFramework;
	using HttpFramework;
	using TestFramework;
	using Server;
	using Resources;

	[AttributeUsage (AttributeTargets.Class, AllowMultiple = false)]
	public sealed class HttpValidationTestRunnerAttribute : TestHostAttribute, ITestHost<HttpValidationTestRunner>
	{
		HttpServerFlags serverFlags;

		public HttpValidationTestRunnerAttribute (HttpServerFlags serverFlags = HttpServerFlags.None)
			: base (typeof (HttpValidationTestRunnerAttribute))
		{
			this.serverFlags = serverFlags;
		}

		HttpServerFlags GetServerFlags (TestContext ctx)
		{
			HttpServerFlags flags = serverFlags | HttpServerFlags.SSL;

			bool reuseConnection;
			if (ctx.TryGetParameter<bool> (out reuseConnection, "ReuseConnection") && reuseConnection)
				flags |= HttpServerFlags.ReuseConnection;

			return flags;
		}

		public HttpValidationTestRunner CreateInstance (TestContext ctx)
		{
			var provider = ctx.GetParameter<ConnectionTestProvider> ();

			var parameters = ctx.GetParameter<HttpValidationTestParameters> ();

			ProtocolVersions protocolVersion;
			if (ctx.TryGetParameter<ProtocolVersions> (out protocolVersion))
				parameters.ProtocolVersion = protocolVersion;

			EndPoint serverEndPoint;

			if (parameters.ListenAddress != null)
				serverEndPoint = parameters.ListenAddress;
			else if (parameters.EndPoint != null)
				serverEndPoint = parameters.EndPoint;
			else
				serverEndPoint = ConnectionTestHelper.GetEndPoint ();

			if (parameters.EndPoint == null)
				parameters.EndPoint = serverEndPoint;
			if (parameters.ListenAddress == null)
				parameters.ListenAddress = serverEndPoint;

			var flags = serverFlags | HttpServerFlags.SSL;

			bool reuseConnection;
			if (ctx.TryGetParameter<bool> (out reuseConnection, "ReuseConnection") && reuseConnection)
				flags |= HttpServerFlags.ReuseConnection;

			Uri uri;
			if (parameters.ExternalServer != null) {
				uri = parameters.ExternalServer;
				flags |= HttpServerFlags.ExternalServer;
			} else if (parameters.TargetHost == null) {
				uri = new Uri (string.Format ("https://{0}/", parameters.EndPoint));
			} else {
				uri = new Uri (string.Format ("https://{0}/", parameters.TargetHost));
			}

			return new HttpValidationTestRunner (parameters.EndPoint, parameters, provider, uri, flags);
		}
	}
}

