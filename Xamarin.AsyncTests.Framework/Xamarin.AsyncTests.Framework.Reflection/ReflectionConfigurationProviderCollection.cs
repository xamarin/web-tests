using System;
using System.Linq;
using System.Collections.Generic;

namespace Xamarin.AsyncTests.Framework.Reflection
{
	class ReflectionConfigurationProviderCollection : ITestConfigurationProvider
	{
		Dictionary<Type,ReflectionConfigurationProvider> providers;
		List<TestFeature> features;
		List<TestCategory> categories;
		bool resolved;

		public ReflectionConfigurationProviderCollection (string name)
		{
			Name = name;
			providers = new Dictionary<Type,ReflectionConfigurationProvider> ();
		}

		public string Name {
			get;
			private set;
		}

		public void Add (Type type)
		{
			if (resolved)
				throw new InvalidOperationException ();
			if (providers.ContainsKey (type))
				return;
			providers.Add (type, null);
		}

		public void Resolve ()
		{
			if (resolved)
				return;

			var types = providers.Keys.ToArray ();

			foreach (var type in types) {
				var provider = (ITestConfigurationProvider)DependencyInjector.Get (type);
				var providerWrapper = new ReflectionConfigurationProvider (provider);
				providers [type] = providerWrapper;
			}

			features = new List<TestFeature> ();
			categories = new List<TestCategory> ();

			foreach (var provider in providers.Values) {
				features.AddRange (provider.Features);
				categories.AddRange (provider.Categories);
			}

			resolved = true;
		}

		public IEnumerable<TestFeature> Features {
			get {
				if (!resolved)
					throw new InvalidOperationException ();
				return features;
			}
		}

		public IEnumerable<TestCategory> Categories {
			get {
				if (!resolved)
					throw new InvalidOperationException ();
				return categories;
			}
		}
	}
}

