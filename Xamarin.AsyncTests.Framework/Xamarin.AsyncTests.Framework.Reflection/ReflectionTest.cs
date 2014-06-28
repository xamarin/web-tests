//
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
					return null;
				} else if (memberType.IsEnum) {
					useFixtureInstance = false;
					return null;
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
				yield return ParameterizedTestHost.CreateBoolean (member.Name);
				yield break;
			}

			if (member.Type.IsEnum) {
				yield return ParameterizedTestHost.CreateEnum (member.Type, member.Name);
				yield break;
			}

			throw new InvalidOperationException ();
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

