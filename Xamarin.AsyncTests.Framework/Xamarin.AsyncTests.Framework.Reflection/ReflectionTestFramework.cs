//
// ReflectionTestFramework.cs
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
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace Xamarin.AsyncTests.Framework.Reflection
{
	using Portable;

	class ReflectionTestFramework : TestFramework
	{
		public Assembly Assembly {
			get;
			private set;
		}

		public override TestName Name {
			get { return name; }
		}

		public override TestConfiguration Configuration {
			get { return configuration; }
		}

		TestName name;
		TestConfiguration configuration;
		SettingsBag settings;

		public ReflectionTestFramework (Assembly assembly, SettingsBag settings)
		{
			Assembly = assembly;
			this.settings = settings;

			Resolve ();
		}

		void Resolve ()
		{
			var cattr = Assembly.GetCustomAttributes<AsyncTestSuiteAttribute> ().First ();
			var type = cattr.Type;
			var instanceMember = type.GetRuntimeField ("Instance");
			var provider = (ITestConfigurationProvider)instanceMember.GetValue (null);

			configuration = new TestConfiguration (provider, settings);
			name = new TestName (Assembly.GetName ().Name);
		}
	}
}

