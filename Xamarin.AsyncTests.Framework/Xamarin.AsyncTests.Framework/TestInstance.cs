//
// TestInstance.cs
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
using System.Reflection;
using System.Xml.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Xamarin.AsyncTests.Framework
{
	abstract class TestInstance : ITestPathInternal
	{
		public TestHost Host {
			get;
			private set;
		}

		public TestInstance Parent {
			get;
			private set;
		}

		ITestPathInternal ITestPathInternal.Parent {
			get { return Parent; }
		}

		public TestPath Path {
			get;
			private set;
		}

		protected TestInstance (TestHost host, TestPath path, TestInstance parent)
		{
			if (host == null)
				throw new ArgumentNullException ("host");
			if (path == null)
				throw new ArgumentNullException ("path");

			Host = host;
			Parent = parent;
			Path = path;

			if (ID == 55 || ID == 57)
				System.Diagnostics.Debug.WriteLine ("DEBUG ME");
		}

		internal abstract ITestParameter GetCurrentParameter ();

		protected FixtureTestInstance GetFixtureInstance ()
		{
			TestInstance instance = this;
			while (instance != null) {
				var fixtureInstance = instance as FixtureTestInstance;
				if (fixtureInstance != null)
					return fixtureInstance;

				instance = instance.Parent;
			}

			throw new InternalErrorException ();
		}

		public virtual void Initialize (TestContext ctx)
		{
		}

		public virtual void Destroy (TestContext ctx)
		{
		}

		static int nextId;
		public readonly int ID = ++nextId;

		TestPath GetCurrentPath ()
		{
			System.Diagnostics.Debug.WriteLine ("  GET CURRENT PATH: {0}:{1} - {2}:{3}", ID, this, Parent?.ID, Parent);
;			TestPath parentPath = null;
			if (Parent != null)
				parentPath = Parent.GetCurrentPath ();

			var parameter = GetCurrentParameter ();
			return new TestPath (Path.Host, parentPath, parameter);
		}

		public static TestPath GetCurrentPath (TestInstance instance)
		{
			System.Diagnostics.Debug.WriteLine ("TEST INSTANCE - GET CURRENT PATH: {0}:{1}", instance.ID, instance);
			var path = instance.GetCurrentPath ();
			System.Diagnostics.Debug.WriteLine ("TEST INSTANCE - GET CURRENT PATH DONE: {0}:{1} - {2}", instance.ID, instance, path);
			return path;
		}

		public TestPath GetCurrentPath (TestPath parentPath)
		{
			var parameter = GetCurrentParameter ();
			return new TestPath (Path.Host, parentPath, parameter);
		}

		public static TestName GetTestName (TestInstance instance)
		{
			return GetCurrentPath (instance).TestName;
		}

		public virtual bool ParameterMatches<T> (string name)
		{
			return Path.ParameterMatches<T> (name);
		}

		public virtual T GetParameter<T> ()
		{
			var parameter = GetCurrentParameter ();
			if (parameter == null)
				throw new InternalErrorException ();

			var path = new TestPath (Parent.Host, Parent.Path, parameter);
			return path.GetParameter<T> ();
		}

		public override string ToString ()
		{
			return string.Format ("[{0}: Host={1}, Parent={2}]", GetType ().Name, Host, Parent);
		}
	}
}

