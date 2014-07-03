//
// CommandSerializer.cs
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
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.Linq;

namespace Xamarin.AsyncTests.Server
{
	class Serializer
	{
		public Connection Connection {
			get;
			private set;
		}

		public Serializer (Connection connection)
		{
			Connection = connection;
		}

		public string Write (Command command)
		{
			var sb = new StringBuilder ();
			var settings = new XmlWriterSettings ();
			settings.OmitXmlDeclaration = true;
			var writer = XmlWriter.Create (sb, settings);
			writer.WriteStartElement (command.GetType ().FullName);
			command.WriteXml (this, writer);
			writer.WriteEndElement ();

			return sb.ToString ();
		}

		public Command ReadCommand (string formatted)
		{
			var reader = XmlReader.Create (new StringReader (formatted));

			var ok = reader.Read ();
			if (!ok || reader.NodeType != XmlNodeType.Element)
				throw new InvalidOperationException ();

			var type = Type.GetType (reader.LocalName);

			var command = (Command)Activator.CreateInstance (type);
			command.ReadXml (this, reader);

			if (reader.Read ())
				throw new InvalidOperationException ();

			return command;
		}

		public static string Write (TestName name)
		{
			var sb = new StringBuilder ();
			var settings = new XmlWriterSettings ();
			settings.OmitXmlDeclaration = true;
			var writer = XmlWriter.Create (sb, settings);
			Write (writer, name);
			return sb.ToString ();
		}

		public static void Write (XmlWriter writer, TestName name)
		{
			writer.WriteStartElement ("TestName");
			writer.WriteAttributeString ("Name", name.Name);

			if (name.HasParameters) {
				foreach (var parameter in name.Parameters) {
					writer.WriteStartElement ("Parameter");
					writer.WriteAttributeString ("Name", parameter.Name);
					writer.WriteAttributeString ("Value", parameter.Value);
					writer.WriteAttributeString ("IsHidden", parameter.IsHidden.ToString ());
					writer.WriteEndElement ();
				}
			}

			writer.WriteEndElement ();
		}

		public static TestName ReadName (string formatted)
		{
			var reader = XmlReader.Create (new StringReader (formatted));
			return ReadName (reader);
		}

		public static TestName ReadName (XmlReader reader)
		{
			var doc = XDocument.Load (reader);
			return ReadName (doc.Root);
		}

		public static TestName ReadName (XElement element)
		{
			if (!element.Name.LocalName.Equals ("TestName"))
				throw new InvalidOperationException ();

			var builder = new TestNameBuilder ();
			builder.PushName (element.Attribute (XName.Get ("Name")).Value);

			foreach (var child in element.Elements (XName.Get ("Parameter"))) {
				var name = child.Attribute (XName.Get ("Name")).Value;
				var value = child.Attribute (XName.Get ("Value")).Value;
				var isHidden = bool.Parse (child.Attribute (XName.Get ("IsHidden")).Value);
				builder.PushParameter (name, value, isHidden);
			}

			return builder.GetName ();
		}

		public string Write (TestResult result)
		{
			var sb = new StringBuilder ();
			var settings = new XmlWriterSettings ();
			settings.OmitXmlDeclaration = true;
			var writer = XmlWriter.Create (sb, settings);
			Write (writer, result);
			return sb.ToString ();
		}

		public void Write (XmlWriter writer, TestResult result)
		{
			writer.WriteStartElement ("TestResult");
			writer.WriteAttributeString ("Status", result.Status.ToString ());
			if (result.Message != null)
				writer.WriteAttributeString ("Message", result.Message);
			if (result.Error != null)
				writer.WriteAttributeString ("Error", result.Error.ToString ());
			Write (writer, result.Name);

			if (result.Test != null) {
				var objectId = RegisterTest (result);
				writer.WriteStartElement ("TestCase");
				writer.WriteAttributeString ("ObjectID", objectId.ToString ());
				Write (writer, result.Test.Name);
				writer.WriteEndElement ();
			}

			foreach (var child in result.Children)
				Write (writer, child);

			writer.WriteEndElement ();
		}

		public TestResult ReadResult (string formatted)
		{
			var reader = XmlReader.Create (new StringReader (formatted));
			return ReadResult (reader);
		}

		public TestResult ReadResult (XmlReader reader)
		{
			var doc = XDocument.Load (reader);
			var element = doc.Root.Element (XName.Get ("TestResult"));
			return ReadResult (element);
		}

		public TestResult ReadResult (XElement element)
		{
			if (!element.Name.LocalName.Equals ("TestResult"))
				throw new InvalidOperationException ();

			var name = ReadName (element.Element (XName.Get ("TestName")));
			var status = (TestStatus)Enum.Parse (typeof(TestStatus), element.Attribute (XName.Get ("Status")).Value);

			var result = new TestResult (name, status);

			var message = element.Attribute (XName.Get ("Message"));
			if (message != null)
				result.Message = message.Value;

			var error = element.Attribute (XName.Get ("Error"));
			if (error != null)
				result.Error = new SavedException (error.Value);

			var test = element.Element (XName.Get ("TestCase"));
			if (test != null)
				result.Test = ReadTest (test);

			foreach (var child in element.Elements (XName.Get ("TestResult"))) {
				result.AddChild (ReadResult (child));
			}

			return result;
		}

		TestCase ReadTest (XElement element)
		{
			var server = Connection as ServerConnection;
			if (server == null)
				throw new InvalidOperationException ();
			if (!element.Name.LocalName.Equals ("TestCase"))
				throw new InvalidOperationException ();

			var name = ReadName (element.Element (XName.Get ("TestName")));
			var objectId = long.Parse (element.Attribute (XName.Get ("ObjectID")).Value);

			return new RemoteTestCase (name, server, objectId);
		}

		static long next_id;
		Dictionary<long,TestCase> tests = new Dictionary<long,TestCase> ();

		long RegisterTest (TestResult result)
		{
			if (result.Test == null)
				return -1;

			var id = ++next_id;
			tests.Add (id, result.Test);
			result.PropertyChanged += (sender, e) => {
				if (e.PropertyName.Equals ("Test"))
					tests [id] = result.Test;
			};
			return id;
		}

		public TestCase GetTest (long id)
		{
			return tests [id];
		}
	}
}

