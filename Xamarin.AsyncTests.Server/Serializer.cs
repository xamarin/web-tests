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
	using Framework;

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
				var client = (ClientConnection)Connection;
				var objectId = client.RegisterTest (result);
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
				result.AddError (new SavedException (error.Value));

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

		public string Write (TestConfiguration config)
		{
			var sb = new StringBuilder ();
			var settings = new XmlWriterSettings ();
			settings.OmitXmlDeclaration = true;
			var writer = XmlWriter.Create (sb, settings);
			Write (writer, config);
			return sb.ToString ();
		}

		public void Write (XmlWriter writer, TestConfiguration config)
		{
			writer.WriteStartElement ("TestConfiguration");
			foreach (var category in config.Categories) {
				if (category.IsBuiltin)
					continue;
				writer.WriteStartElement ("Category");
				writer.WriteAttributeString ("Name", category.Name);
				if (category == config.CurrentCategory)
					writer.WriteAttributeString ("IsCurrent", "true");
				writer.WriteEndElement ();
			}
			foreach (var feature in config.Features) {
				writer.WriteStartElement ("Feature");
				writer.WriteAttributeString ("Name", feature.Name);
				writer.WriteAttributeString ("Description", feature.Description);
				if (feature.Constant != null)
					writer.WriteAttributeString ("Constant", feature.Constant.Value.ToString ());
				if (feature.DefaultValue != null)
					writer.WriteAttributeString ("DefaultValue", feature.DefaultValue.Value.ToString ());
				if (feature.CanModify)
					writer.WriteAttributeString ("IsEnabled", config.IsEnabled (feature).ToString ());
				writer.WriteEndElement ();
			}
			writer.WriteEndElement ();
		}

		public TestConfiguration ReadConfiguration (XmlReader reader)
		{
			var doc = XDocument.Load (reader);
			var element = doc.Root.Element (XName.Get ("TestConfiguration"));
			return ReadConfiguration (element);
		}

		public TestConfiguration ReadConfiguration (XElement element)
		{
			if (!element.Name.LocalName.Equals ("TestConfiguration"))
				throw new InvalidOperationException ();

			var config = new TestConfiguration ();
			foreach (var item in element.Elements (XName.Get ("Category"))) {
				var category = new TestCategory (item.Attribute (XName.Get ("Name")).Value);
				config.AddCategory (category);
				var current = item.Attribute (XName.Get ("IsCurrent"));
				if (current != null && bool.Parse (current.Value))
					config.CurrentCategory = category;
			}
			foreach (var item in element.Elements (XName.Get ("Feature"))) {
				var name = item.Attribute (XName.Get ("Name")).Value;
				var description = item.Attribute (XName.Get ("Description")).Value;
				var constant = item.Attribute (XName.Get ("Constant"));
				var defaultValue = item.Attribute (XName.Get ("DefaultValue"));
				var enabled = item.Attribute (XName.Get ("IsEnabled"));

				TestFeature feature;
				if (constant != null) {
					var constantValue = bool.Parse (constant.Value);
					feature = new TestFeature (name, description, () => constantValue);
				} else if (defaultValue != null)
					feature = new TestFeature (name, description, bool.Parse (defaultValue.Value));
				else
					feature = new TestFeature (name, description);

				bool isEnabled;
				if (enabled != null)
					isEnabled = bool.Parse (enabled.Value);
				else
					isEnabled = feature.Constant ?? feature.DefaultValue ?? false;

				config.AddFeature (feature, isEnabled);
			}
			return config;
		}

		public string Write (TestSuite suite)
		{
			var sb = new StringBuilder ();
			var settings = new XmlWriterSettings ();
			settings.OmitXmlDeclaration = true;
			var writer = XmlWriter.Create (sb, settings);
			Write (writer, suite);
			return sb.ToString ();
		}

		public void Write (XmlWriter writer, TestSuite suite)
		{
			writer.WriteStartElement ("TestSuite");

			var client = Connection as ClientConnection;
			if (client == null)
				throw new InvalidOperationException ();

			var objectId = client.RegisterTestSuite (suite);
			writer.WriteAttributeString ("ObjectID", objectId.ToString ());

			Write (writer, suite.Name);
			writer.WriteEndElement ();
		}

		public TestSuite ReadTestSuite (XmlReader reader)
		{
			var doc = XDocument.Load (reader);
			var element = doc.Root.Element (XName.Get ("TestSuite"));
			return ReadTestSuite (element);
		}

		public TestSuite ReadTestSuite (XElement element)
		{
			var server = Connection as ServerConnection;
			if (server == null)
				throw new InvalidOperationException ();
			if (!element.Name.LocalName.Equals ("TestSuite"))
				throw new InvalidOperationException ();

			var name = ReadName (element.Element (XName.Get ("TestName")));
			var objectId = long.Parse (element.Attribute (XName.Get ("ObjectID")).Value);

			return new RemoteTestSuite (name, server, objectId);
		}
	}
}

