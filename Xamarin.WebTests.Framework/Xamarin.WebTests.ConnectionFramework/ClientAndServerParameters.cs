using System;
using System.Collections.Generic;
using Xamarin.AsyncTests;
using Xamarin.WebTests.Portable;

namespace Xamarin.WebTests.ConnectionFramework
{
	public class ClientAndServerParameters : ConnectionParameters
	{
		readonly ClientParameters clientParameters;
		readonly ServerParameters serverParameters;

		protected ClientAndServerParameters (ClientAndServerParameters other)
			: base (other)
		{
		}

		public ClientAndServerParameters (ClientParameters clientParameters, ServerParameters serverParameters)
			: base (clientParameters.Identifier + ":" + serverParameters.Identifier)
		{
			this.clientParameters= clientParameters;
			this.serverParameters = serverParameters;
		}

		public override ConnectionParameters DeepClone ()
		{
			var clonedClient = (ClientParameters)clientParameters.DeepClone ();
			var clonedServer = (ServerParameters)serverParameters.DeepClone ();
			return new ClientAndServerParameters (clonedClient, clonedServer);
		}

		public ClientParameters ClientParameters {
			get { return clientParameters; }
		}

		public ServerParameters ServerParameters {
			get { return serverParameters; }
		}

		public static ClientAndServerParameters Create (ClientParameters clientParameters, ServerParameters serverParameters)
		{
			return new ClientAndServerParameters (clientParameters, serverParameters);
		}
	}
}

