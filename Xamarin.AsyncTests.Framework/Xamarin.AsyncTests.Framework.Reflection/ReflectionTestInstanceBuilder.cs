//
// ReflectionTestInstanceBuilder.cs
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
using System.Collections.Generic;

namespace Xamarin.AsyncTests.Framework.Reflection
{
	class ReflectionTestInstanceBuilder : ReflectionTestBuilder
	{
		public TestBuilder UnwindBase {
			get;
		}

		public ConstructorInfo Constructor {
			get;
		}

		public bool IsShared {
			get;
		}

		public TypeInfo FixtureType => Fixture.Type;

		protected ReflectionFixtureHost FixtureHost {
			get;
		}

		public ReflectionTestInstanceBuilder (
			TestBuilder parent, TestBuilder unwindBase, ConstructorInfo ctor, bool shared)
			: base (parent, TestPathType.Instance, null, GetFixtureName (parent), null)
		{
			UnwindBase = unwindBase;
			Constructor = ctor;
			IsShared = shared;

			Filter = new TestFilter (this, parent.Filter, false, new TestCategoryAttribute[0], new TestFeature[0]);

			FixtureHost = new ReflectionFixtureHost (Fixture, this, ctor);
		}

		static string GetFixtureName (TestBuilder builder)
		{
			var fixtureBuilder = (ReflectionTestFixtureBuilder)GetFixtureBuilder (builder);
			return TestPath.GetFriendlyName (fixtureBuilder.Type.AsType ());
		}

		public sealed override TestFilter Filter {
			get;
		}

		protected override void ResolveMembers ()
		{
			base.ResolveMembers ();

			ResolveFixtureProperties (true);

			AddMethods (Fixture.InstanceMethods);
		}

		protected override TestHost CreateHost ()
		{
			return FixtureHost;
		}
	}
}
