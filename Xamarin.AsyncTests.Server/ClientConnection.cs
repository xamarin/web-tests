//
// ClientConnection.cs
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
	public abstract class ClientConnection : Connection
	{
		public ClientConnection (Stream stream)
			: base (stream)
		{
		}

		public abstract TestContext Context {
			get;
		}

		#region Public API

		public async Task LoadResult (TestResult result, CancellationToken cancellationToken)
		{
			var command = new LoadResultCommand { Result = result };
			await SendCommand (command);
		}

		#endregion

		CancellationTokenSource remoteCts;
		long remoteID;

		internal override Task Run (Command command, CancellationToken cancellationToken)
		{
			var clientCommand = command as IClientCommand;
			if (clientCommand == null)
				throw new InvalidOperationException ();

			return clientCommand.Run (this, cancellationToken);
		}

		internal Task Run (RunTestCommand command, CancellationToken cancellationToken)
		{
			CancellationToken token;
			lock (this) {
				remoteCts = new CancellationTokenSource ();
				token = remoteCts.Token;
				remoteID = command.ObjectID;
			}

			var test = Serializer.GetTest (command.ObjectID);
			Task.Factory.StartNew (() => RunRemoteTest (command, test, token));
			return Task.FromResult<object> (null);
		}

		async void RunRemoteTest (RunTestCommand command, TestCase test, CancellationToken cancellationToken)
		{
			var result = new TestResult (test.Name);

			try {
				await test.Run (Context, result, cancellationToken).ConfigureAwait (false);
			} catch (OperationCanceledException) {
				result.Status = TestStatus.Canceled;
			} catch (Exception ex) {
				result.Error = ex;
			}

			var resultCommand = new LoadResultCommand { ObjectID = command.ObjectID, Result = result };
			await SendCommand (resultCommand);

			lock (this) {
				remoteCts = null;
				remoteID = 0;
			}
		}

		internal Task Run (CancelCommand command, CancellationToken cancellationToken)
		{
			return Task.Run (() => {
				lock (this) {
					if (remoteCts == null || command.ObjectID != remoteID)
						return;
					remoteCts.Cancel ();
				}
			});
		}
	}
}

