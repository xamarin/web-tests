//
// NotifyCanExecute.cs
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
using System.Collections.Generic;

namespace Xamarin.AsyncTests.MacUI
{
	class NotifyStateChanged : INotifyStateChanged
	{
		List<INotifyStateChanged> listeners;
		bool currentState;

		public NotifyStateChanged (params INotifyStateChanged[] parentListeners)
		{
			listeners = new List<INotifyStateChanged> (parentListeners);

			foreach (var listener in listeners)
				listener.StateChanged += (sender, e) => Update ();
			Update ();
		}

		public void Register (INotifyStateChanged listener)
		{
			listeners.Add (listener);
			listener.StateChanged += (sender, e) => Update ();
			Update ();
		}

		void OnStateChanged ()
		{
			if (StateChanged != null)
				StateChanged (null, currentState);
		}

		internal void Update ()
		{
			var newState = true;
			foreach (var listener in listeners) {
				if (!listener.State) {
					newState = false;
					break;
				}
			}

			if (newState == currentState)
				return;

			currentState = newState;
			OnStateChanged ();
		}

		public event EventHandler<bool> StateChanged;

		public bool State {
			get { return currentState; }
		}
	}
}

