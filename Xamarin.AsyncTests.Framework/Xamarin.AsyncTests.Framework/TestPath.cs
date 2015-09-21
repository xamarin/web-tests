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
using System.Reflection;
using System.Xml.Linq;
using System.Collections.Generic;

namespace Xamarin.AsyncTests.Framework
{
	sealed class TestPath : ITestPath, ITestPathInternal
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

		ITestPathInternal ITestPathInternal.Parent {
			get { return Parent; }
		}

		public TestName Name {
			get { return name; }
		}

		public bool IsParameterized {
			get { return Host.ParameterType != null; }
		}

		public bool HasParameter {
			get { return parameter != null; }
		}

		public ITestParameter Parameter {
			get { return parameter; }
		}

		public bool IsHidden {
			get { return (Flags & TestFlags.Hidden) != 0; } 
		}

		public bool IsBrowseable {
			get { return (Flags & TestFlags.Browsable) != 0; }
		}

		readonly ITestParameter parameter;
		readonly TestName name;

		internal TestPath (TestHost host, TestPath parent, ITestParameter parameter = null)
		{
			Host = host;
			Flags = host.Flags;
			Parent = parent;

			this.parameter = host.HasFixedParameter ? host.GetFixedParameter () : parameter;
			this.name = GetTestName (host, parent, this.parameter);
		}

		internal TestPath Clone ()
		{
			return new TestPath (Host, Parent, Parameter);
		}

		static TestName GetTestName (TestHost host, TestPath parent, ITestParameter parameter = null)
		{
			var builder = new TestNameBuilder ();
			if (parent != null)
				builder.Merge (parent.name);
			if (host.Name != null) {
				if (parameter != null && ((host.Flags & TestFlags.PathHidden) == 0))
					builder.PushParameter (host.Name, parameter.Value, (host.Flags & TestFlags.Hidden) != 0);
				else if ((host.Flags & TestFlags.Hidden) == 0)
					builder.PushName (host.Name);
			}
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

		public bool ParameterMatches<T> (string name = null)
		{
			if (!IsParameterized)
				return false;

			if (name != null)
				return Host.Identifier.Equals (name);
			else {
				var friendlyName = TestSerializer.GetFriendlyName (typeof(T));
				return friendlyName.Equals (Host.ParameterType);
			}
		}

		public T GetParameter<T> ()
		{
			if (Parameter == null)
				return default (T);

			var typeInfo = typeof(T).GetTypeInfo ();
			if (typeInfo.IsEnum)
				return (T)Enum.Parse (typeof (T), Parameter.Value);

			var wrapper = Parameter as ITestParameterWrapper;
			if (wrapper != null)
				return (T)wrapper.Value;

			return (T)Parameter;
		}

		public XElement SerializePath ()
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

