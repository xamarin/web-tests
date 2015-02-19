//
// TestApp.cs
//
// Authors:
//      Martin Baulig (martin.baulig@gmail.com)
//
// Copyright 2012 Xamarin Inc. (http://www.xamarin.com)
//
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
// 
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//
using System;
using System.Text;
using System.Linq;
using SD = System.Diagnostics;
using System.Reflection;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using System.ComponentModel;

namespace Xamarin.AsyncTests.Framework
{
	using Portable;

	public abstract class TestApp : INotifyPropertyChanged
	{
		readonly IPortableSupport support;
		volatile TestSuite currentTestSuite;

		public abstract TestLogger Logger {
			get;
		}

		public abstract SettingsBag Settings {
			get;
		}

		public TestSuite CurrentTestSuite {
			get { return currentTestSuite; }
			set {
				if (currentTestSuite == value)
					return;
				currentTestSuite = value;
				OnPropertyChanged ("CurrentTestSuite");
			}
		}

		public IPortableSupport PortableSupport {
			get { return support; }
		}

		public TestApp (IPortableSupport support)
		{
			this.support = support;
		}

		protected virtual void OnPropertyChanged (string propertyName)
		{
			if (PropertyChanged != null)
				PropertyChanged (this, new PropertyChangedEventArgs (propertyName));
		}

		public event PropertyChangedEventHandler PropertyChanged;

		public abstract TestFramework GetLocalTestFramework ();
	}
}
