﻿//
// ReflectionTest.cs
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
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Xamarin.AsyncTests.Framework.Reflection
{
	abstract class ReflectionTest : TestCase
	{
		readonly IEnumerable<string> categories;

		public AsyncTestAttribute Attribute {
			get;
			private set;
		}

		public override IEnumerable<string> Categories {
			get { return categories; }
		}

		internal abstract TestInvoker Invoker {
			get;
		}

		public ReflectionTest (TestName name, AsyncTestAttribute attr, IMemberInfo member)
			: base (name)
		{
			Attribute = attr;

			categories = GetCategories (member);
		}

		static TypeInfo GetHostType (
			TypeInfo fixtureType, Type hostType, TypeInfo memberType, Type attrType,
			bool allowImplicit, out bool useFixtureInstance)
		{
			var genericInstance = hostType.GetTypeInfo ().MakeGenericType (memberType.AsType ()).GetTypeInfo ();

			if (attrType != null) {
				var attrInfo = attrType.GetTypeInfo ();
				if (!genericInstance.IsAssignableFrom (attrInfo))
					throw new InvalidOperationException ();
				useFixtureInstance = false;
				return attrInfo;
			}

			if (genericInstance.IsAssignableFrom (fixtureType)) {
				useFixtureInstance = true;
				return null;
			}

			if (allowImplicit) {
				if (memberType.AsType ().Equals (typeof(bool))) {
					useFixtureInstance = false;
					return typeof(BooleanTestSource).GetTypeInfo ();
				} else if (memberType.IsEnum) {
					useFixtureInstance = false;
					return typeof(EnumTestSource<>).MakeGenericType (memberType.AsType ()).GetTypeInfo ();
				}
			}

			throw new InvalidOperationException ();
		}

		protected static IEnumerable<ParameterizedTestHost> ResolveParameter (TypeInfo fixtureType, IMemberInfo member)
		{
			if (typeof(ITestInstance).GetTypeInfo ().IsAssignableFrom (member.Type)) {
				var hostAttr = member.GetCustomAttribute<TestHostAttribute> ();
				if (hostAttr == null)
					hostAttr = member.Type.GetCustomAttribute<TestHostAttribute> ();
				if (hostAttr == null)
					throw new InvalidOperationException ();

				bool useFixtureInstance;
				var hostType = GetHostType (
					fixtureType, typeof(ITestHost<>), member.Type,
					hostAttr.HostType, false, out useFixtureInstance);

				yield return new CustomHostAttributeTestHost (
					member.Name, member.Type, hostType, useFixtureInstance, hostAttr);
				yield break;
			}

			bool found = false;
			var paramAttrs = member.GetCustomAttributes<TestParameterSourceAttribute> ();
			foreach (var paramAttr in paramAttrs) {
				bool useFixtureInstance = false;
				var sourceType = GetHostType (
					fixtureType, typeof(ITestParameterSource<>), member.Type,
					paramAttr.SourceType, true, out useFixtureInstance);

				yield return new ParameterAttributeTestHost (
					member.Name, member.Type, sourceType, useFixtureInstance, paramAttr);
				found = true;
			}

			if (found)
				yield break;

			paramAttrs = member.Type.GetCustomAttributes<TestParameterSourceAttribute> ();
			foreach (var paramAttr in paramAttrs) {
				bool useFixtureInstance = false;
				var sourceType = GetHostType (
					fixtureType, typeof(ITestParameterSource<>), member.Type,
					paramAttr.SourceType, true, out useFixtureInstance);

				yield return new ParameterAttributeTestHost (
					member.Name, member.Type, sourceType, useFixtureInstance, paramAttr);
				found = true;
			}

			if (found)
				yield break;

			if (member.Type.AsType ().Equals (typeof(bool))) {
				yield return new ParameterSourceHost<bool> (member.Name, new BooleanTestSource (), null);
				yield break;
			}

			if (member.Type.IsEnum) {
				yield return CreateEnum (member.Type, member.Name);
				yield break;
			}

			throw new InvalidOperationException ();
		}

		static ParameterizedTestHost CreateEnum (TypeInfo typeInfo, string name)
		{
			if (!typeInfo.IsEnum || typeInfo.GetCustomAttribute<FlagsAttribute> () != null)
				throw new InvalidOperationException ();

			var type = typeInfo.AsType ();
			var sourceType = typeof(EnumTestSource<>).MakeGenericType (type);
			var hostType = typeof(ParameterSourceHost<>).MakeGenericType (type);

			var source = Activator.CreateInstance (sourceType);
			return (ParameterizedTestHost)Activator.CreateInstance (hostType, name, source, null, TestFlags.None);
		}

		class BooleanTestSource : ITestParameterSource<bool>
		{
			public IEnumerable<bool> GetParameters (TestContext context, string filter)
			{
				yield return false;
				yield return true;
			}
		}

		class EnumTestSource<T> : ITestParameterSource<T>
		{
			public IEnumerable<T> GetParameters (TestContext context, string filter)
			{
				foreach (var value in Enum.GetValues (typeof (T)))
					yield return (T)value;
			}
		}

		class ParameterSourceHost<T> : ParameterizedTestHost
		{
			public string Name {
				get;
				private set;
			}

			public ITestParameterSource<T> Source {
				get;
				private set;
			}

			public string Filter {
				get;
				private set;
			}

			public ParameterSourceHost (string name, ITestParameterSource<T> source, string filter, TestFlags flags = TestFlags.None)
				: base (name, typeof (T).GetTypeInfo (), flags)
			{
				Source = source;
				Filter = filter;
			}

			internal override TestInstance CreateInstance (TestContext context, TestInstance parent)
			{
				return ParameterSourceInstance<T>.CreateFromSource (this, parent, Source, Filter);
			}
		}

		protected static IEnumerable<string> GetCategories (IMemberInfo member)
		{
			foreach (var cattr in member.GetCustomAttributes<TestCategoryAttribute> ())
				yield return cattr.Name;
		}

		public override Task<bool> Run (TestContext ctx, TestResult result, CancellationToken cancellationToken)
		{
			return Invoker.Invoke (ctx, null, result, cancellationToken);
		}
	}
}
