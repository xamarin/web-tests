//
// PathNode.cs
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
	public abstract class PathNode
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
			if (Flags != TestFlags.None)
				element.Add (new XAttribute ("Flags", WriteTestFlags (Flags)));
		}

		static readonly XName ElementName = "TestParameter";

		public static XElement WriteNode (PathNode node)
		{
			var element = new XElement (ElementName);
			node.Write (element);
			return element;
		}

		public static PathNode ReadNode (XElement element)
		{
			return new PathNodeWrapper (element);
		}

		internal static IEnumerable<PathNode> ReadAllNodes (XElement root)
		{
			foreach (var element in root.Elements (ElementName)) {
				yield return ReadNode (element);
			}
		}

		public override string ToString ()
		{
			string parameter = ParameterValue != null ? string.Format (", Parameter={0}", ParameterValue) : string.Empty;
			return string.Format ("[TestPathNode: Type={0}, Identifier={1}, Name={2}{3}]", PathType, Identifier, Name, parameter);
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

		class PathNodeWrapper : PathNode
		{
			readonly TestPathType pathType;
			readonly TestFlags flags;
			readonly string identifier;
			readonly string name;
			readonly string parameterType;
			readonly string parameterValue;

			public override TestPathType PathType {
				get { return pathType; }
			}
			public override TestFlags Flags {
				get { return flags; }
			}
			public override string Identifier {
				get { return identifier; }
			}
			public override string Name {
				get { return name; }
			}
			public override string ParameterType {
				get { return parameterType; }
			}
			public override string ParameterValue {
				get { return parameterValue; }
			}

			public PathNodeWrapper (XElement element)
			{
				pathType = ReadPathType (element.Attribute ("Type").Value);

				var flagsAttr = element.Attribute ("Flags");
				if (flagsAttr != null)
					flags = ReadTestFlags (flagsAttr.Value);

				var identifierAttr = element.Attribute ("Identifier");
				if (identifierAttr != null)
					identifier = identifierAttr.Value;

				var nameAttr = element.Attribute ("Name");
				if (nameAttr != null)
					name = nameAttr.Value;

				var paramTypeAttr = element.Attribute ("ParameterType");
				if (paramTypeAttr != null)
					parameterType = paramTypeAttr.Value;

				var paramValueAttr = element.Attribute ("Parameter");
				if (paramValueAttr != null)
					parameterValue = paramValueAttr.Value;
			}
		}
	}
}

