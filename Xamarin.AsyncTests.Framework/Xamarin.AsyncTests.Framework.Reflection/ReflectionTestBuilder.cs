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

		void WalkStackForForked ()
		{
			var forkedInstanceType = typeof (IForkedTestInstance).GetTypeInfo ();
			for (TestBuilder builder = current; builder != null; builder = builder.Parent) {
				switch (builder.PathType) {
				case TestPathType.Suite:
				case TestPathType.Assembly:
				case TestPathType.Fixture:
				case TestPathType.Test:
				case TestPathType.Collection:
					continue;
				case TestPathType.Parameter:
				case TestPathType.Argument:
					CheckParameterBuilder ();
					continue;
				case TestPathType.Instance:
					CheckInstanceBuilder ();
					continue;
				default:
					throw ThrowInvalidBuilder ();
				}

				void CheckInstanceBuilder ()
				{
					if (!(builder is ReflectionTestInstanceBuilder instanceBuilder))
						throw ThrowInvalidBuilder ();
					if (forkedInstanceType.IsAssignableFrom (instanceBuilder.FixtureType))
						return;
					throw new InternalErrorException ($"Fixture type '{DebugHelper.FormatType (instanceBuilder.FixtureType)}' must implement IForkedTestInstance.");
				}

				void CheckParameterBuilder ()
				{
					if (!(builder is ReflectionTestParameterBuilder parameterBuilder))
						throw ThrowInvalidBuilder ();
					if (parameterBuilder.ParameterHost is ParameterizedTestHost)
						return;
					if (parameterBuilder.ParameterHost is HeavyTestHost heavy) {
						if (forkedInstanceType.IsAssignableFrom (heavy.Type.GetTypeInfo ()))
							return;
						throw new InternalErrorException ($"Test host '{DebugHelper.FormatType (heavy.Type)}' must implement IForkedTestInstance.");
					}
				}

				Exception ThrowInvalidBuilder ()
				{
					throw new InternalErrorException ($"Invalid builder '{builder}' on stack for [Forked].");
				}
			}
		}

		void ResolveForked (string name, ForkAttribute attribute, bool hasParameterHost)
		{
			if (attribute.Type != ForkType.Task)
				WalkStackForForked ();
			var forkedHost = new ForkedTestHost (name, attribute, hasParameterHost);
			Add (new ReflectionTestForkedBuilder (current, forkedHost));
		}

		protected void ResolveForkedFixture ()
		{
			var fork = Fixture.Type.GetCustomAttribute<ForkAttribute> ();
			if (fork != null)
				ResolveForked (Fixture.Name, fork, false);
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

			var fork = method.GetCustomAttribute<ForkAttribute> ();
			if (fork != null) {
				if (method.IsConstructor)
					throw new InternalErrorException (
						$"Cannot use `[Fork]' on constructor `{DebugHelper.FormatMethod (method)}'.");
				ResolveForked (method.Name, fork, false);
			}

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

				fork = paramInfo.GetCustomAttribute<ForkAttribute> ();
				if (fork != null && paramType.Equals (typeof (IFork))) {
					ResolveForked (paramName, fork, false);
					continue;
				}

				var fork2 = paramTypeInfo.GetCustomAttribute<ForkAttribute> ();
				if (fork2 != null) {
					if (fork != null)
						throw InvalidForkedAttribute ();
					fork = fork2;
				}

				if (paramType.Equals (typeof (CancellationToken))) {
					if (seenToken)
						throw new InternalErrorException ();
					if (fork != null)
						throw InvalidForkedAttribute ();
					seenToken = true;
					continue;
				}

				if (paramType.Equals (typeof (TestContext))) {
					if (seenCtx)
						throw new InternalErrorException ();
					if (fork != null)
						throw InvalidForkedAttribute ();
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
				} else {
					var parameter = ReflectionHelper.ResolveParameter (
						Fixture.Type, paramInfo, attribute.ParameterFilter);
					Add (new ReflectionTestParameterBuilder (current, parameter));
				}

				if (fork != null)
					ResolveForked (paramName, fork, true);
			}

			if (method is ConstructorInfo constructor) {
				Add (new ReflectionTestInstanceBuilder (current, unwindBase, constructor, shared));
				if (!shared)
					ResolveFixtureProperties (true);
			} else if (this is ReflectionTestCaseBuilder caseBuilder) {
				Add (new ReflectionTestMethodBuilder (current, caseBuilder));
			} else {
				throw new InternalErrorException ();
			}

			Exception InvalidForkedAttribute ()
			{
				throw new InternalErrorException (
					$"Cannot use `[Fork]' on something that is not `IFork' in `{DebugHelper.FormatMethod (method)}'.");
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

		protected void ResolveFixtureProperties (bool instance)
		{
			for (var type = Fixture.Type; type != null; type = type.BaseType?.GetTypeInfo ()) {
				foreach (var property in type.DeclaredProperties) {
					var host = ReflectionHelper.ResolveFixtureProperty (Fixture.Type, property, instance);
					if (host == null)
						continue;
					Add (new ReflectionTestParameterBuilder (current, host));
				}
			}
		}
	}
}
