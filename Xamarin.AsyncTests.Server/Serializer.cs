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
		public static readonly Serializer<long> ObjectID = new ObjectIDSerializer ();
		public static readonly Serializer<TestName> TestName = new TestNameSerializer ();
		public static readonly Serializer<TestSuite> TestSuite = new TestSuiteSerializer ();
		public static readonly Serializer<TestCase> TestCase = new TestCaseSerializer ();
		public static readonly Serializer<TestResult> TestResult = new TestResultSerializer ();
		public static readonly Serializer<Handshake> Handshake = new HandshakeSerializer ();
		public static readonly Serializer<TestLoggerBackend.LogEntry> LogEntry = new LogEntrySerializer ();
		public static readonly Serializer<TestLoggerBackend.StatisticsEventArgs> StatisticsEventArgs = new StatisticsEventArgsSerializer ();

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
					throw new ServerErrorException ();

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
					throw new ServerErrorException ();

				return node.Attribute ("Value").Value;
			}

			public override XElement Write (Connection connection, string instance)
			{
				var element = new XElement ("Text");
				element.SetAttributeValue ("Value", instance);
				return element;
			}
		}

		class ObjectIDSerializer : Serializer<long>
		{
			public override long Read (Connection connection, XElement node)
			{
				if (!node.Name.LocalName.Equals ("ObjectID"))
					throw new ServerErrorException ();

				return long.Parse (node.Attribute ("Value").Value);
			}

			public override XElement Write (Connection connection, long instance)
			{
				var element = new XElement ("ObjectID");
				element.SetAttributeValue ("Value", instance);
				return element;
			}
		}

		class TestNameSerializer : Serializer<TestName>
		{
			public override TestName Read (Connection connection, XElement node)
			{
				if (!node.Name.LocalName.Equals ("TestName"))
					throw new ServerErrorException ();

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
					throw new ServerErrorException ();

				var objectId = long.Parse (node.Attribute ("ObjectID").Value);

				TestSuite suite;
				if (connection.TryGetRemoteObject<TestSuite> (objectId, out suite))
					return suite;

				var name = Serializer.TestName.Read (connection, node.Element ("TestName"));

				return new RemoteTestSuite (connection, objectId, name);
			}

			public override XElement Write (Connection connection, TestSuite instance)
			{
				var element = new XElement ("TestSuite");
				var objectId = connection.RegisterRemoteObject (instance);
				element.SetAttributeValue ("ObjectID", objectId);

				var name = Serializer.TestName.Write (connection, instance.Name);
				element.Add (name);

				return element;
			}
		}

		class TestCaseSerializer : Serializer<TestCase>
		{
			public override TestCase Read (Connection connection, XElement node)
			{
				if (!node.Name.LocalName.Equals ("TestCase"))
					throw new ServerErrorException ();

				var objectId = long.Parse (node.Attribute ("ObjectID").Value);

				TestCase test;
				if (connection.TryGetRemoteObject<TestCase> (objectId, out test))
					return test;

				var name = Serializer.TestName.Read (connection, node.Element ("TestName"));

				return new RemoteTestCase (null, name, connection, objectId);
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
					throw new ServerErrorException ();

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
							throw new ServerErrorException ();
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

		class StatisticsEventArgsSerializer : Serializer<TestLoggerBackend.StatisticsEventArgs>
		{
			public override TestLoggerBackend.StatisticsEventArgs Read (Connection connection, XElement node)
			{
				if (!node.Name.LocalName.Equals ("TestStatisticsEventArgs"))
					throw new ServerErrorException ();

				var instance = new TestLoggerBackend.StatisticsEventArgs ();
				instance.Type = (TestLoggerBackend.StatisticsEventType)Enum.Parse (typeof(TestLoggerBackend.StatisticsEventType), node.Attribute ("Type").Value);
				instance.Status = (TestStatus)Enum.Parse (typeof(TestStatus), node.Attribute ("Status").Value);
				instance.IsRemote = true;

				var name = node.Element ("TestName");
				if (name != null)
					instance.Name = Serializer.TestName.Read (connection, name);

				return instance;
			}

			public override XElement Write (Connection connection, TestLoggerBackend.StatisticsEventArgs instance)
			{
				if (instance.IsRemote)
					throw new ServerErrorException ();

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
					throw new ServerErrorException ();

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

		class LogEntrySerializer : Serializer<TestLoggerBackend.LogEntry>
		{
			#region implemented abstract members of Serializer
			public override TestLoggerBackend.LogEntry Read (Connection connection, XElement node)
			{
				if (!node.Name.LocalName.Equals ("LogEntry"))
					throw new ServerErrorException ();

				TestLoggerBackend.EntryKind kind;
				if (!Enum.TryParse (node.Attribute ("Kind").Value, out kind))
					throw new ServerErrorException ();

				var text = node.Attribute ("Text").Value;
				var logLevel = int.Parse (node.Attribute ("LogLevel").Value);

				Exception exception = null;
				var error = node.Element ("Error");
				if (error != null) {
					var errorMessage = error.Attribute ("Text").Value;
					var stackTrace = error.Attribute ("StackTrace").Value;
					exception = new SavedException (errorMessage, stackTrace);
				}

				var instance = new TestLoggerBackend.LogEntry (kind, logLevel, text, exception);
				return instance;
			}
			public override XElement Write (Connection connection, TestLoggerBackend.LogEntry instance)
			{
				var element = new XElement ("LogEntry");
				element.SetAttributeValue ("Kind", instance.Kind);
				element.SetAttributeValue ("LogLevel", instance.LogLevel);
				element.SetAttributeValue ("Text", instance.Text);

				if (instance.Error != null) {
					var error = new XElement ("Error");
					error.SetAttributeValue ("Text", instance.Error.Message);
					error.SetAttributeValue ("StackTrace", instance.Error.StackTrace);
					element.Add (error);
				}

				return element;
			}
			#endregion
		}
	}

	abstract class Serializer<T>
	{
		public abstract T Read (Connection connection, XElement node);

		public abstract XElement Write (Connection connection, T instance);
	}
}

