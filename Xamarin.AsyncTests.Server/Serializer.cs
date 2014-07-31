//
// ParameterSerializer.cs
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
using System.Text;
using System.ComponentModel;
using System.Xml;
using System.Xml.Linq;

namespace Xamarin.AsyncTests.Server
{
	using Framework;

	static class Serializer
	{
		public static readonly Serializer<string> String = new StringSerializer ();
		public static readonly Serializer<TestName> TestName = new TestNameSerializer ();
		public static readonly Serializer<TestSuite> TestSuite = new TestSuiteSerializer ();
		public static readonly Serializer<TestCase> TestCase = new TestCaseSerializer ();
		public static readonly Serializer<TestResult> TestResult = new TestResultSerializer ();
		public static readonly Serializer<TestConfiguration> Configuration = new ConfigurationSerializer ();
		public static readonly Serializer<TestStatistics.StatisticsEventArgs> StatisticsEventArgs = new StatisticsEventArgsSerializer ();
		public static readonly Serializer<Handshake> Handshake = new HandshakeSerializer ();

		static readonly SettingsSerializer settingsSerializer = new SettingsSerializer ();

		public static Serializer<SettingsBag> Settings {
			get { return settingsSerializer; }
		}

		internal static SettingsBag ReadSettings (XElement node)
		{
			return settingsSerializer.Read (node);
		}

		internal static XElement WriteSettings (SettingsBag settings)
		{
			return settingsSerializer.Write (settings);
		}

		class SettingsSerializer : Serializer<SettingsBag>
		{
			public override XElement Write (Connection connection, SettingsBag instance)
			{
				return Write (instance);
			}

			public XElement Write (SettingsBag instance)
			{
				var settings = new XElement ("Settings");

				foreach (var entry in instance.Settings) {
					var item = new XElement ("Setting");
					settings.Add (item);

					item.SetAttributeValue ("Key", entry.Key);
					item.SetAttributeValue ("Value", entry.Value);
				}

				return settings;
			}

			public override SettingsBag Read (Connection connection, XElement node)
			{
				return Read (node);
			}

			public SettingsBag Read (XElement node)
			{
				if (!node.Name.LocalName.Equals ("Settings"))
					throw new InvalidOperationException ();

				var settings = SettingsBag.CreateDefault ();
				foreach (var element in node.Elements ("Setting")) {
					var key = element.Attribute ("Key");
					var value = element.Attribute ("Value");
					settings.Add (key.Value, value.Value);
				}
				return settings;
			}
		}

		class StringSerializer : Serializer<string>
		{
			public override string Read (Connection connection, XElement node)
			{
				if (!node.Name.LocalName.Equals ("Text"))
					throw new InvalidOperationException ();

				return node.Attribute ("Value").Value;
			}

			public override XElement Write (Connection connection, string instance)
			{
				var element = new XElement ("Text");
				element.SetAttributeValue ("Value", instance);
				return element;
			}
		}

		class TestNameSerializer : Serializer<TestName>
		{
			public override TestName Read (Connection connection, XElement node)
			{
				if (!node.Name.LocalName.Equals ("TestName"))
					throw new InvalidOperationException ();

				var builder = new TestNameBuilder ();
				var nameAttr = node.Attribute ("Name");
				if (nameAttr != null)
					builder.PushName (nameAttr.Value);

				foreach (var child in node.Elements ("Parameter")) {
					var name = child.Attribute ("Name").Value;
					var value = child.Attribute ("Value").Value;
					var isHidden = bool.Parse (child.Attribute ("IsHidden").Value);
					builder.PushParameter (name, value, isHidden);
				}

				return builder.GetName ();
			}

			public override XElement Write (Connection connection, TestName instance)
			{
				var element = new XElement ("TestName");
				if (instance.Name != null)
					element.SetAttributeValue ("Name", instance.Name);

				if (instance.HasParameters) {
					foreach (var parameter in instance.Parameters) {
						var node = new XElement ("Parameter");
						element.Add (node);

						node.SetAttributeValue ("Name", parameter.Name);
						node.SetAttributeValue ("Value", parameter.Value);
						node.SetAttributeValue ("IsHidden", parameter.IsHidden.ToString ());
					}
				}

				return element;
			}
		}

		class TestSuiteSerializer : Serializer<TestSuite>
		{
			public override TestSuite Read (Connection connection, XElement node)
			{
				if (!node.Name.LocalName.Equals ("TestSuite"))
					throw new InvalidOperationException ();

				var objectId = long.Parse (node.Attribute ("ObjectID").Value);

				TestSuite suite;
				if (connection.TryGetRemoteObject<TestSuite> (objectId, out suite))
					return suite;

				var name = Serializer.TestName.Read (connection, node.Element ("TestName"));
				var config = Serializer.Configuration.Read (connection, node.Element ("TestConfiguration"));

				return new RemoteTestSuite (connection, objectId, name, config);
			}

			public override XElement Write (Connection connection, TestSuite instance)
			{
				var element = new XElement ("TestSuite");
				var objectId = connection.RegisterRemoteObject (instance);
				element.SetAttributeValue ("ObjectID", objectId);

				var name = Serializer.TestName.Write (connection, instance.Name);
				element.Add (name);

				if (instance.Configuration != null) {
					var config = Serializer.Configuration.Write (connection, instance.Configuration);
					element.Add (config);
				}

				return element;
			}
		}

		class TestCaseSerializer : Serializer<TestCase>
		{
			public override TestCase Read (Connection connection, XElement node)
			{
				if (!node.Name.LocalName.Equals ("TestCase"))
					throw new InvalidOperationException ();

				var objectId = long.Parse (node.Attribute ("ObjectID").Value);

				TestCase test;
				if (connection.TryGetRemoteObject<TestCase> (objectId, out test))
					return test;

				var name = Serializer.TestName.Read (connection, node.Element ("TestName"));

				return new RemoteTestCase (name, connection, objectId);
			}

			public override XElement Write (Connection connection, TestCase instance)
			{
				var element = new XElement ("TestCase");
				var objectId = connection.RegisterRemoteObject (instance);
				element.SetAttributeValue ("ObjectID", objectId);

				var name = Serializer.TestName.Write (connection, instance.Name);
				element.Add (name);
				return element;
			}
		}

		class TestResultSerializer : Serializer<TestResult>
		{
			public override TestResult Read (Connection connection, XElement node)
			{
				if (!node.Name.LocalName.Equals ("TestResult"))
					throw new InvalidOperationException ();

				var name = Serializer.TestName.Read (connection, node.Element ("TestName"));
				var status = (TestStatus)Enum.Parse (typeof(TestStatus), node.Attribute ("Status").Value);

				var result = new TestResult (name, status);

				var error = node.Attribute ("Error");
				if (error != null)
					result.AddError (new SavedException (error.Value));

				var test = node.Element ("TestCase");
				if (connection != null && test != null) {
					var testcase = Serializer.TestCase.Read (connection, test);
					result.Test = testcase;
					result.PropertyChanged += (sender, e) => {
						if (result.Test == testcase)
							return;
						if (result.Test != null)
							throw new InvalidOperationException ();
						connection.UnregisterRemoteObject (testcase);
					};
				}

				foreach (var child in node.Elements ("TestResult")) {
					result.AddChild (Read (connection, child));
				}

				return result;
			}

			void OnPropertyChanged (Connection connection, TestResult result, TestCase oldTest)
			{
				if (result.Test == oldTest)
					return;

			}

			public override XElement Write (Connection connection, TestResult instance)
			{
				var element = new XElement ("TestResult");
				element.SetAttributeValue ("Status", instance.Status.ToString ());

				if (instance.Error != null)
					element.SetAttributeValue ("Error", instance.Error.ToString ());

				element.Add (Serializer.TestName.Write (connection, instance.Name));

				if (connection != null && instance.Test != null)
					element.Add (Serializer.TestCase.Write (connection, instance.Test));

				foreach (var child in instance.Children)
					element.Add (Write (connection, child));

				return element;
			}
		}

		class ConfigurationSerializer : Serializer<TestConfiguration>
		{
			public override TestConfiguration Read (Connection connection, XElement node)
			{
				return TestConfiguration.ReadFromXml (node);
			}

			public override XElement Write (Connection connection, TestConfiguration instance)
			{
				return instance.WriteToXml ();
			}
		}

		class StatisticsEventArgsSerializer : Serializer<TestStatistics.StatisticsEventArgs>
		{
			public override TestStatistics.StatisticsEventArgs Read (Connection connection, XElement node)
			{
				if (!node.Name.LocalName.Equals ("TestStatisticsEventArgs"))
					throw new InvalidOperationException ();

				var instance = new TestStatistics.StatisticsEventArgs ();
				instance.Type = (TestStatistics.EventType)Enum.Parse (typeof(TestStatistics.EventType), node.Attribute ("Type").Value);
				instance.Status = (TestStatus)Enum.Parse (typeof(TestStatus), node.Attribute ("Status").Value);
				instance.IsRemote = true;

				var name = node.Element ("TestName");
				if (name != null)
					instance.Name = Serializer.TestName.Read (connection, name);

				return instance;
			}

			public override XElement Write (Connection connection, TestStatistics.StatisticsEventArgs instance)
			{
				if (instance.IsRemote)
					throw new InvalidOperationException ();

				var element = new XElement ("TestStatisticsEventArgs");
				element.SetAttributeValue ("Type", instance.Type.ToString ());
				element.SetAttributeValue ("Status", instance.Status.ToString ());

				if (instance.Name != null)
					element.Add (Serializer.TestName.Write (connection, instance.Name));

				return element;
			}
		}

		class HandshakeSerializer : Serializer<Handshake>
		{
			public override Handshake Read (Connection connection, XElement node)
			{
				if (!node.Name.LocalName.Equals ("Handshake"))
					throw new InvalidOperationException ();

				var instance = new Handshake ();
				instance.WantStatisticsEvents = bool.Parse (node.Attribute ("WantStatisticsEvents").Value);

				var settings = node.Element ("Settings");
				if (settings != null)
					instance.Settings = Serializer.Settings.Read (connection, settings);

				var suite = node.Element ("TestSuite");
				if (suite != null)
					instance.TestSuite = Serializer.TestSuite.Read (connection, suite);

				return instance;
			}

			public override XElement Write (Connection connection, Handshake instance)
			{
				var element = new XElement ("Handshake");
				element.SetAttributeValue ("WantStatisticsEvents", instance.WantStatisticsEvents);

				if (instance.Settings != null)
					element.Add (Serializer.Settings.Write (connection, instance.Settings));

				if (instance.TestSuite != null)
					element.Add (Serializer.TestSuite.Write (connection, instance.TestSuite));

				return element;
			}
		}
	}

	abstract class Serializer<T>
	{
		public abstract T Read (Connection connection, XElement node);

		public abstract XElement Write (Connection connection, T instance);
	}
}

