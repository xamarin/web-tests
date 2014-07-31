//
// IStartStopCommand.cs
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
using Xamarin.Forms;

namespace Xamarin.AsyncTests.UI
{
	public abstract class Command : BindableObject, ICommand
	{
		public CommandProvider Provider {
			get;
			private set;
		}

		protected Command (CommandProvider provider)
		{
			Provider = provider;
			CanExecute = true;
		}

		public static readonly BindableProperty CanExecuteProperty =
			BindableProperty.Create ("CanExecute", typeof(bool), typeof(Command), false,
				propertyChanged: (bo, o, n) => ((Command)bo).OnCanExecuteChanged ());

		public bool CanExecute {
			get { return (bool)GetValue (CanExecuteProperty); }
			set { SetValue (CanExecuteProperty, value); }
		}

		protected void OnCanExecuteChanged ()
		{
			if (CanExecuteChanged != null)
				CanExecuteChanged (this, EventArgs.Empty);
		}

		public abstract bool IsEnabled ();

		public abstract Task Execute ();

		#region ICommand implementation

		public event EventHandler CanExecuteChanged;

		bool ICommand.CanExecute (object parameter)
		{
			return CanExecute && IsEnabled ();
		}

		async void ICommand.Execute (object parameter)
		{
			try {
				await Execute ();
			} catch {
				;
			}
		}

		#endregion
	}

	public abstract class Command<T> : Command
		where T : class
	{
		new readonly CommandProvider<T> Provider;

		public CommandProvider Parent {
			get;
			private set;
		}

		protected Command (CommandProvider<T> provider, CommandProvider parent = null)
			: base (provider)
		{
			Provider = provider;
			Parent = parent;

			Provider.CanStartChanged += (sender, e) => OnCanExecuteChanged ();
			if (Parent != null)
				Parent.HasInstanceChanged += (sender, e) => OnCanExecuteChanged ();
		}

		public override bool IsEnabled ()
		{
			if (!Provider.CanStart)
				return false;
			if (Parent != null && !Parent.HasInstance)
				return false;
			return true;
		}

		internal abstract Task<T> Start (CancellationToken cancellationToken);

		internal abstract Task<bool> Run (T instance, CancellationToken cancellationToken);

		internal abstract Task Stop (T instance, CancellationToken cancellationToken);

		public override async Task Execute ()
		{
			try {
				await Provider.ExecuteStart (this);
			} catch (Exception ex) {
				Provider.App.Logger.LogError (ex);
			}
		}
	}
}

