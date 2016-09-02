//
// ServerHost.cs
//
// Author:
//       Martin Baulig <martin.baulig@xamarin.com>
//
// Copyright (c) 2014 Xamarin Inc. (http://www.xamarin.com)
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
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

using Xamarin.AsyncTests.Portable;

namespace Xamarin.AsyncTests.Portable
{
	using Portable;

	class ServerHostImpl : IServerHost
	{
		public Task<IServerConnection> Listen (IPortableEndPoint endpoint, CancellationToken cancellationToken)
		{
			return Task.Run<IServerConnection> (() => {
				var networkEndpoint = PortableEndPointSupportImpl.GetEndpoint (endpoint);
				var listener = new TcpListener (networkEndpoint);
				listener.Start ();
				Debug.WriteLine ("Server started: {0}", listener.LocalEndpoint);
				return new ServerConnection (listener);
			});
		}

		public async Task<IServerConnection> Connect (IPortableEndPoint endpoint, CancellationToken cancellationToken)
		{
			var networkEndpoint = PortableEndPointSupportImpl.GetEndpoint (endpoint);
			var client = new TcpClient ();
			await client.ConnectAsync (networkEndpoint.Address, networkEndpoint.Port);
			return new ClientConnection (client);
		}

		public Task<IPipeConnection> CreatePipe (IPortableEndPoint endpoint, PipeArguments arguments, CancellationToken cancellationToken)
		{
#if !__TVOS__
			return Task.Run<IPipeConnection> (() => {
				var networkEndpoint = PortableEndPointSupportImpl.GetEndpoint (endpoint);
				var listener = new TcpListener (networkEndpoint);
				listener.Start ();

				var monoPath = Path.Combine (arguments.MonoPrefix, "bin", "mono");

				var cmd = new StringBuilder ();
				cmd.Append ("--debug ");
				if (arguments.ConsolePath != null)
					cmd.Append (arguments.ConsolePath);
				else
					cmd.Append (arguments.Assembly);
				if (arguments.Dependencies != null) {
					foreach (var dependency in arguments.Dependencies) {
						cmd.AppendFormat (" --dependency={0}", dependency);
					}
				}
				cmd.AppendFormat (" --gui={0}:{1}", networkEndpoint.Address, networkEndpoint.Port);
				if (arguments.ExtraArguments != null)
					cmd.AppendFormat (" {0}", arguments.ExtraArguments);
				if (arguments.ConsolePath != null) {
					cmd.Append (" ");
					cmd.Append (arguments.Assembly);
				}

				var psi = new ProcessStartInfo (monoPath, cmd.ToString ());
				psi.UseShellExecute = false;

				foreach (var reserved in ReservedNames) {
					psi.EnvironmentVariables.Remove (reserved);
				}
			
				var process = Process.Start (psi);

				return new PipeConnection (listener, process);
			});
#else
			throw new NotSupportedException ();
#endif
		}

#if !__TVOS__
		static readonly string[] ReservedNames = { "MONO_RUNTIME", "MONO_PATH", "MONO_GAC_PREFIX" };
#endif

		class PipeConnection : ServerConnection, IPipeConnection
		{
			public Process Process {
				get;
				private set;
			}

			public PipeConnection (TcpListener listener, Process process)
				: base (listener)
			{
				Process = process;
				process.Exited += (sender, e) => {;
					if (ExitedEvent != null)
						ExitedEvent (sender, e);
				};
			}

			public event EventHandler ExitedEvent;

			public bool HasExited {
				get { return Process.HasExited; }
			}

			public override void Close ()
			{
				base.Close ();

				try {
					if (Process != null) {
						Process.Kill ();
						Process = null;
					}
				} catch {
				}
			}	
		}

		class ClientConnection : IServerConnection
		{
			TcpClient client;
			NetworkStream stream;

			public ClientConnection (TcpClient client)
			{
				this.client = client;

				Name = client.Client.RemoteEndPoint.ToString ();
			}

			public string Name {
				get;
				private set;
			}

			public Task<Stream> Open (CancellationToken cancellationToken)
			{
				return Task.Run<Stream> (() => {
					stream = client.GetStream ();
					return stream;
				});
			}


			public void Close ()
			{
				try {
					if (stream != null) {
						stream.Close ();
						stream = null;
					}
				} catch {
					;
				}
				try {
					if (client != null) {
						client.Close ();
						client = null;
					}
				} catch {
					;
				}
			}

		}

		class ServerConnection : IServerConnection
		{
			TcpListener listener;
			Socket socket;
			NetworkStream stream;

			public ServerConnection (TcpListener listener)
			{
				this.listener = listener;
				Name = listener.LocalEndpoint.ToString ();
			}

			public string Name {
				get;
				private set;
			}

			public async Task<Stream> Open (CancellationToken cancellationToken)
			{
				var cts = CancellationTokenSource.CreateLinkedTokenSource (cancellationToken);
				cts.Token.Register (() => listener.Stop ());

				try {
					socket = await listener.AcceptSocketAsync ();
					cts.Token.ThrowIfCancellationRequested ();
				} catch (Exception ex) {
					if (!cancellationToken.IsCancellationRequested)
						Debug.WriteLine ("Failed to start server: {0}", ex);
					throw;
				}

				Debug.WriteLine ("Server accepted connection from {0}.", socket.RemoteEndPoint);
				stream = new NetworkStream (socket);
				return stream;
			}

			public virtual void Close ()
			{
				try {
					if (socket != null) {
						socket.Shutdown (SocketShutdown.Both);
						socket.Dispose ();
						socket = null;
					}
				} catch {
					;
				}
				try {
					if (stream != null) {
						stream.Close ();
						stream = null;
					}
				} catch {
					;
				}
				try {
					listener.Stop ();
				} catch {
					;
				}
			}
		}
	}
}

