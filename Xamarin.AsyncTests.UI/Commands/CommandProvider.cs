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

namespace Xamarin.AsyncTests.UI
{
	using Binding;

	public abstract class CommandProvider : BindableObject
	{
		public UITestApp App {
			get;
			private set;
		}

		public CommandProvider (UITestApp app)
		{
			App = app;

			stopCommand = new StopCommand (this);
			CanStart = true;
		}

		public Command Stop {
			get { return stopCommand; }
		}

		readonly StopCommand stopCommand;

		public static readonly BindableProperty CanStartProperty =
			BindableProperty.Create ("CanStart", typeof(bool), typeof(CommandProvider), false,
				propertyChanged: (bo, o, n) => ((CommandProvider)bo).OnCanStartChanged ((bool)n));

		public bool CanStart {
			get { return (bool)GetValue (CanStartProperty); }
			set { SetValue (CanStartProperty, value); }
		}

		protected void OnCanStartChanged (bool canStart)
		{
			if (CanStartChanged != null)
				CanStartChanged (this, canStart);
		}

		public event EventHandler<bool> CanStartChanged;

		public static readonly BindableProperty CanStopProperty =
			BindableProperty.Create ("CanStop", typeof(bool), typeof(CommandProvider), false,
				propertyChanged: (bo, o, n) => ((CommandProvider)bo).OnCanStopChanged ((bool)n));

		public bool CanStop {
			get { return (bool)GetValue (CanStopProperty); }
			set { SetValue (CanStopProperty, value); }
		}

		protected void OnCanStopChanged (bool canStop)
		{
			if (CanStopChanged != null)
				CanStopChanged (this, canStop);
		}

		public event EventHandler<bool> CanStopChanged;

		public static readonly BindableProperty HasInstanceProperty =
			BindableProperty.Create ("HasInstance", typeof(bool), typeof(CommandProvider), false,
				propertyChanged: (bo, o, n) => ((CommandProvider)bo).OnHasInstanceChanged ((bool)n));

		public bool HasInstance {
			get { return (bool)GetValue (HasInstanceProperty); }
			set { SetValue (HasInstanceProperty, value); }
		}

		protected void OnHasInstanceChanged (bool hasInstance)
		{
			if (HasInstanceChanged != null)
				HasInstanceChanged (this, hasInstance);
		}

		public event EventHandler<bool> HasInstanceChanged;

		public static readonly BindableProperty StatusMessageProperty =
			BindableProperty.Create ("StatusMessage", typeof(string), typeof(CommandProvider), string.Empty,
				propertyChanged: (bo, o, n) => ((CommandProvider)bo).OnStatusMessageChanged ((string)n));

		public string StatusMessage {
			get { return (string)GetValue(StatusMessageProperty); }
			set { SetValue (StatusMessageProperty, value); }
		}

		protected virtual void OnStatusMessageChanged (string message)
		{
		}

		internal void SetStatusMessage (string message, params object[] args)
		{
			StatusMessage = string.Format (message, args);
		}

		internal abstract Task ExecuteStop ();

		class StopCommand : Command
		{
			public StopCommand (CommandProvider command)
				: base (command)
			{
				command.CanStopChanged += (sender, e) => OnCanExecuteChanged ();
			}

			public override bool IsEnabled ()
			{
				return Provider.CanStop;
			}

			public override Task Execute ()
			{
				return Provider.ExecuteStop ();
			}
		}
	}

	public abstract class CommandProvider<T> : CommandProvider
		where T : class
	{
		public CommandProvider (UITestApp app)
			: base (app)
		{
		}

		public readonly BindableProperty InstanceProperty = BindableProperty.Create (
			"Instance", typeof (T), typeof (CommandProvider<T>), null,
			propertyChanged: (bo, o, n) => ((CommandProvider<T>)bo).OnInstanceChanged ((T)n));

		public T Instance {
			get { return (T)GetValue (InstanceProperty); }
			set { SetValue (InstanceProperty, value); }
		}

		protected virtual void OnInstanceChanged (T instance)
		{
			HasInstance = instance != null;
		}

		volatile Command<T> currentCommand;
		volatile TaskCompletionSource<T> startTcs;
		volatile CancellationTokenSource cts;

		internal Task ExecuteStart (Command<T> command)
		{
			lock (this) {
				if (startTcs != null || !CanStart || !command.CanExecute)
					return Task.FromResult<object> (null);
				CanStart = false;
				CanStop = true;
				currentCommand = command;
				startTcs = new TaskCompletionSource<T> ();
				cts = new CancellationTokenSource ();
			}

			Task.Factory.StartNew (async () => {
				var token = cts.Token;
				try {
					Instance = await command.Start (token);
					startTcs.SetResult (Instance);
				} catch (OperationCanceledException) {
					startTcs.SetCanceled ();
				} catch (Exception ex) {
					SetStatusMessage ("Command failed: {0}", ex.Message);
					startTcs.SetException (ex);
				}

				try {
					token.ThrowIfCancellationRequested ();
					bool running = await command.Run (Instance, token);
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
				CanStop = false;
				instance = Instance;
				Instance = null;
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
				CanStart = true;
			}
		}
	}
}

