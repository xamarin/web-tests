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
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace Xamarin.AsyncTests.Framework.Reflection
{
	using Portable;

	class ReflectionTestFramework : TestFramework
	{
		public Assembly RootAssembly {
			get;
			private set;
		}

		public List<ReflectionTestAssembly> Assemblies {
			get { return assemblies; }
		}

		public override string Name {
			get { return name; }
		}

		public override ITestConfigurationProvider ConfigurationProvider {
			get { return providers; }
		}

		string name;
		ReflectionConfigurationProviderCollection providers;
		List<Assembly> dependencyAssemblies;
		List<ReflectionTestAssembly> assemblies;

		public ReflectionTestFramework (Assembly assembly, params Assembly[] dependencies)
		{
			RootAssembly = assembly;
			assemblies = new List<ReflectionTestAssembly> ();

			dependencyAssemblies = new List<Assembly> ();
			if (dependencies != null)
				dependencyAssemblies.AddRange (dependencies);

			name = RootAssembly.GetName ().Name;
			providers = new ReflectionConfigurationProviderCollection (name);

			Resolve ();

			providers.Resolve ();

			CheckDependencies ();
		}

		void RegisterDependency (Assembly assembly)
		{
			if (dependencyAssemblies.Contains (assembly))
				return;
			dependencyAssemblies.Add (assembly);
			DependencyInjector.RegisterAssembly (assembly);
		}

		void Resolve ()
		{
			RegisterDependency (RootAssembly);
			var cattrs = RootAssembly.GetCustomAttributes<AsyncTestSuiteAttribute> ().ToList ();
			if (cattrs.Count == 0)
				throw new InternalErrorException ("Assembly '{0}' is not a Xamarin.AsyncTests test suite.", RootAssembly);

			foreach (var cattr in cattrs) {
				Assembly assembly;
				AsyncTestSuiteAttribute attribute;

				string thisName = cattr.Name;

				if (cattr.IsReference) {
					assembly = cattr.Type.GetTypeInfo ().Assembly;
					RegisterDependency (assembly);
					var refcattrs = assembly.GetCustomAttributes<AsyncTestSuiteAttribute> ().ToList ();
					if (refcattrs.Count == 0)
						throw new InternalErrorException ("Referenced assembly '{0}' (referenced by '{1}') is not a Xamarin.AsyncTests test suite.", assembly, RootAssembly);
					else if (refcattrs.Count > 1)
						throw new InternalErrorException ("Referenced assembly '{0}' (referenced by '{1}') contains multiple '[AsyncTestSuite]' attributes.", assembly, RootAssembly);

					attribute = refcattrs [0];

					if (attribute.IsReference)
						throw new InternalErrorException ("Assembly '{0}' references '{1}', which is a reference itself.", RootAssembly, assembly);
				} else {
					attribute = cattr;
					assembly = RootAssembly;
				}

				if (thisName == null)
					thisName = attribute.Name ?? assembly.GetName ().Name;

				assemblies.Add (new ReflectionTestAssembly (thisName, attribute, assembly));
				providers.Add (attribute.Type);

				if (attribute.Dependencies != null) {
					foreach (var dependency in attribute.Dependencies) {
						RegisterDependency (dependency.GetTypeInfo ().Assembly);
						providers.Add (dependency);
					}
				}
			}
		}

		void CheckDependencies ()
		{
			foreach (var asm in dependencyAssemblies) {
				var cattrs = asm.GetCustomAttributes<RequireDependencyAttribute> ();
				foreach (var cattr in cattrs) {
					if (!DependencyInjector.IsAvailable (cattr.Type))
						throw new InternalErrorException ("Missing '{0}' dependency.", cattr.Type);
				}
			}
		}
	}
}

