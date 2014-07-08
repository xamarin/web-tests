//
// ServerControlModel.cs
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
using System.Windows.Input;
using Xamarin.Forms;

namespace Xamarin.AsyncTests.UI
{
	using Framework;

	public class ServerControlModel : BindableObject
	{
		public TestApp App {
			get;
			private set;
		}

		public ServerControlModel (TestApp app)
		{
			App = app;

			CurrentTestRunner = App.RootTestRunner;

			App.TestSuiteManager.PropertyChanged += (sender, e) => {
				switch (e.PropertyName) {
				case "HasInstance":
					OnPropertyChanged ("CanRun");
					break;
				}
			};
		}

		#region Test Runner

		TestRunnerModel currentRunner;
		public TestRunnerModel CurrentTestRunner {
			get { return currentRunner; }
			set {
				currentRunner = value;
				OnPropertyChanged ("CurrentTestRunner");
			}
		}

		bool running;
		public bool IsRunning {
			get { return running; }
			set {
				running = value;
				OnPropertyChanged ("IsRunning");
				OnPropertyChanged ("CanStop");
				OnPropertyChanged ("CanRun");
				OnPropertyChanged ("IsStopped");
			}
		}

		public bool CanStop {
			get { return running; }
		}

		public bool CanRun {
			get { return !running && CurrentTestRunner.Test != null; }
		}

		public bool IsStopped {
			get { return !running; }
		}

		CancellationTokenSource cancelCts;

		internal async Task<bool> Run (bool repeat)
		{
			lock (this) {
				if (!CanRun || IsRunning)
					return false;
				if (cancelCts != null)
					return false;
			}

			cancelCts = new CancellationTokenSource ();
			IsRunning = true;

			App.Context.ResetStatistics ();

			try {
				await CurrentTestRunner.Run (repeat, cancelCts.Token);
				return true;
			} finally {
				IsRunning = false;
				cancelCts.Dispose ();
				cancelCts = null;
			}
		}

		internal void Stop ()
		{
			lock (this) {
				if (!IsRunning || cancelCts == null)
					return;

				cancelCts.Cancel ();
				cancelCts = null;
			}
		}

		#endregion

	}
}

