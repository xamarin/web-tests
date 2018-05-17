//
// SslAuthenticationOptionsProvider.cs
//
// Author:
//       Martin Baulig <mabaul@microsoft.com>
//
// Copyright (c) 2018 Xamarin Inc. (http://www.xamarin.com)
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
using System.Threading;
using System.Threading.Tasks;
using System.Net.Security;
using System.Reflection;

namespace Xamarin.WebTests.TestProvider
{
	using System.Security.Cryptography.X509Certificates;
	using ConnectionFramework;

	class SslAuthenticationOptionsProvider : ISslAuthenticationOptionsProvider
	{
		public SslAuthenticationOptionsProvider ()
		{
			SslStreamType = typeof (SslStream);
			ClientType = SslStreamType.Assembly.GetType ("System.Net.Security.SslClientAuthenticationOptions");
			ServerType = SslStreamType.Assembly.GetType ("System.Net.Security.SslServerAuthenticationOptions");
			IsSupported = ClientType != null && ServerType != null;

			if (!IsSupported)
				return;

			ClientAuthMethod = SslStreamType.GetMethod ("AuthenticateAsClientAsync", new[] { ClientType, typeof (CancellationToken) });
			ServerAuthMethod = SslStreamType.GetMethod ("AuthenticateAsServerAsync", new[] { ServerType, typeof (CancellationToken) });
			TargetHost = ClientType.GetProperty ("TargetHost");
			ClientCertificates = ClientType.GetProperty ("ClientCertificates");
			LocalCertificateSelectionCallback = ClientType.GetProperty ("LocalCertificateSelectionCallback");
			RemoteCertificateValidationCallback = ClientType.GetProperty ("RemoteCertificateValidationCallback");
			ClientCertificateRequired = ServerType.GetProperty ("ClientCertificateRequired");
			ServerCertificate = ServerType.GetProperty ("ServerCertificate");
			AllowClientRenegotiation = ClientType.GetProperty ("AllowRenegotiation");
			AllowServerRenegotiation = ServerType.GetProperty ("AllowRenegotiation");
		}

		public Type SslStreamType {
			get;
		}

		public Type ClientType {
			get;
		}

		public Type ServerType {
			get;
		}

		public MethodInfo ClientAuthMethod {
			get;
		}

		public MethodInfo ServerAuthMethod {
			get;
		}

		public PropertyInfo AllowClientRenegotiation {
			get;
		}

		public PropertyInfo AllowServerRenegotiation {
			get;
		}

		public PropertyInfo TargetHost {
			get;
		}

		public PropertyInfo ClientCertificates {
			get;
		}

		public PropertyInfo LocalCertificateSelectionCallback {
			get;
		}

		public PropertyInfo RemoteCertificateValidationCallback {
			get;
		}

		public PropertyInfo ClientCertificateRequired {
			get;
		}

		public PropertyInfo ServerCertificate {
			get;
		}

		public bool IsSupported {
			get;
		}

		public ISslClientAuthenticationOptions CreateClientOptions ()
		{
			if (!IsSupported)
				throw new NotSupportedException ();

			var instance = Activator.CreateInstance (ClientType);
			return new ClientOptions (this, instance);
		}

		public ISslServerAuthenticationOptions CreateServerOptions ()
		{
			if (!IsSupported)
				throw new NotSupportedException ();

			var instance = Activator.CreateInstance (ServerType);
			return new ServerOptions (this, instance);
		}

		public Task AuthenticateAsClientAsync (ISslClientAuthenticationOptions options, SslStream stream, CancellationToken cancellationToken)
		{
			return (Task)ClientAuthMethod.Invoke (stream, new object[] { ((ClientOptions)options).Instance, cancellationToken });
		}

		public Task AuthenticateAsServerAsync (ISslServerAuthenticationOptions options, SslStream stream, CancellationToken cancellationToken)
		{
			return (Task)ServerAuthMethod.Invoke (stream, new object[] { ((ServerOptions)options).Instance, cancellationToken });
		}

		abstract class Options : ISslAuthenticationOptions
		{
			public SslAuthenticationOptionsProvider Provider {
				get;
			}

			public object Instance {
				get;
			}

			public abstract bool AllowRenegotiation {
				get; set;
			}

			protected Options (SslAuthenticationOptionsProvider provider, object instance)
			{
				Provider = provider;
				Instance = instance;
			}

			protected T GetProperty<T> (PropertyInfo property)
			{
				return (T)property.GetValue (Instance);
			}

			protected void SetProperty<T> (PropertyInfo property, T value)
			{
				property.SetValue (Instance, value);
			}
		}

		class ClientOptions : Options, ISslClientAuthenticationOptions
		{
			public ClientOptions (SslAuthenticationOptionsProvider provider, object instance)
				: base (provider, instance)
			{
			}

			public override bool AllowRenegotiation {
				get => GetProperty<bool> (Provider.AllowClientRenegotiation);
				set => SetProperty (Provider.AllowClientRenegotiation, value);
			}

			public string TargetHost {
				get => GetProperty<string> (Provider.TargetHost);
				set => SetProperty (Provider.TargetHost, value);
			}

			public X509CertificateCollection ClientCertificates {
				get => GetProperty<X509CertificateCollection> (Provider.ClientCertificates);
				set => SetProperty (Provider.ClientCertificates, value);
			}

			public LocalCertificateSelectionCallback LocalCertificateSelectionCallback {
				get => GetProperty<LocalCertificateSelectionCallback> (Provider.LocalCertificateSelectionCallback);
				set => SetProperty (Provider.LocalCertificateSelectionCallback, value);
			}

			public RemoteCertificateValidationCallback RemoteCertificateValidationCallback {
				get => GetProperty<RemoteCertificateValidationCallback> (Provider.RemoteCertificateValidationCallback);
				set => SetProperty (Provider.RemoteCertificateValidationCallback, value);
			}
		}

		class ServerOptions : Options, ISslServerAuthenticationOptions
		{
			public ServerOptions (SslAuthenticationOptionsProvider provider, object instance)
				: base (provider, instance)
			{
			}

			public override bool AllowRenegotiation {
				get => GetProperty<bool> (Provider.AllowServerRenegotiation);
				set => SetProperty (Provider.AllowServerRenegotiation, value);
			}

			public bool ClientCertificateRequired {
				get => GetProperty<bool> (Provider.ClientCertificateRequired);
				set => SetProperty (Provider.ClientCertificateRequired, value);
			}

			public X509Certificate ServerCertificate {
				get => GetProperty<X509Certificate> (Provider.ServerCertificate);
				set => SetProperty (Provider.ServerCertificate, value);
			}
		}
	}
}
