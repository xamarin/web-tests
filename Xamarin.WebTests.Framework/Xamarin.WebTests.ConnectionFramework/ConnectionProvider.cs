//
// ConnectionProvider.cs
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
using System.Net.Security;
using System.Threading;
using System.Threading.Tasks;
using System.Security.Cryptography.X509Certificates;
using Xamarin.AsyncTests;

namespace Xamarin.WebTests.ConnectionFramework
{
	using ConnectionFramework;

	public abstract class ConnectionProvider : ITestParameter
	{
		readonly ConnectionProviderFactory factory;
		readonly ConnectionProviderType type;
		readonly ConnectionProviderFlags flags;

		protected ConnectionProvider (ConnectionProviderFactory factory, ConnectionProviderType type, ConnectionProviderFlags flags)
		{
			this.factory = factory;
			this.type = type;
			this.flags = flags;
		}

		string ITestParameter.Value {
			get { return Type.ToString (); }
		}

		string ITestParameter.FriendlyValue => Type.ToString ();

		public ConnectionProviderFactory Factory {
			get { return factory; }
		}

		public virtual string Name {
			get { return Type.ToString (); }
		}

		public ConnectionProviderType Type {
			get { return type; }
		}

		public ConnectionProviderFlags Flags {
			get { return flags; }
		}

		public abstract Connection CreateClient (ConnectionParameters parameters);

		public abstract Connection CreateServer (ConnectionParameters parameters);

		public bool SupportsSslStreams => (Flags & ConnectionProviderFlags.SupportsSslStream) != 0;

		public bool SupportsMonoExtensions => (Flags & ConnectionProviderFlags.SupportsMonoExtensions) != 0;

		public bool SupportsCleanShutdown => (Flags & ConnectionProviderFlags.SupportsCleanShutdown) != 0;

		public bool HasFlag (ConnectionProviderFlags flags)
		{
			return (Flags & flags) == flags;
		}

		public abstract ProtocolVersions SupportedProtocols {
			get;
		}

		public ISslStreamProvider SslStreamProvider {
			get {
				if (!SupportsSslStreams)
					throw new InvalidOperationException ();
				return GetSslStreamProvider ();
			}
		}

		protected abstract ISslStreamProvider GetSslStreamProvider ();

		public bool SupportsHttp {
			get { return (Flags & ConnectionProviderFlags.SupportsHttp) != 0; }
		}

		public virtual X509Certificate GetCertificateFromData (byte[] data)
		{
			return new X509Certificate (data);
		}

		public virtual X509Certificate2 GetCertificate2FromData (byte[] data)
		{
			return new X509Certificate2 (data);
		}
	}
}

