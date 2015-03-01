//
// Command.cs
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
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using Xamarin.AsyncTests;
using Xamarin.AsyncTests.Framework;

namespace Xamarin.AsyncTests.MacUI
{
	public abstract class CommandProvider
	{
		public MacUI App {
			get;
			private set;
		}

		public CommandProvider (MacUI app, CommandProvider parent)
		{
			App = app;

			IsEnabled = new BooleanProperty ("IsEnabled", true);
			CanStart = new BooleanProperty ("CanStart", true);
			CanStop = new BooleanProperty ("CanStop", false);

			notifyCanExecute = new NotifyStateChanged (IsEnabled, CanStart);
			if (parent != null)
				notifyCanExecute.Register (parent.NotifyHasInstance);

			stopCommand = new StopCommand (this);
		}

		public StopCommand Stop {
			get { return stopCommand; }
		}

		readonly StopCommand stopCommand;
		readonly NotifyStateChanged notifyCanExecute;

		public BooleanProperty IsEnabled {
			get;
			private set;
		}

		public BooleanProperty CanStart {
			get;
			private set;
		}

		public BooleanProperty CanStop {
			get;
			private set;
		}

		public INotifyStateChanged NotifyCanExecute {
			get { return notifyCanExecute; }
		}

		public abstract INotifyStateChanged NotifyHasInstance {
			get;
		}

		public readonly Property<string> StatusMessage = new InstanceProperty<string> ("StatusMessage", string.Empty);

		internal void SetStatusMessage (string message, params object[] args)
		{
			StatusMessage.Value = string.Format (message, args);
		}

		internal abstract Task ExecuteStop ();
	}

	public abstract class CommandProvider<T> : CommandProvider
		where T : class
	{
		public CommandProvider (MacUI app, CommandProvider parent)
			: base (app, parent)
		{
			instanceProperty = new InstanceProperty<T> ("Instance", null);
		}

		readonly InstanceProperty<T> instanceProperty;

		public Property<T> Instance {
			get { return instanceProperty; }
		}

		public override INotifyStateChanged NotifyHasInstance {
			get { return instanceProperty; }
		}

		volatile Command<T> currentCommand;
		volatile TaskCompletionSource<T> startTcs;
		volatile CancellationTokenSource cts;

		internal Task<T> ExecuteStart<U> (Command<T,U> command, U argument)
		{
			lock (this) {
				if (startTcs != null || !CanStart || !command.CanExecute)
					return Task.FromResult<T> (null);
				CanStart.Value = false;
				CanStop.Value = true;
				currentCommand = command;
				startTcs = new TaskCompletionSource<T> ();
				cts = new CancellationTokenSource ();
			}

			Task.Factory.StartNew (async () => {
				var token = cts.Token;
				T instance = null;
				try {
					instance = await command.Start (argument, token);
					Instance.Value = instance;
					startTcs.SetResult (instance);
				} catch (OperationCanceledException) {
					startTcs.SetCanceled ();
				} catch (Exception ex) {
					SetStatusMessage ("Command failed: {0}", ex.Message);
					startTcs.SetException (ex);
				}

				try {
					token.ThrowIfCancellationRequested ();
					bool running = await command.Run (instance, token);
					if (running)
						return;
				} catch (OperationCanceledException) {
					;
				} catch (Exception ex) {
					SetStatusMessage ("Command failed: {0}", ex.Message);
				}

				await ExecuteStop ();
			}, CancellationToken.None, TaskCreationOptions.None, TaskScheduler.FromCurrentSynchronizationContext ());

			return startTcs.Task;
		}

		internal async override Task ExecuteStop ()
		{
			T instance;
			lock (this) {
				if (startTcs == null)
					return;
				CanStop.Value = false;
				instance = Instance.Value;
				Instance.Value = null;
			}

			try {
				if (currentCommand != null)
					await currentCommand.Stop (instance, CancellationToken.None);
			} catch {
				;
			}

			lock (this) {
				if (cts != null) {
					cts.Cancel ();
					cts.Dispose ();
					cts = null;
				}
			}

			try {
				await startTcs.Task;
			} catch {
				;
			}

			lock (this) {
				startTcs = null;
				currentCommand = null;
				CanStart.Value = true;
			}
		}
	}
}

