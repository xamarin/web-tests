//
// ReflectionTestBuilder.cs
//
// Author:
//       Martin Baulig <mabaul@microsoft.com>
//
// Copyright (c) 2018 Xamarin Inc. (http://www.xamarin.com)
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
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace Xamarin.AsyncTests.Framework.Reflection
{
	abstract class ReflectionTestBuilder : TestBuilder
	{
		public sealed override TestBuilder Parent {
			get;
		}

		public ReflectionTestFixtureBuilder Fixture {
			get;
		}

		protected ReflectionTestBuilder (
			TestBuilder parent, TestPathType type, string identifier,
			string name, ITestParameter parameter,
			TestFlags flags = TestFlags.Hidden)
			: base (type, identifier, name, parameter, flags)
		{
			Parent = parent;
			Fixture = (ReflectionTestFixtureBuilder)GetFixtureBuilder (this);
		}

		public ReflectionTestBuilder Inner {
			get;
			private set;
		}

		void Add (ReflectionTestBuilder inner)
		{
			if (Inner != null)
				Inner.Add (inner);
			else
				Inner = inner;
			current = inner;
		}

		protected override IList<TestBuilder> CreateChildren ()
		{
			if (!SkipThisTest && Inner != null)
				return new TestBuilder[] { Inner };
			return null;
		}

		ReflectionTestBuilder current;
		bool membersResolved;
		bool skipThisTest;

		protected sealed override bool SkipThisTest {
			get {
				if (!membersResolved)
					throw new InternalErrorException ();
				return skipThisTest;
			}
		}

		protected override void ResolveMembers ()
		{
			base.ResolveMembers ();
			current = this;
			membersResolved = true;
		}

		protected void ResolveFixedParameters ()
		{
			var fixedParameters = Fixture.Type.GetCustomAttributes<FixedTestParameterAttribute> ();
			foreach (var fixedParameter in fixedParameters) {
				var parameter = ReflectionHelper.CreateFixedParameterHost (fixedParameter);
				Add (new ReflectionTestParameterBuilder (current, parameter));
			}

			if (Fixture.Attribute.Repeat != 0) {
				var parameter = ReflectionHelper.CreateRepeatHost (Fixture.Attribute.Repeat);
				Add (new ReflectionTestParameterBuilder (current, parameter));
			}
		}

		protected void ResolveChildren (
			MethodBase method, AsyncTestAttribute attribute, bool shared)
		{
			bool seenCtx = false;
			bool seenToken = false;
			bool seenFixtureInstance = !method.IsStatic;

			ReflectionTestBuilder unwindBase = current;

			var fixedParameters = method.GetCustomAttributes<FixedTestParameterAttribute> ();
			foreach (var fixedParameter in fixedParameters) {
				var parameter = ReflectionHelper.CreateFixedParameterHost (fixedParameter);
				Add (new ReflectionTestParameterBuilder (current, parameter));
			}

			var parameters = method.GetParameters ();
			for (int i = 0; i < parameters.Length; i++) {
				var paramInfo = ReflectionHelper.GetParameterInfo (parameters[i]);
				var paramType = paramInfo.Type;
				var paramTypeInfo = paramInfo.TypeInfo;
				var paramName = paramInfo.Name;

				var fork = parameters[i].GetCustomAttribute<ForkAttribute> ();
				if (fork != null) {
					if (!paramType.Equals (typeof (IFork)))
						throw new InternalErrorException (
							$"Cannot use `[Fork]' on something that is not `IFork' in `{DebugHelper.FormatMethod (method)}'.");
					var forkedHost = new ForkedTestHost (paramName, fork);
					Add (new ReflectionTestForkedBuilder (current, forkedHost));
					continue;
				}

				if (paramType.Equals (typeof (CancellationToken))) {
					if (seenToken)
						throw new InternalErrorException ();
					seenToken = true;
					continue;
				}

				if (paramType.Equals (typeof (TestContext))) {
					if (seenCtx)
						throw new InternalErrorException ();
					seenCtx = true;
					continue;
				}

				if (paramType.Equals (typeof (IFork)))
					throw new InternalErrorException ();

				if (paramTypeInfo.IsAssignableFrom (Fixture.Type)) {
					if (!method.IsStatic)
						throw new InternalErrorException ($"Cannot use fixture instance parameter in instance method `{DebugHelper.FormatMethod (method)}'.");
					if (seenFixtureInstance)
						throw new InternalErrorException ($"Cannot use more than one fixture instance parameter in method `{DebugHelper.FormatMethod (method)}'.");
					seenFixtureInstance = true;
					if (Fixture.Type.IsAbstract) {
						skipThisTest = true;
						continue;
					}

					ResolveConstructor (false);
					continue;
				}

				var parameter = ReflectionHelper.ResolveParameter (
					Fixture.Type, paramInfo, attribute.ParameterFilter);
				Add (new ReflectionTestParameterBuilder (current, parameter));
			}

			if (method is ConstructorInfo constructor) {
				Add (new ReflectionTestInstanceBuilder (current, unwindBase, constructor, shared));
				if (!shared)
					ResolveFixtureProperties ();
			} else if (this is ReflectionTestCaseBuilder caseBuilder) {
				Add (new ReflectionTestMethodBuilder (current, caseBuilder));
			} else {
				throw new InternalErrorException ();
			}
		}

		protected void ResolveConstructor (bool shared)
		{
			ConstructorInfo defaultCtor = null;
			ConstructorInfo customCtor = null;

			foreach (var ctor in Fixture.Type.DeclaredConstructors) {
				if (ctor.IsStatic || ctor.IsAbstract || !ctor.IsPublic)
					continue;
				var parameters = ctor.GetParameters ();
				if (parameters.Length == 0) {
					if (customCtor != null)
						CannotHaveBothDefaultAndCustom ();
					defaultCtor = ctor;
					ResolveChildren (ctor, null, shared);
					continue;
				}
				var attr = ctor.GetCustomAttribute<AsyncTestAttribute> ();
				if (attr == null)
					continue;
				if (defaultCtor != null)
					CannotHaveBothDefaultAndCustom ();
				if (customCtor != null)
					CannotHaveMultipleCustom ();
				customCtor = ctor;

				ResolveChildren (ctor, attr, shared);
			}

			if (defaultCtor == null && customCtor == null)
				throw new InternalErrorException ($"Missing default .ctor in type `{Fixture.Type}'.");

			void CannotHaveMultipleCustom ()
			{
				throw new InternalErrorException ($"Type `{Fixture.Type}' has more than one [AsyncTest] constructor.");
			}

			void CannotHaveBothDefaultAndCustom ()
			{
				throw new InternalErrorException ($"Cannot have both a default and a custom [AsyncTest] constructor in `{Fixture.Type}'.");
			}
		}

		protected void AddMethods (IReadOnlyList<ReflectionMethodEntry> methods)
		{
			if (methods.Count == 0)
				return;

			Add (new ReflectionTestCollectionBuilder (current, methods));
		}

		protected void ResolveFixtureProperties ()
		{
			foreach (var property in Fixture.Type.DeclaredProperties) {
				var host = ReflectionHelper.ResolveFixtureProperty (Fixture.Type, property);
				if (host == null)
					continue;
				Add (new ReflectionTestParameterBuilder (current, host));
			}
		}
	}
}
