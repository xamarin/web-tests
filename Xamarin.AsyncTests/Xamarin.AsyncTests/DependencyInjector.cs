//
// DependencyInjector.cs
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
using System.Threading;
using System.Reflection;
using System.Collections.Generic;

namespace Xamarin.AsyncTests
{
	public static class DependencyInjector
	{
		static Dictionary<Type,SingletonInstance> dict = new Dictionary<Type,SingletonInstance> ();
		static Dictionary<string,object> assemblies = new Dictionary<string,object> ();
		static Dictionary<Type,object> extensionProviders = new Dictionary<Type,object> ();
		static Dictionary<object,object> extensionObjects = new Dictionary<object,object> ();
		static Dictionary<Type,object> collections = new Dictionary<Type,object> ();
		static Dictionary<Type,object> defaults = new Dictionary<Type,object> ();
		static object syncRoot = new object ();

		public static void RegisterDependency<T> (Func<T> constructor)
			where T : class, ISingletonInstance
		{
			lock (syncRoot) {
				if (dict.ContainsKey (typeof(T)))
					return;
				var instance = new SingletonInstance (constructor);
				dict.Add (typeof(T), instance);
			}
		}

		public static void RegisterAssembly (Assembly assembly)
		{
			lock (syncRoot) {
				var aname = assembly.FullName;
				if (assemblies.ContainsKey (aname))
					return;
				assemblies.Add (aname, assembly);
				foreach (var cattr in assembly.GetCustomAttributes<DependencyProviderAttribute> ()) {
					var provider = (IDependencyProvider)Activator.CreateInstance (cattr.Type);
					provider.Initialize ();
				}
			}
		}

		public static T Get<T> ()
			where T : class, ISingletonInstance
		{
			lock (syncRoot) {
				T dependency;
				if (!TryGet (out dependency))
					throw new InvalidOperationException (string.Format ("Missing dependency: `{0}'", typeof(T)));
				return dependency;
			}
		}

		internal static ISingletonInstance Get (Type type)
		{
			lock (syncRoot) {
				ISingletonInstance instance;
				if (!TryGet (type, out instance))
					throw new InvalidOperationException (string.Format ("Missing dependency: `{0}'", type));
				return instance;
			}
		}

		static bool TryGet (Type type, out ISingletonInstance dependency)
		{
			lock (syncRoot) {
				SingletonInstance instance;
				if (!dict.TryGetValue (type, out instance)) {
					dependency = null;
					return false;
				}
				dependency = (ISingletonInstance)instance.Instance;
				return true;
			}
		}

		public static bool TryGet<T> (out T dependency)
			where T : class, ISingletonInstance
		{
			lock (syncRoot) {
				SingletonInstance instance;
				if (!dict.TryGetValue (typeof(T), out instance)) {
					dependency = null;
					return false;
				}
				dependency = (T)instance.Instance;
				return true;
			}
		}

		public static bool IsAvailable (Type type)
		{
			lock (syncRoot) {
				return dict.ContainsKey (type);
			}
		}

		public static void RegisterCollection<T> (T item)
		{
			lock (syncRoot) {
				List<T> list;
				object value;
				if (collections.TryGetValue (typeof(T), out value))
					list = (List<T>)value;
				else {
					list = new List<T> ();
					collections.Add (typeof(T), list);
				}
				list.Add (item);
			}
		}

		public static ICollection<T> GetCollection<T> ()
		{
			lock (syncRoot) {
				object value;
				if (collections.TryGetValue (typeof(T), out value))
					return (List<T>)value;
				return new T [0];
			}
		}

		public static void RegisterExtension<T> (IExtensionProvider<T> provider)
			where T : class
		{
			lock (syncRoot) {
				if (extensionProviders.ContainsKey (typeof(T)))
					throw new InvalidOperationException ();
				extensionProviders.Add (typeof(T), provider);
			}
		}

		public static void RegisterExtension<T,E> (T instance, E extension)
			where T : class
			where E : class, IExtensionObject<T>
		{
			lock (syncRoot) {
				if (extensionObjects.ContainsKey (instance))
					throw new InvalidOperationException ();
				extensionObjects.Add (instance, extension);
			}
		}

		public static void RegisterExtension<T> (Func<T,IExtensionObject<T>> provider)
			where T : class
		{
			lock (syncRoot) {
				if (extensionProviders.ContainsKey (typeof(T)))
					throw new InvalidOperationException ();
				extensionProviders.Add (typeof (T), new ExtensionProvider<T> (provider));
			}
		}

		public static bool TryGetExtension<T,E> (T instance, out E extension)
			where T : class
			where E : class, IExtensionObject<T>
		{
			lock (syncRoot) {
				object value;
				if (extensionObjects.TryGetValue (instance, out value)) {
					extension = (E)value;
					return true;
				}
				if (!extensionProviders.TryGetValue (typeof(T), out value)) {
					extension = null;
					return false;
				}
				var provider = (IExtensionProvider<T>)value;
				extension = (E)provider.GetExtensionObject (instance);
				return true;
			}
		}

		public static E GetExtension<T,E> (T instance)
			where T : class
			where E : class, IExtensionObject<T>
		{
			E extension;
			if (!TryGetExtension (instance, out extension))
				throw new InvalidOperationException ();
			return extension;
		}

		class SingletonInstance
		{
			object instance;
			Func<object> constructor;

			public object Instance {
				get {
					if (instance == null)
						Interlocked.CompareExchange (ref instance, constructor (), null);
					return instance;
				}
			}

			public SingletonInstance (Func<object> ctor)
			{
				constructor = ctor;
			}
		}

		class ExtensionProvider<T> : IExtensionProvider<T>
			where T : class
		{
			readonly Func<T,IExtensionObject<T>> provider;

			public ExtensionProvider (Func<T,IExtensionObject<T>> provider)
			{
				this.provider = provider;
			}

			public IExtensionObject<T> GetExtensionObject (T instance)
			{
				return provider (instance);
			}
		}

		class DefaultEntry<T>
			where T : class, ITestDefaults
		{
			public int Priority {
				get;
				private set;
			}

			T instance;
			Func<T> constructor;

			public T Instance {
				get {
					if (instance == null)
						Interlocked.CompareExchange (ref instance, constructor (), null);
					return instance;
				}
			}

			public DefaultEntry (int priority, Func<T> ctor)
			{
				Priority = priority;
				constructor = ctor;
			}
		}

		public static bool RegisterDefaults<T> (int priority, Func<T> constructor)
			where T : class, ITestDefaults
		{
			lock (syncRoot) {
				object value;
				DefaultEntry<T> entry;
				if (!defaults.TryGetValue (typeof(T), out value)) {
					entry = new DefaultEntry<T> (priority, constructor);
					defaults.Add (typeof(T), entry);
					return true;
				}
				entry = (DefaultEntry<T>)value;
				if (priority <= entry.Priority)
					return false;
				entry = new DefaultEntry<T> (priority, constructor);
				defaults [typeof(T)] = entry;
				return true;
			}
		}

		public static T GetDefaults<T> (int minPriority = 0)
			where T : class, ITestDefaults
		{
			lock (syncRoot) {
				object value;
				DefaultEntry<T> entry;
				if (!defaults.TryGetValue (typeof(T), out value))
					return default (T);
				entry = (DefaultEntry<T>)value;
				if (entry.Priority < minPriority)
					return null;
				return entry.Instance;
			}
		}
	}
}

