//
// ReflectionTestFixtureBuilder.cs
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
using System.Xml.Linq;
using System.Reflection;
using System.Collections.Generic;

namespace Xamarin.AsyncTests.Framework.Reflection
{
	class ReflectionTestFixtureBuilder : ReflectionTestBuilder
	{
		public ReflectionTestAssemblyBuilder AssemblyBuilder {
			get;
		}

		public TypeInfo Type {
			get;
		}

		public HeavyTestHost FixtureHost {
			get;
		}

		public AsyncTestFixtureAttribute Attribute {
			get;
		}

		TestFilter filter;

		public ReflectionTestFixtureBuilder (ReflectionTestAssemblyBuilder assembly, AsyncTestFixtureAttribute attr, TypeInfo type)
			: base (assembly, TestPathType.Fixture, null,
			        attr.Prefix != null ? attr.Prefix + "." + type.Name : type.Name,
			        TestSerializer.GetStringParameter (type.FullName), TestFlags.Browsable)
		{
			AssemblyBuilder = assembly;
			Type = type;
			Attribute = attr;

			filter = ReflectionHelper.CreateTestFilter (this, null, false, ReflectionHelper.GetTypeInfo (type));
		}

		public override TestFilter Filter {
			get { return filter; }
		}

		bool resolvedMembers;
		List<ReflectionMethodEntry> instanceMethods;
		List<ReflectionMethodEntry> staticMethods;

		public IReadOnlyList<ReflectionMethodEntry> InstanceMethods {
			get {
				if (!resolvedMembers)
					throw new InternalErrorException ();
				return instanceMethods;
			}
		}

		protected override void ResolveMembers ()
		{
			base.ResolveMembers ();

			instanceMethods = new List<ReflectionMethodEntry> ();
			staticMethods = new List<ReflectionMethodEntry> ();

			ResolveMembers (Type);

			ResolveFixedParameters ();

			ResolveFixtureProperties (false);

			if (instanceMethods.Count > 0 || (Type.IsAbstract && Type.IsSealed))
				ResolveForkedFixture ();

			if (instanceMethods.Count > 0)
				ResolveConstructor (true);

			AddMethods (staticMethods);

			resolvedMembers = true;

			void ResolveMembers (TypeInfo type)
			{
				foreach (var method in type.DeclaredMethods) {
					ResolveMethod (method);
				}

				var baseType = type.BaseType?.GetTypeInfo ();
				if (baseType != null)
					ResolveMembers (baseType);
			}

			void ResolveMethod (MethodInfo method)
			{
				if (!method.IsPublic || method.IsAbstract)
					return;
				if (Type.IsAbstract && !method.IsStatic)
					return;
				var attr = method.GetCustomAttribute<AsyncTestAttribute> (true);
				if (attr == null)
					return;

				if (method.IsStatic) {
					if (instanceMethods.Count > 0)
						CannotMixStaticAndInstance ();
					staticMethods.Add (new ReflectionMethodEntry (method, attr));
				} else {
					if (staticMethods.Count > 0)
						CannotMixStaticAndInstance ();
					instanceMethods.Add (new ReflectionMethodEntry (method, attr));
				}
			}

			void CannotMixStaticAndInstance ()
			{
				throw new InternalErrorException (
					$"Cannot mix static and instance methods in fixture `{Type}'.");
			}
		}
	}
}

