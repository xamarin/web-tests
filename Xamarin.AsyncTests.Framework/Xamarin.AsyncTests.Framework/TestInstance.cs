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
	abstract class TestInstance
	{
		public TestHost Host {
			get;
		}

		public TestInstance Parent {
			get;
		}

		TestPath Path {
			get;
		}

		public TestPath ParentPath {
			get;
		}

		public TestNode Node {
			get;
		}

		internal int ID {
			get;
		}

		static int nextId;

		protected TestInstance (TestHost host, TestNode node, TestInstance parent)
		{
			Host = host ?? throw new ArgumentNullException (nameof (host));
			Node = node ?? throw new ArgumentNullException (nameof (node));
			Parent = parent;

			ID = Interlocked.Increment (ref nextId);

			ParentPath = Parent?.GetCurrentPath ();
			Path = new TestPath (ParentPath, Node);
		}

		internal abstract TestParameterValue GetCurrentParameter ();

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

		public TestPath GetCurrentPath ()
		{
			var parameter = GetCurrentParameter ();
			if (parameter == null)
				return Path;

			return parameter.GetCurrentPath ();
		}

		internal static void LogDebug (TestContext ctx, TestInstance instance, int level)
		{
			while (instance != null) {
				ctx.LogDebug (TestInvoker.LogCategory, level, $"    {instance}");
				instance = instance.Parent;
			}
		}

		protected virtual string MyToString () => null;

		public override string ToString ()
		{
			var parent = Parent != null ? $", Parent={Parent.ID}" : string.Empty;
			var my = MyToString ();
			if (my != null)
				my = ":" + my;
			return $"[{DebugHelper.FormatType (this)}({ID}{my}): Host={Host}{parent}]";
		}
	}
}

