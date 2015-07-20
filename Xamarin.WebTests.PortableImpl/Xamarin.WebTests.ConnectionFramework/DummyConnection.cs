//
// DummyConnection.cs
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
using Xamarin.AsyncTests;
using Xamarin.AsyncTests.Portable;

namespace Xamarin.WebTests.ConnectionFramework
{
	using Providers;

	public class DummyConnection : Connection, ICommonConnection
	{
		readonly ConnectionProvider provider;

		public DummyConnection (ConnectionProvider provider, IPortableEndPoint endpoint, ConnectionParameters parameters)
			: base (endpoint, parameters)
		{
			this.provider = provider;
		}

		public override Task Start (TestContext ctx, CancellationToken cancellationToken)
		{
			return FinishedTask;
		}

		public override Task WaitForConnection (TestContext ctx, CancellationToken cancellationToken)
		{
			return FinishedTask;
		}

		public override Task<bool> Shutdown (TestContext ctx, CancellationToken cancellationToken)
		{
			return Task.FromResult (false);
		}

		protected override void Stop ()
		{
			;
		}

		public ConnectionProvider Provider {
			get { return provider; }
		}

		public override bool SupportsCleanShutdown {
			get { return false; }
		}

		public override ProtocolVersions SupportedProtocols {
			get { return ProtocolVersions.Tls10 | ProtocolVersions.Tls11 | ProtocolVersions.Tls12; }
		}

		public ProtocolVersions ProtocolVersion {
			get {
				throw new NotImplementedException ();
			}
		}

		public ISslStream SslStream {
			get {
				throw new NotSupportedException ();
			}
		}

		public Stream Stream {
			get {
				throw new NotSupportedException ();
			}
		}

		public IStreamInstrumentation StreamInstrumentation {
			get {
				throw new NotSupportedException ();
			}
		}
	}
}

