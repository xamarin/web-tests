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
using System.Collections.Generic;

namespace Xamarin.AsyncTests.Framework
{
	sealed class TestPath : ITestPath
	{
		public TestHost Host {
			get;
			private set;
		}

		public TestFlags Flags {
			get;
			internal set;
		}

		public TestPath Parent {
			get;
			private set;
		}

		public bool IsParameterized {
			get { return Host.ParameterType != null; }
		}

		public bool HasParameter {
			get { return Parameter != null; }
		}

		public ITestParameter Parameter {
			get;
			private set;
		}

		public bool IsHidden {
			get { return (Flags & TestFlags.Hidden) != 0; } 
		}

		public bool IsBrowseable {
			get { return (Flags & TestFlags.Browsable) != 0; }
		}

		internal TestPath (TestHost host, TestPath parent, ITestParameter parameter = null)
		{
			Host = host;
			Flags = host.Flags;
			Parent = parent;
			Parameter = parameter;
		}

		internal TestPath Clone ()
		{
			return new TestPath (Host, Parent, Parameter);
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

		public static TestName GetTestName (TestPath path)
		{
			var builder = new TestNameBuilder ();
			if (path != null)
				path.GetTestName (builder);
			return builder.GetName ();
		}

		public TestPath Parameterize (ITestParameter parameter)
		{
			if (!IsParameterized)
				throw new InternalErrorException ();
			return new TestPath (Host, Parent, parameter);
		}

		internal TestBuilder GetTestBuilder ()
		{
			var builder = Host as TestBuilderHost;
			if (builder != null)
				return builder.Builder;
			return Parent.GetTestBuilder ();
		}

		public bool Matches (IPathNode node)
		{
			if (!node.Identifier.Equals (Host.Identifier))
				return false;
			if (IsParameterized != (node.ParameterType != null))
				return false;
			if (node.ParameterType != null && !node.ParameterType.Equals (Host.ParameterType))
				return false;

			return true;
		}

		public static bool Matches (IPathNode first, IPathNode second)
		{
			if (!first.Identifier.Equals (second.Identifier))
				return false;
			if ((first.Name != null) != (second.Name != null))
				return false;
			if ((first.ParameterType != null) != (second.ParameterType != null))
				return false;
			if (first.ParameterType != null && !first.ParameterType.Equals (second.ParameterType))
				return false;

			return true;
		}

		public XElement Serialize ()
		{
			return TestSerializer.SerializePath (this);
		}

		public readonly int ID = ++next_id;
		static int next_id;

		public override string ToString ()
		{
			string parameter = IsParameterized ? string.Format (", Parameter={0}", Parameter != null ? Parameter.Value : "<null>") : string.Empty;
			var parent = Parent != null ? string.Format (", Parent={0}", Parent.ID) : string.Empty;
			return string.Format ("[TestPath: ID={0}, Identifier={1}, Name={2}{3}{4}]", ID, Host.Identifier, Host.Name, parameter, parent);
		}
	}
}

