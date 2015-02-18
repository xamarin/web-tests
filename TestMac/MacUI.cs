//
// MacUI.cs
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
using System.Reflection;
using Xamarin.AsyncTests;
using Xamarin.AsyncTests.Framework;
using Xamarin.AsyncTests.UI;
using Xamarin.AsyncTests.Portable;
using Xamarin.AsyncTests.Sample;

namespace TestMac
{
	public class MacUI : UITestApp
	{
		MacUI (IPortableSupport support, ITestConfigurationProvider configProvider,
			SettingsBag settings, Assembly assembly)
			: base (support, configProvider, settings, assembly)
		{
		}

		public static MacUI Create ()
		{
			// var support = PortableSupportImpl.Initialize ();
			// var provider = WebTestFeatures.Instance;
			var settings = SettingsBag.CreateDefault ();
			var assembly = typeof(SampleFeatures).Assembly;
			return new MacUI (null, SampleFeatures.Instance, settings, assembly);
		}

	}
}

