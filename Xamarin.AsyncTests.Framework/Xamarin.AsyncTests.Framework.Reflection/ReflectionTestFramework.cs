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

		public Assembly[] Dependencies {
			get;
			private set;
		}

		public override string Name {
			get { return name; }
		}

		public override ITestConfigurationProvider ConfigurationProvider {
			get { return provider; }
		}

		string name;
		ITestConfigurationProvider provider;

		public ReflectionTestFramework (Assembly assembly, params Assembly[] dependencies)
		{
			Assembly = assembly;
			Dependencies = dependencies;

			ResolveDependencies ();
			Resolve ();
		}

		void ResolveDependencies ()
		{
			foreach (var asm in Dependencies) {
				var cattr = asm.GetCustomAttribute<DependencyProviderAttribute> ();
				if (cattr == null)
					continue;
				var type = cattr.Type;
				var instanceMember = type.GetRuntimeField ("Instance");
				var provider = (IDependencyProvider)instanceMember.GetValue (null);
				provider.Initialize ();
			}
		}

		void Resolve ()
		{
			var cattr = Assembly.GetCustomAttribute<AsyncTestSuiteAttribute> ();
			if (cattr == null)
				throw new InternalErrorException ("Assembly '{0}' is not a Xamarin.AsyncTests test suite.", Assembly);
			var type = cattr.Type;
			var instanceMember = type.GetRuntimeField ("Instance");
			provider = (ITestConfigurationProvider)instanceMember.GetValue (null);
			name = Assembly.GetName ().Name;
		}
	}
}

