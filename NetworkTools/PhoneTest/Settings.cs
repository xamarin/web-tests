//
// Settings.cs
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

namespace Xamarin.NetworkUtils.PhoneTest
{
	public class Settings
	{
		bool showListening;
		bool showLocal = true;
		bool autoRefresh = true;
		bool usePortFilter;
		int portFilter;
		Uri uri = new Uri ("http://localhost:9615/");
		ServicePoint servicePoint;

		public static readonly Settings Instance = new Settings ();

		public bool ShowListening {
			get { return showListening; }
			set {
				if (value == showListening)
					return;
				showListening = value;
				OnModified ();
			}
		}

		public bool ShowLocal {
			get { return showLocal; }
			set {
				if (value == showLocal)
					return;
				showLocal = value;
				OnModified ();
			}
		}

		public bool AutoRefresh {
			get { return autoRefresh; }
			set {
				if (value == autoRefresh)
					return;
				autoRefresh = value;
				OnModified ();
			}
		}

		public int PortFilter {
			get { return portFilter; }
			set {
				if (value == portFilter)
					return;
				portFilter = value;
				OnModified ();
			}
		}

		public bool UsePortFilter {
			get { return usePortFilter; }
			set {
				if (value == usePortFilter)
					return;
				usePortFilter = value;
				OnModified ();
			}
		}

		public int ConnectionLimit {
			get {
				if (servicePoint != null)
					return servicePoint.ConnectionLimit;
				else
					return ServicePointManager.DefaultConnectionLimit;
			}
			set {
				GetServicePoint ();
				if (value == servicePoint.ConnectionLimit)
					return;
				servicePoint.ConnectionLimit = value;
				OnModified ();
			}
		}

		public int ServicePointIdleTime {
			get {
				if (servicePoint != null)
					return servicePoint.MaxIdleTime;
				else
					return ServicePointManager.MaxServicePointIdleTime;
			}
			set {
				GetServicePoint ();
				if (value == servicePoint.MaxIdleTime)
					return;
				servicePoint.MaxIdleTime = value;
				OnModified ();
			}
		}

		void GetServicePoint ()
		{
			if (servicePoint != null)
				return;
			servicePoint = ServicePointManager.FindServicePoint (uri);
		}

		public Uri Uri {
			get { return uri; }
			set {
				uri = value;
				servicePoint = null;
				OnModified ();
			}
		}

		protected virtual void OnModified ()
		{
			if (Modified != null)
				Modified (this, null);
		}

		public event EventHandler Modified;
	}
}

