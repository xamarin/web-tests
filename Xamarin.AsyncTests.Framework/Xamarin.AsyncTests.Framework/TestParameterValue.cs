//
// TestParameterValue.cs
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

namespace Xamarin.AsyncTests.Framework
{
	abstract class TestParameterValue
	{
		public TestInstance Instance {
			get;
			private set;
		}

		public TestPathInternal Path {
			get;
			private set;
		}

		public abstract ITestParameter Parameter {
			get;
		}

		public TestParameterValue (TestInstance instance)
		{
			Instance = instance;
			Path = instance.Path;
		}

		TestPathInternal currentPath;
		TestPathInternal parentPath;

		public TestPathInternal GetCurrentPath ()
		{
			if (currentPath != null)
				return currentPath;

			if (Instance.Parent != null) {
				parentPath = Instance.Parent.GetCurrentPath ();

				if (false) {
					// parentPath = (TestPathInternal)Path.Parent;

					var working = parentPath.SerializePath (false).ToString ();
					var broken = Path.Parent.SerializePath (false).ToString ();
					if (!string.Equals (working, broken))
						Debug ("GET CURRENT PATH: {0}\n{1}\n{2}\n\n", Instance, working, broken);
				}
			}

			currentPath = new TestPathInternal (Instance.Host, parentPath, Parameter);
			return currentPath;
		}

		static void Debug (string message, params object[] args)
		{
			System.Diagnostics.Debug.WriteLine ("TEST PARAMETER VALUE: {0}", string.Format (message, args));
		}
	}
}
