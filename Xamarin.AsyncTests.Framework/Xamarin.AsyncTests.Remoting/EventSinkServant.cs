//
// EventSinkServant.cs
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
using System.Threading;
using SD = System.Diagnostics;

namespace Xamarin.AsyncTests.Remoting
{
	class EventSinkServant : ObjectServant, RemoteEventSink
	{
		public TestLoggerBackend Logger {
			get;
			private set;
		}

		public override string Type {
			get { return "EventSink"; }
		}

		public EventSinkServant (ClientConnection connection, TestLoggerBackend logger)
			: base (connection)
		{
			Logger = logger;
		}

		public void LogEvent (TestLoggerBackend.LogEntry entry)
		{
			SD.Debug.WriteLine ("ON LOG EVENT: {0}", entry);
			Logger.OnLogEvent (entry);
		}

		public void StatisticsEvent (TestLoggerBackend.StatisticsEventArgs args)
		{
			SD.Debug.WriteLine ("ON STATISTICS EVENT: {0}", args);
			Logger.OnStatisticsEvent (args);
		}

		EventSinkClient RemoteObject<EventSinkClient,EventSinkServant>.Client {
			get { throw new ServerErrorException (); }
		}

		EventSinkServant RemoteObject<EventSinkClient,EventSinkServant>.Servant {
			get { return this; }
		}
	}
}

