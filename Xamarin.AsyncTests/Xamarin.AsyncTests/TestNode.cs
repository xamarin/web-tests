//
// TestNode.cs
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

namespace Xamarin.AsyncTests
{
	public abstract class TestNode
	{
		public abstract TestPathType PathType {
			get;
		}

		public abstract TestFlags Flags {
			get;
		}

		public abstract string Identifier {
			get;
		}

		public abstract string Name {
			get;
		}

		public abstract string ParameterType {
			get;
		}

		public abstract string ParameterValue {
			get;
		}

		public abstract string FriendlyParameterValue {
			get;
		}

		public bool IsParameterized {
			get { return ParameterType != null; }
		}

		public bool IsHidden {
			get { return (Flags & TestFlags.Hidden) != 0; }
		}

		public bool IsBrowseable {
			get { return (Flags & TestFlags.Browsable) != 0; }
		}

		public abstract bool HasParameter {
			get;
		}

		public abstract ITestParameter Parameter {
			get;
		}

		public abstract XElement CustomParameter {
			get;
		}

		public bool Matches (TestNode node)
		{
			if (node.PathType != PathType)
				return false;
			if (!string.Equals (node.Identifier, Identifier, StringComparison.Ordinal))
				return false;
			if (IsParameterized != (node.ParameterType != null))
				return false;
			if (node.ParameterType != null && !node.ParameterType.Equals (ParameterType))
				return false;

			return true;
		}

		public bool ParameterMatches<T> (string name = null)
		{
			if (!IsParameterized)
				return false;

			if (name != null) {
				if (PathType != TestPathType.Parameter)
					return false;
				return Identifier.Equals (name);
			}

			var friendlyName = TestPath.GetFriendlyName (typeof (T));
			return friendlyName.Equals (ParameterType);
		}

		public abstract T GetParameter<T> ();

		public abstract TestNode Parameterize (ITestParameter parameter);

		internal abstract TestNode Parameterize (ITestParameter parameter, XElement customParameter);

		public abstract bool HasParameters {
			get;
		}

		internal abstract TestNode Clone ();

		public virtual void Write (XElement element)
		{
			element.Add (new XAttribute ("Type", WritePathType (PathType)));
			if (Identifier != null)
				element.Add (new XAttribute ("Identifier", Identifier));
			if (Name != null)
				element.Add (new XAttribute ("Name", Name));
			if (ParameterType != null)
				element.Add (new XAttribute ("ParameterType", ParameterType));
			if (ParameterValue != null)
				element.Add (new XAttribute ("Parameter", ParameterValue));
			if (FriendlyParameterValue != null)
				element.Add (new XAttribute ("FriendlyParameter", FriendlyParameterValue));
			if (Flags != TestFlags.None)
				element.Add (new XAttribute ("Flags", WriteTestFlags (Flags)));
			if (CustomParameter != null)
				element.Add (CustomParameter);
		}

		internal static XElement CreateCustomParameterNode ()
		{
			return new XElement (CustomParameterName);
		}

		static readonly XName CustomParameterName = "CustomParameter";

		static readonly XName ElementName = "TestParameter";

		public static XElement WriteNode (TestNode node)
		{
			var element = new XElement (ElementName);
			node.Write (element);
			return element;
		}

		public static TestNode ReadNode (XElement element)
		{
			return new PathNodeWrapper (element);
		}

		internal static IEnumerable<TestNode> ReadAllNodes (XElement root)
		{
			foreach (var element in root.Elements (ElementName)) {
				yield return ReadNode (element);
			}
		}

		public override string ToString ()
		{
			string parameter = ParameterValue != null ? string.Format (", Parameter={0}", ParameterValue) : string.Empty;
			return string.Format ("[TestNode: Type={0}, Identifier={1}, Name={2}{3}]", PathType, Identifier, Name, parameter);
		}

		static TestPathType ReadPathType (string value)
		{
			return (TestPathType)Enum.Parse (typeof (TestPathType), value, true);
		}

		static string WritePathType (TestPathType type)
		{
			return Enum.GetName (typeof (TestPathType), type).ToLowerInvariant ();
		}

		static string WriteTestFlags (TestFlags flags)
		{
			return Enum.Format (typeof (TestFlags), flags, "g");
		}

		static TestFlags ReadTestFlags (string value)
		{
			return (TestFlags)Enum.Parse (typeof (TestFlags), value);
		}

		sealed class PathNodeWrapper : TestNode
		{
			public override TestPathType PathType {
				get;
			}
			public override TestFlags Flags {
				get;
			}
			public override string Identifier {
				get;
			}
			public override string Name {
				get;
			}
			public override string ParameterType {
				get;
			}
			public override string ParameterValue {
				get;
			}
			public override string FriendlyParameterValue {
				get;
			}
			public override XElement CustomParameter {
				get;
			}

			public PathNodeWrapper (XElement element)
			{
				PathType = ReadPathType (element.Attribute ("Type").Value);

				var flagsAttr = element.Attribute ("Flags");
				if (flagsAttr != null)
					Flags = ReadTestFlags (flagsAttr.Value);

				Identifier = element.Attribute ("Identifier")?.Value;

				Name = element.Attribute ("Name")?.Value;

				ParameterType = element.Attribute ("ParameterType")?.Value;

				ParameterValue = element.Attribute ("Parameter")?.Value;

				FriendlyParameterValue = element.Attribute ("FriendlyParameter")?.Value;

				CustomParameter = element.Element (CustomParameterName);
			}

			PathNodeWrapper (TestNode other)
			{
				PathType = other.PathType;
				Flags = other.Flags;
				Identifier = other.Identifier;
				Name = other.Name;
				ParameterType = other.ParameterType;
				ParameterValue = other.ParameterValue;
				FriendlyParameterValue = other.FriendlyParameterValue;
			}

			public override bool HasParameter => false;

			public override ITestParameter Parameter {
				get {
					throw new InternalErrorException ();
				}
			}

			public override T GetParameter<T> ()
			{
				throw new InternalErrorException ();
			}

			public override TestNode Parameterize (ITestParameter parameter)
			{
				throw new InternalErrorException ();
			}

			internal override TestNode Parameterize (ITestParameter parameter, XElement customParameter)
			{
				throw new InternalErrorException ();
			}

			public override bool HasParameters {
				get {
					throw new InternalErrorException ();
				}
			}

			internal override TestNode Clone ()
			{
				return new PathNodeWrapper (this);
			}
		}
	}
}

