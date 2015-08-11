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
using System.Collections.Generic;
using Xamarin.AsyncTests;
using Xamarin.AsyncTests.Constraints;
using Xamarin.AsyncTests.Portable;

namespace Xamarin.WebTests.Features
{
	using Portable;
	using Providers;
	using TestRunners;
	using ConnectionFramework;

	static class ConnectionTestFeatures
	{
		static readonly ConnectionProviderFactory Factory;
		static readonly Constraint isProviderSupported;

		static ConnectionTestFeatures ()
		{
			Factory = DependencyInjector.Get<ConnectionProviderFactory> ();
			isProviderSupported = new IsSupportedConstraint<ConnectionProviderType> (f => Factory.IsSupported (f));
		}

		public static ConnectionProviderFlags GetProviderFlags (ConnectionProviderType type)
		{
			return Factory.GetProviderFlags (type);
		}

		public static Constraint IsProviderSupported {
			get { return isProviderSupported; }
		}

		public static ClientParameters GetClientParameters (TestContext ctx)
		{
			ClientAndServerParameters clientAndServerParameters = null;
			return GetClientParameters (ctx, ref clientAndServerParameters);
		}

		public static ClientParameters GetClientParameters (TestContext ctx, ref ClientAndServerParameters clientAndServerParameters)
		{
			ClientParameters clientParameters;

			if (clientAndServerParameters != null) {
				clientParameters = clientAndServerParameters.ClientParameters;
			} else if (ctx.TryGetParameter<ClientParameters> (out clientParameters)) {
				clientAndServerParameters = null;
			} else if (ctx.TryGetParameter<ClientAndServerParameters> (out clientAndServerParameters)) {
				clientParameters = clientAndServerParameters.ClientParameters;
			} else {
				ctx.AssertFail ("Missing '{0}' property.", "ClientParameters");
				clientAndServerParameters = null;
				return null;
			}

			return clientParameters;
		}

		public static ServerParameters GetServerParameters (TestContext ctx)
		{
			ClientAndServerParameters clientAndServerParameters = null;
			return GetServerParameters (ctx, ref clientAndServerParameters);
		}

		public static ServerParameters GetServerParameters (TestContext ctx, ref ClientAndServerParameters clientAndServerParameters)
		{
			ServerParameters serverParameters;

			if (clientAndServerParameters != null) {
				serverParameters = clientAndServerParameters.ServerParameters;
			} else if (ctx.TryGetParameter<ServerParameters> (out serverParameters)) {
				clientAndServerParameters = null;
			} else if (ctx.TryGetParameter<ClientAndServerParameters> (out clientAndServerParameters)) {
				serverParameters = clientAndServerParameters.ServerParameters;
			} else {
				ctx.AssertFail ("Missing '{0}' property.", "ServerParameters");
				clientAndServerParameters = null;
				return null;
			}

			return serverParameters;
		}

		public static ConnectionProviderType GetClientType (TestContext ctx)
		{
			ConnectionProviderType type;
			if (ctx.TryGetParameter<ConnectionProviderType> (out type, "ClientType"))
				return type;
			ClientAndServerType clientAndServerType;
			if (ctx.TryGetParameter<ClientAndServerType> (out clientAndServerType))
				return clientAndServerType.Client;
			if (!ctx.TryGetParameter<ConnectionProviderType> (out type))
				type = ConnectionProviderType.DotNet;
			return type;
		}

		public static ConnectionProviderType GetServerType (TestContext ctx)
		{
			ConnectionProviderType type;
			if (ctx.TryGetParameter<ConnectionProviderType> (out type, "ServerType"))
				return type;
			ClientAndServerType clientAndServerType;
			if (ctx.TryGetParameter<ClientAndServerType> (out clientAndServerType))
				return clientAndServerType.Server;
			if (!ctx.TryGetParameter<ConnectionProviderType> (out type))
				type = ConnectionProviderType.DotNet;
			return type;
		}

		public static IHttpProvider GetHttpProvider (TestContext ctx)
		{
			var type = GetClientType (ctx);
			var provider = Factory.GetProvider (type);
			return provider.HttpProvider;
		}

		public static IClient CreateClient (TestContext ctx)
		{
			var providerType = GetClientType (ctx);
			ctx.Assert (providerType, IsProviderSupported);
			var provider = Factory.GetProvider (providerType);

			var parameters = GetClientParameters (ctx);
			return provider.CreateClient (parameters);
		}

		public static IServer CreateServer (TestContext ctx)
		{
			var providerType = GetServerType (ctx);
			ctx.Assert (providerType, IsProviderSupported);
			var provider = Factory.GetProvider (providerType);

			var parameters = GetServerParameters (ctx);
			return provider.CreateServer (parameters);
		}

		public static R CreateTestRunner<P,R> (TestContext ctx, Func<IServer,IClient,P,R> constructor)
			where P : ClientAndServerParameters
			where R : ClientAndServerTestRunner
		{
			var parameters = ctx.GetParameter<P> ();
			return CreateTestRunner (ctx, parameters, constructor);
		}

		public static R CreateTestRunner<P,R> (TestContext ctx, P parameters, Func<IServer,IClient,P,R> constructor)
			where P : ClientAndServerParameters
			where R : ClientAndServerTestRunner
		{
			var clientProviderType = GetClientType (ctx);
			var serverProviderType = GetServerType (ctx);

			var clientProvider = Factory.GetProvider (clientProviderType);
			var serverProvider = Factory.GetProvider (serverProviderType);

			ProtocolVersions protocolVersion;
			if (ctx.TryGetParameter<ProtocolVersions> (out protocolVersion))
				parameters.ProtocolVersion = protocolVersion;

			if (serverProviderType == ConnectionProviderType.Manual) {
				string serverAddress;
				if (!ctx.Settings.TryGetValue ("ServerAddress", out serverAddress))
					throw new NotSupportedException ("Missing 'ServerAddress' setting.");

				var support = DependencyInjector.Get<IPortableEndPointSupport> ();
				parameters.EndPoint = support.ParseEndpoint (serverAddress, 443, true);

				string serverHost;
				if (ctx.Settings.TryGetValue ("ServerHost", out serverHost))
					parameters.ClientParameters.TargetHost = serverHost;
			}

			if (parameters.EndPoint != null) {
				if (parameters.ClientParameters.EndPoint == null)
					parameters.ClientParameters.EndPoint = parameters.EndPoint;
				if (parameters.ServerParameters.EndPoint == null)
					parameters.ServerParameters.EndPoint = parameters.EndPoint;

				if (parameters.ClientParameters.TargetHost == null)
					parameters.ClientParameters.TargetHost = parameters.EndPoint.HostName;
			} else {
				CommonHttpFeatures.GetUniqueEndPoint (ctx, parameters);
			}

			var server = serverProvider.CreateServer (parameters.ServerParameters);

			var client = clientProvider.CreateClient (parameters.ClientParameters);

			return constructor (server, client, parameters);
		}

		public static IEnumerable<R> Join<T,U,R> (IEnumerable<T> first, IEnumerable<U> second, Func<T, U, R> resultSelector) {
			foreach (var e1 in first) {
				foreach (var e2 in second) {
					yield return resultSelector (e1, e2);
				}
			}
		}
	}
}

