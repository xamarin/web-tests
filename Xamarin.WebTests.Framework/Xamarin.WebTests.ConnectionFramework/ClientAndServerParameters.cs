using System;
using System.Collections.Generic;
using Xamarin.AsyncTests;
using Xamarin.WebTests.Portable;

namespace Xamarin.WebTests.ConnectionFramework
{
	public abstract class ClientAndServerParameters : ConnectionParameters, IClientAndServerParameters
	{
		protected ClientAndServerParameters (string identifier)
			: base (identifier)
		{
		}

		protected ClientAndServerParameters (ClientAndServerParameters other)
			: base (other)
		{
		}

		public abstract IClientParameters ClientParameters {
			get;
		}

		public abstract IServerParameters ServerParameters {
			get;
		}

		public static ClientAndServerParameters Create (ClientParameters clientParameters, ServerParameters serverParameters)
		{
			return new SimpleClientAndServerParameters (clientParameters, serverParameters);
		}

		class SimpleClientAndServerParameters : ClientAndServerParameters
		{
			readonly ClientParameters clientParameters;
			readonly ServerParameters serverParameters;

			public SimpleClientAndServerParameters (ClientParameters clientParameters, ServerParameters serverParameters)
				: base (clientParameters.Identifier + ":" + serverParameters.Identifier)
			{
				this.clientParameters= clientParameters;
				this.serverParameters = serverParameters;
			}

			public override ConnectionParameters DeepClone ()
			{
				var clonedClient = (ClientParameters)clientParameters.DeepClone ();
				var clonedServer = (ServerParameters)serverParameters.DeepClone ();
				return new SimpleClientAndServerParameters (clonedClient, clonedServer);
			}

			public override IClientParameters ClientParameters {
				get { return clientParameters; }
			}

			public override IServerParameters ServerParameters {
				get { return serverParameters; }
			}
		}
	}
}

