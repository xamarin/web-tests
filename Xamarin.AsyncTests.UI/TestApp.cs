//
// App.cs
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
using System.Reflection;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Xamarin.Forms;

namespace Xamarin.AsyncTests.UI
{
	using Framework;

	public class TestApp
	{
		public TestSuite TestSuite {
			get;
			private set;
		}

		public TestContext Context {
			get;
			private set;
		}

		public MainPage MainPage {
			get;
			private set;
		}

		public Page Root {
			get;
			private set;
		}

		public TestApp (string name)
		{
			TestSuite = new TestSuite (name);
			Context = new TestContext ();

			MainPage = new MainPage (this);
			Root = new NavigationPage (MainPage);
		}

		public async void LoadAssembly (Assembly assembly)
		{
			var name = assembly.GetName ().Name;
			MainPage.Message ("Loading {0}", name);
			var test = await TestSuite.LoadAssembly (assembly);
			MainPage.Message ("Successfully loaded {0} tests from {1}.", test.Count, name);
			OnAssemblyLoaded (assembly);
		}

		protected void OnAssemblyLoaded (Assembly assembly)
		{
			if (AssemblyLoadedEvent != null)
				AssemblyLoadedEvent (this, assembly);
		}

		public event EventHandler<Assembly> AssemblyLoadedEvent;

		public Task<TestResultCollection> Run (CancellationToken cancellationToken)
		{
			return TestSuite.Run (Context, cancellationToken);
		}
	}
}
