//
// ExternalDomainClient.cs
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
using System.IO;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Xamarin.AsyncTests.Remoting
{
	using Portable;
	using Framework;

	class ExternalDomainClient : Connection, IClientConnection
	{
		IExternalDomainHost host;

		protected override bool IsServer => false;

		Connection IClientConnection.Connection => this;

		public ExternalDomainClient (TestApp app, IExternalDomainHost host)
			: base (app)
		{
			this.host = host;
		}

		protected override Task MainLoop ()
		{
			return host.WaitForCompletion (cancelCts.Token);
		}

		internal override async Task OnStart (CancellationToken cancellationToken)
		{
			var handshake = new Handshake { WantStatisticsEvents = true, Settings = App.Settings };

			await RemoteObjectManager.Handshake (this, App.Logger.Backend, handshake, cancellationToken);
		}

		public override void Stop ()
		{
			try {
				base.Stop ();
			} catch {
				;
			}

			try {
				if (host != null) {
					host.Dispose ();
					host = null;
				}
			} catch {
				;
			}
		}

		protected override async Task SendMessage (Message message)
		{
			var element = message.Write (this);

			var response = await host.SendMessage (element, cancelCts.Token).ConfigureAwait (false);
			if (response == null)
				return;

			var objectID = response.Attribute ("ObjectID").Value;
			var operation = GetResponse (long.Parse (objectID));
			operation.Response.Read (this, response);
			operation.Task.SetResult (true);
		}
	}
}
