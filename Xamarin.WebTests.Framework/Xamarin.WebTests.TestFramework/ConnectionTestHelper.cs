//
// ConnectionTestFeatures.cs
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
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using Xamarin.AsyncTests;
using Xamarin.AsyncTests.Constraints;
using Xamarin.AsyncTests.Portable;

namespace Xamarin.WebTests.TestFramework
{
	using TestRunners;
	using ConnectionFramework;

	public static class ConnectionTestHelper {
		static readonly ConnectionProviderFactory Factory;

		static ConnectionTestHelper ()
		{
			Factory = DependencyInjector.Get<ConnectionProviderFactory> ();
		}

		public static ConnectionProviderFlags GetProviderFlags (ConnectionProviderType type)
		{
			return Factory.GetProviderFlags (type);
		}

		public static R CreateTestRunner<P, A, R> (TestContext ctx, Func<Connection, Connection, P, A, R> constructor)
			where P : ClientAndServerProvider
			where A : ConnectionParameters
			where R : ClientAndServer
		{
			var parameters = ctx.GetParameter<A> ();
			var provider = ctx.GetParameter<P> ();
			return CreateTestRunner (ctx, provider, parameters, constructor);
		}

		public static R CreateTestRunner<P, A, R> (TestContext ctx, P provider, A parameters, Func<Connection, Connection, P, A, R> constructor)
			where P : ClientAndServerProvider
			where A : ConnectionParameters
			where R : ClientAndServer
		{
			ProtocolVersions protocolVersion;
			if (ctx.TryGetParameter<ProtocolVersions> (out protocolVersion))
				parameters.ProtocolVersion = protocolVersion;

			if (provider.IsManual) {
				string serverAddress;
				if (ctx.Settings.TryGetValue ("ServerAddress", out serverAddress)) {
					var support = DependencyInjector.Get<IPortableEndPointSupport> ();
					parameters.ListenAddress = support.ParseEndpoint (serverAddress, 443, true);

					string serverHost;
					if (ctx.Settings.TryGetValue ("ServerHost", out serverHost))
						parameters.TargetHost = serverHost;
				}
			}

			if (parameters.EndPoint != null) {
				if (parameters.TargetHost == null)
					parameters.TargetHost = parameters.EndPoint.HostName;
			} else if (provider.IsManual) {
				var support = DependencyInjector.Get<IPortableEndPointSupport> ();
				parameters.ListenAddress = support.GetEndpoint ("0.0.0.0", 4433);
			} else if (parameters.ListenAddress != null)
				parameters.EndPoint = parameters.ListenAddress;
			else
				parameters.EndPoint = GetEndPoint (ctx);

			var server = provider.CreateServer (parameters);

			var client = provider.CreateClient (parameters);

			return constructor (server, client, provider, parameters);
		}

		public static IPortableEndPoint GetEndPoint (TestContext ctx)
		{
			var support = DependencyInjector.Get<IPortableEndPointSupport> ();
			var port = TestContext.GetUniquePort ();
			return support.GetLoopbackEndpoint (port);
		}

		public static bool IsMicrosoftRuntime {
			get { return DependencyInjector.Get<IPortableSupport> ().IsMicrosoftRuntime; }
		}

		public static IEnumerable<R> Join<T, U, R> (IEnumerable<T> first, IEnumerable<U> second, Func<T, U, R> resultSelector, bool filterOutNull = true)
		{
			foreach (var e1 in first) {
				foreach (var e2 in second) {
					var result = resultSelector (e1, e2);
					if (!filterOutNull || result != null)
						yield return result;
				}
			}
		}
	}
}

