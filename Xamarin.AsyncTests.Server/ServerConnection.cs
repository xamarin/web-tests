//
// ServerConnection.cs
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
using System.Threading;
using System.Threading.Tasks;

namespace Xamarin.AsyncTests.Server
{
	public abstract class ServerConnection : Connection
	{
		public ServerConnection (Stream stream)
			: base (stream)
		{
		}

		public abstract TestContext Context {
			get;
		}

		internal override Task Run (Command command)
		{
			var serverCommand = command as IServerCommand;
			if (serverCommand == null)
				throw new InvalidOperationException ();

			return serverCommand.Run (this, CancellationToken.None);
		}

		internal async Task Run (LoadResultCommand command, CancellationToken cancellationToken)
		{
			lock (this) {
				if (remoteTest != null) {
					if (command.ObjectID != remoteTest.ObjectID) {
						Debug ("UNKNOWN RESULT: {0} {1}", command.ObjectID, remoteTest.ObjectID);
						return;
					}

					remoteTcs.SetResult (command.Result);
					remoteCts.Dispose ();
					remoteCts = null;
					return;
				}
			}

			await OnLoadResult (command.Result);
		}

		TaskCompletionSource<TestResult> remoteTcs;
		CancellationTokenSource remoteCts;
		RemoteTestCase remoteTest;

		internal async Task<bool> Run (RemoteTestCase test, TestResult result, CancellationToken cancellationToken)
		{
			CancellationTokenSource cts;
			lock (this) {
				if (remoteTcs != null)
					throw new InvalidOperationException ();
				remoteTcs = new TaskCompletionSource<TestResult> ();
				cts = remoteCts = CancellationTokenSource.CreateLinkedTokenSource (cancellationToken);
				remoteTest = test;
			}

			cts.Token.Register (async () => {
				Debug ("CANCEL!");
				var cancelCommand = new CancelCommand { ObjectID = test.ObjectID };
				await SendCommand (cancelCommand);
				Debug ("DONE SENDING CANCEL");
			});

			var command = new RunTestCommand { ObjectID = test.ObjectID };
			await SendCommand (command);

			Debug ("RUN REMOTE TEST WAITING");

			try {
				var remoteResult = await remoteTcs.Task;
				Debug ("GOT REMOTE RESULT: {0}", remoteResult);
				result.AddChild (remoteResult);
				result.MergeStatus (remoteResult.Status);
				return true;
			} catch (Exception ex) {
				Debug ("SEND COMMAND ERROR: {0}", ex);
				result.Error = ex;
				return false;
			} finally {
				Debug ("RUN REMOTE TEST DONE");
				lock (this) {
					remoteTcs = null;
					remoteCts = null;
					remoteTest = null;
				}
			}
		}

		#region Public API

		#endregion

		#region Server API

		protected abstract Task OnLoadResult (TestResult result);

		#endregion
	}
}

