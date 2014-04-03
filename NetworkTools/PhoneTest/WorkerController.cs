//
// WorkerController.cs
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
using System.Net;
using System.Collections.Generic;
using System.Linq;
using MonoTouch.Foundation;
using MonoTouch.UIKit;
using MonoTouch.Dialog;

namespace Xamarin.NetworkUtils.PhoneTest
{
	using ConnectionReuse;

	public class WorkerController : DialogViewController
	{
		CheckPortsRequestWorker worker;

		public WorkerController ()
			: base (new RootElement ("Network Worker"))
		{
			worker = new CheckPortsRequestWorker (Settings.Instance.Uri);

			var controlSection = new Section ();
			Root.Add (controlSection);

			var urlEntry = new EntryElement ("URL", "<worker url>", Settings.Instance.Uri.AbsoluteUri);
			controlSection.Add (urlEntry);
			urlEntry.Changed += (sender, e) => UriChanged (urlEntry.Value);
			UriChanged (urlEntry.Value);

			var startButton = new StyledStringElement ("Start network worker");
			controlSection.Add (startButton);
			startButton.Tapped += () => {
				Settings.Instance.UsePortFilter = true;
				worker.StartOne ();
			};

			var stopButton = new StyledStringElement ("Stop network worker");
			controlSection.Add (stopButton);
			stopButton.Tapped += () => {
				worker.StopOne ();
			};

			var statusSection = new Section ();
			Root.Add (statusSection);

			var countElement = new StringElement ("Network workers");
			statusSection.Add (countElement);

			var requestElement = new StringElement ("Requests");
			statusSection.Add (requestElement);

			var errorElement = new StringElement ("Errors");
			statusSection.Add (errorElement);

			var openSocketsElement = new StringElement ("Open Sockets");
			statusSection.Add (openSocketsElement);

			var knownPortsElement = new StringElement ("Known Ports");
			statusSection.Add (knownPortsElement);

			var timer = NSTimer.CreateRepeatingTimer (1.0, delegate {
				countElement.Value = worker.NumWorkers.ToString ();
				requestElement.Value = worker.RequestCount.ToString ();
				errorElement.Value = worker.ErrorCount.ToString ();

				var openSockets = GetOpenSockets ();
				openSocketsElement.Value = openSockets.ToString ();

				knownPortsElement.Value = worker.KnownPorts.ToString ();

				Root.Reload (statusSection, UITableViewRowAnimation.None);
			});
			NSRunLoop.Main.AddTimer (timer, NSRunLoopMode.Default);

			Settings.Instance.Modified += (sender, e) => {
				ServicePointManager.MaxServicePoints = 0;
			};
		}

		void UriChanged (string newUri)
		{
			var uri = new Uri (newUri);
			Settings.Instance.Uri = worker.Uri = uri;
			Settings.Instance.PortFilter = uri.Port;
		}

		int GetOpenSockets ()
		{
			int count = 0;
			int port = Settings.Instance.PortFilter;
			foreach (var entry in ManagedNetstat.GetTcp ()) {
				if (entry.RemoteEndpoint.Port != port)
					continue;
				if (entry.State != TcpState.Established)
					continue;
				count++;
			}
			return count;
		}
	}
}

