//
// ServerLogger.cs
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

namespace Xamarin.AsyncTests.Server
{
	class ServerLogger : TestLogger
	{
		public Connection Connection {
			get;
			private set;
		}

		public TestLogger Parent {
			get;
			private set;
		}

		public ServerLogger (Connection connection, TestLogger parent)
		{
			Connection = connection;
			Parent = parent;
		}

		protected internal override void OnLogEvent (LogEntry entry)
		{
			if (Parent != null)
				Parent.OnLogEvent (entry);

			switch (entry.Kind) {
			case LogEntry.EntryKind.Debug:
				if (entry.LogLevel <= Connection.Context.DebugLevel)
					SendMessage (entry.Text);
				break;

			case LogEntry.EntryKind.Error:
				if (entry.Error != null)
					SendMessage (string.Format ("ERROR: {0}", entry.Error));
				else
					SendMessage (entry.Text);
				break;

			default:
				SendMessage (entry.Text);
				break;
			}
		}

		async void SendMessage (string message)
		{
			await Connection.LogMessage (message);
		}
	}
}

