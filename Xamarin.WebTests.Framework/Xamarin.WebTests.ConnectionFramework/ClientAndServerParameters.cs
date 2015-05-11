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

		public ClientAndServerParameters (ClientParameters clientParameters, ServerParameters serverParameters)
			: this (clientParameters.Identifier + ":" + serverParameters.Identifier, clientParameters, serverParameters)
		{
		}

		protected ClientAndServerParameters (string identifier, ClientParameters clientParameters, ServerParameters serverParameters)
			: base (identifier)
		{
			this.clientParameters = clientParameters;
			this.serverParameters = serverParameters;
		}

		public ClientAndServerParameters (string identifier, IServerCertificate serverCertificate)
			: base (identifier)
		{
			clientParameters = new ClientParameters (identifier);
			serverParameters = new ServerParameters (identifier, serverCertificate);
		}

		protected ClientAndServerParameters (ClientAndServerParameters other)
			: this (other.Identifier, (ClientParameters)other.ClientParameters.DeepClone (), (ServerParameters)other.ServerParameters.DeepClone ())
		{
		}

		public override ConnectionParameters DeepClone ()
		{
			return new ClientAndServerParameters (this);
		}

		public ClientParameters ClientParameters {
			get { return clientParameters; }
		}

		public ServerParameters ServerParameters {
			get { return serverParameters; }
		}

		public IClientCertificate ClientCertificate {
			get { return ClientParameters.ClientCertificate; }
			set { ClientParameters.ClientCertificate = value; }
		}

		public ICertificateValidator ClientCertificateValidator {
			get { return ClientParameters.ClientCertificateValidator; }
			set { ClientParameters.ClientCertificateValidator = value; }
		}

		public ICertificateValidator ServerCertificateValidator {
			get { return ServerParameters.ServerCertificateValidator; }
			set { ServerParameters.ServerCertificateValidator = value; }
		}

		public ClientFlags ClientFlags {
			get { return ClientParameters.Flags; }
			set { ClientParameters.Flags = value; }
		}

		public ServerFlags ServerFlags {
			get { return ServerParameters.Flags; }
			set { ServerParameters.Flags = value; }
		}
	}
}

