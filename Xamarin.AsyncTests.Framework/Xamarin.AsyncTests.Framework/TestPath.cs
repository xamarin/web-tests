//
// TestPath.cs
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
using System.Xml.Linq;

namespace Xamarin.AsyncTests.Framework
{
	sealed class TestPath
	{
		public TestHost Host {
			get;
			private set;
		}

		public TestPath Parent {
			get;
			private set;
		}

		[Obsolete ("MUST GO")]
		public TestBuilder BrokenBuilder {
			get;
			private set;
		}

		public bool IsParameterized {
			get { return Host.ParameterType != null; }
		}

		public ITestParameter Parameter {
			get;
			private set;
		}

		internal TestPath (TestHost host, TestPath parent)
		{
			Host = host;
			Parent = parent;

			var builderHost = host as TestBuilderHost;
			if (builderHost != null)
				BrokenBuilder = builderHost.Builder;
		}

		void GetTestName (TestNameBuilder builder)
		{
			if (Parent != null)
				Parent.GetTestName (builder);
			if (Host.Name != null) {
				if (Parameter != null && ((Host.Flags & TestFlags.PathHidden) == 0))
					builder.PushParameter (Host.Name, Parameter.Value, (Host.Flags & TestFlags.Hidden) != 0);
				else
					builder.PushName (Host.Name);
			}
		}

		[Obsolete ("REMOVE")]
		public static TestPath CreateFromInstance (TestInstance instance)
		{
			TestPath parent = null;
			if (instance.Parent != null)
				parent = CreateFromInstance (instance.Parent);
			var path = new TestPath (instance.Host, parent);
			var parameter = instance.Host.GetParameter (instance);
			if (parameter != null)
				path = path.Parameterize (parameter);
			return path;
		}

		public static TestName GetTestName (TestPath path)
		{
			var builder = new TestNameBuilder ();
			if (path != null)
				path.GetTestName (builder);
			return builder.GetName ();
		}

		public TestPath Parameterize (ITestParameter parameter)
		{
			if (!IsParameterized || Parameter != null)
				throw new InternalErrorException ();
			var newPath = new TestPath (Host, Parent);
			newPath.Parameter = parameter;
			newPath.BrokenBuilder = BrokenBuilder;
			return newPath;
		}

		internal TestInvoker CreateInvoker ()
		{
			TestInvoker invoker = null;
			if (Parent != null)
				invoker = Parent.CreateInvoker ();
			else
				invoker = BrokenBuilder.Invoker;
			invoker = Host.CreateInvoker (invoker);
			return invoker;
		}

		public readonly int ID = ++next_id;
		static int next_id;

		public override string ToString ()
		{
			return string.Format ("[TestPath: ID={0}, Type={1}, Identifier={2}, Parent={3}]", ID, Host.TypeKey, Host.Identifier, Parent != null ? Parent.ID : 0);
		}
	}
}

