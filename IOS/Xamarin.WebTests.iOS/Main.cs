//
// Main.cs
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
using System.Linq;
using Xamarin.AsyncTests;
using Xamarin.AsyncTests.Remoting;
using Xamarin.AsyncTests.Portable;
using Xamarin.WebTests.TestProvider;
using Xamarin.WebTests.MonoTestProvider;
using Xamarin.WebTests.MonoTestFramework;
using Xamarin.WebTests.ConnectionFramework;
using Xamarin.WebTests.MonoConnectionFramework;

using Foundation;
using UIKit;

[assembly: AsyncTestSuite (typeof (Xamarin.WebTests.WebTestFeatures), true)]
[assembly: AsyncTestSuite (typeof (Xamarin.WebTests.MonoTests.MonoWebTestFeatures), true)]

namespace Xamarin.WebTests.iOS
{
	[Serializable]
	public class Application
	{
		public bool SupportsProcessForks => false;

		// This is the main entry point of the application.
		static void Main (string[] args)
		{
			var setup = new MonoConnectionFrameworkSetup ("Xamarin.WebTests.iOS");
			DependencyInjector.RegisterDependency<IConnectionFrameworkSetup> (() => setup);
			DependencyInjector.RegisterDependency<IMonoConnectionFrameworkSetup> (() => setup);

			DependencyInjector.RegisterAssembly (typeof (WebDependencyProvider).Assembly);
			DependencyInjector.RegisterAssembly (typeof (AppDelegate).Assembly);

			// if you want to use a different Application Delegate class from "AppDelegate"
			// you can specify it here.
			UIApplication.Main (args, null, "AppDelegate");
		}
	}
}
