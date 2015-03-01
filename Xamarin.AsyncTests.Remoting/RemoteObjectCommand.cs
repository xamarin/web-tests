//
// RemoteObjectCommand.cs
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
using System.Xml.Linq;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Xamarin.AsyncTests.Remoting
{
	abstract class RemoteObjectCommand<T,A,R> : Command<A,R>
		where T : class, RemoteObject
	{
		public T Proxy {
			get { return proxy; }
		}

		T proxy;

		public async Task<R> Send (T proxy, A argument, CancellationToken cancellationToken)
		{
			this.proxy = proxy;

			try {
				return await base.Send (proxy.Connection, argument, cancellationToken).ConfigureAwait (false);
			} finally {
				proxy = null;
			}
		}

		public override void Read (Connection connection, XElement node)
		{
			base.Read (connection, node);

			var instanceID = long.Parse (node.Attribute ("InstanceID").Value);
			if (!connection.TryGetRemoteObject (instanceID, out proxy))
				throw new ServerErrorException ();
		}

		public override void Write (Connection connection, XElement node)
		{
			base.Write (connection, node);

			node.SetAttributeValue ("InstanceID", proxy.ObjectID);
		}

		protected sealed override Task<R> Run (Connection connection, A argument, CancellationToken cancellationToken)
		{
			return Run (connection, proxy, argument, cancellationToken);
		}

		protected abstract Task<R> Run (
			Connection connection, T proxy, A argument, CancellationToken cancellationToken);
	}
}

