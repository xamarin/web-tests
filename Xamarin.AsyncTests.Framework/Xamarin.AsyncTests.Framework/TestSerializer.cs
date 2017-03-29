//
// TestSerializer.cs
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
using System.Linq;
using System.Text;
using System.Reflection;
using System.Collections.Generic;
using System.Xml.Linq;

namespace Xamarin.AsyncTests.Framework
{
	public static class TestSerializer
	{
		internal static void Debug (string message, params object [] args)
		{
			System.Diagnostics.Debug.WriteLine (string.Format (message, args));
		}

		internal static TestPathTreeNode DeserializePath (TestSuite suite, TestContext ctx, XElement root)
		{
			var resolver = (IPathResolver)suite;

			var path = TestPath.Read (root);
			foreach (var node in path.GetNodes ()) {
				if (node.PathType == TestPathType.Group)
					continue;
				resolver = resolver.Resolve (ctx, node);
			}

			return resolver.Node;
		}

		internal static ITestParameter GetStringParameter (string value)
		{
			return new ParameterWrapper { Value = value };
		}

		class ParameterWrapper : ITestParameter
		{
			public string Value {
				get; set;
			}

			public override string ToString ()
			{
				return string.Format ("[ParameterWrapper: {0}]", Value);
			}
		}

		public static SettingsBag ReadSettings (XElement node)
		{
			if (!node.Name.LocalName.Equals ("Settings"))
				throw new InternalErrorException ();

			var settings = SettingsBag.CreateDefault ();
			foreach (var element in node.Elements ("Setting")) {
				var key = element.Attribute ("Key");
				var value = element.Attribute ("Value");
				settings.Add (key.Value, value.Value);
			}
			return settings;
		}

		public static XElement WriteSettings (SettingsBag instance)
		{
			var node = new XElement ("Settings");

			foreach (var entry in instance.Settings) {
				var item = new XElement ("Setting");
				node.Add (item);

				item.SetAttributeValue ("Key", entry.Key);
				item.SetAttributeValue ("Value", entry.Value);
			}

			return node;
		}

		public static XElement WriteError (Exception error)
		{
			var element = new XElement ("Error");
			element.SetAttributeValue ("Type", error.GetType ().FullName);
			element.SetAttributeValue ("Message", error.Message);
			if (error.StackTrace != null)
				element.SetAttributeValue ("StackTrace", error.StackTrace);
			return element;
		}

		public static Exception ReadError (XElement node)
		{
			if (!node.Name.LocalName.Equals ("Error"))
				throw new InternalErrorException ();

			var type = node.Attribute ("Type").Value;
			var message = node.Attribute ("Message").Value;
			var stackAttr = node.Attribute ("StackTrace");
			var stackTrace = stackAttr != null ? stackAttr.Value : null;
			return new SavedException (type, message, stackTrace);
		}

		public static TestResult ReadTestResult (XElement node)
		{
			if (!node.Name.LocalName.Equals ("TestResult"))
				throw new InternalErrorException ();

			var status = (TestStatus)Enum.Parse (typeof(TestStatus), node.Attribute ("Status").Value);

			var path = TestPath.Read (node.Element ("TestPath"));

			var result = new TestResult (path, status);

			var elapsedTime = node.Element ("ElapsedTime");
			if (elapsedTime != null)
				result.ElapsedTime = TimeSpan.FromMilliseconds (int.Parse (elapsedTime.Value));

			foreach (var error in node.Elements ("Error")) {
				result.AddError (ReadError (error));
			}

			foreach (var message in node.Elements ("Message")) {
				result.AddMessage (message.Attribute ("Text").Value);
			}

			foreach (var log in node.Elements ("LogEntry")) {
				result.AddLogMessage (ReadLogEntry (log));
			}

			foreach (var child in node.Elements ("TestResult")) {
				result.AddChild (ReadTestResult (child));
			}

			return result;
		}

		public static XElement WriteTestResult (TestResult instance)
		{
			var element = new XElement ("TestResult");
			element.SetAttributeValue ("Status", instance.Status.ToString ());

			if (!string.IsNullOrEmpty (instance.Path.FullName))
				element.SetAttributeValue ("Name", instance.Path.FullName);

			if (instance.Path != null)
				element.Add (instance.Path.SerializePath ());

			if (instance.ElapsedTime != null)
				element.SetAttributeValue ("ElapsedTime", (int)instance.ElapsedTime.Value.TotalMilliseconds);

			foreach (var error in instance.Errors) {
				element.Add (WriteError (error));
			}

			if (instance.HasMessages) {
				foreach (var message in instance.Messages) {
					var msgElement = new XElement ("Message");
					msgElement.SetAttributeValue ("Text", message);
					element.Add (msgElement);
				}
			}

			if (instance.HasLogEntries) {
				foreach (var entry in instance.LogEntries) {
					element.Add (WriteLogEntry (entry));

				}
			}

			foreach (var child in instance.Children)
				element.Add (WriteTestResult (child));

			return element;
		}

		public static TestLoggerBackend.StatisticsEventArgs ReadStatisticsEvent (XElement node)
		{
			if (!node.Name.LocalName.Equals ("TestStatisticsEventArgs"))
				throw new InternalErrorException ();

			var instance = new TestLoggerBackend.StatisticsEventArgs ();
			instance.Type = (TestLoggerBackend.StatisticsEventType)Enum.Parse (typeof(TestLoggerBackend.StatisticsEventType), node.Attribute ("Type").Value);
			instance.Status = (TestStatus)Enum.Parse (typeof(TestStatus), node.Attribute ("Status").Value);
			instance.IsRemote = true;

			var name = node.Attribute ("Name");
			if (name != null)
				instance.Name = name.Value;

			return instance;
		}

		public static XElement WriteStatisticsEvent (TestLoggerBackend.StatisticsEventArgs instance)
		{
			if (instance.IsRemote)
				throw new InternalErrorException ();

			var element = new XElement ("TestStatisticsEventArgs");
			element.SetAttributeValue ("Type", instance.Type.ToString ());
			element.SetAttributeValue ("Status", instance.Status.ToString ());

			if (instance.Name != null)
				element.SetAttributeValue ("Name", instance.Name);

			return element;
		}

		public static TestLoggerBackend.LogEntry ReadLogEntry (XElement node)
		{
			if (!node.Name.LocalName.Equals ("LogEntry"))
				throw new InternalErrorException ();

			TestLoggerBackend.EntryKind kind;
			if (!Enum.TryParse (node.Attribute ("Kind").Value, out kind))
				throw new InternalErrorException ();

			var text = node.Attribute ("Text").Value;
			var logLevel = int.Parse (node.Attribute ("LogLevel").Value);

			Exception exception = null;
			var error = node.Element ("Error");
			if (error != null) {
				var errorMessage = error.Attribute ("Text").Value;
				var stackTrace = error.Attribute ("StackTrace");
				exception = new SavedException (errorMessage, stackTrace != null ? stackTrace.Value : null);
			}

			return new TestLoggerBackend.LogEntry (kind, logLevel, text, exception);
		}

		public static XElement WriteLogEntry (TestLoggerBackend.LogEntry instance)
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

		public static string ReadString (XElement node)
		{
			if (!node.Name.LocalName.Equals ("Text"))
				throw new InternalErrorException ();

			return node.Attribute ("Value").Value;
		}

		public static XElement WriteString (string instance)
		{
			var element = new XElement ("Text");
			element.SetAttributeValue ("Value", instance);
			return element;
		}

		public static XElement ReadElement (XElement node)
		{
			if (!node.Name.LocalName.Equals ("Element"))
				throw new InternalErrorException ();

			return (XElement)node.FirstNode;
		}

		public static XElement WriteElement (XElement instance)
		{
			return new XElement ("Element", instance);
		}

		static XElement WriteTestFeature (TestFeature instance)
		{
			var element = new XElement ("TestFeature");
			element.SetAttributeValue ("Name", instance.Name);
			if (instance.Description != null)
				element.SetAttributeValue ("Description", instance.Description);
			if (instance.DefaultValue != null)
				element.SetAttributeValue ("DefaultValue", instance.DefaultValue);
			if (instance.Constant != null)
				element.SetAttributeValue ("Constant", instance.Constant);
			return element;
		}

		static bool? GetNullableBool (XElement node, string name)
		{
			var attr = node.Attribute (name);
			if (attr == null)
				return null;
			return bool.Parse (attr.Value);
		}

		static TestFeature ReadTestFeature (XElement node)
		{
			if (!node.Name.LocalName.Equals ("TestFeature"))
				throw new InternalErrorException ();

			var name = node.Attribute ("Name");
			var descriptionAttr = node.Attribute ("Description");
			var description = descriptionAttr != null ? descriptionAttr.Value : null;

			var defaultValue = GetNullableBool (node, "DefaultValue");
			var constant = GetNullableBool (node, "Constant");

			return new TestFeature (name.Value, description, defaultValue, constant);
		}

		static XElement WriteTestCategory (TestCategory instance)
		{
			var element = new XElement ("TestCategory");
			element.SetAttributeValue ("Name", instance.Name);
			element.SetAttributeValue ("IsBuiltin", instance.IsBuiltin);
			element.SetAttributeValue ("IsExplicit", instance.IsExplicit);
			return element;
		}

		static TestCategory ReadTestCategory (XElement node)
		{
			if (!node.Name.LocalName.Equals ("TestCategory"))
				throw new InternalErrorException ();

			var name = node.Attribute ("Name");
			return new TestCategory (name.Value);
		}

		public static XElement WriteConfiguration (ITestConfigurationProvider configuration)
		{
			var element = new XElement ("TestConfiguration");
			element.SetAttributeValue ("Name", configuration.Name);

			foreach (var feature in configuration.Features) {
				element.Add (WriteTestFeature (feature));
			}

			foreach (var category in configuration.Categories) {
				element.Add (WriteTestCategory (category));
			}

			return element;
		}

		public static ITestConfigurationProvider ReadConfiguration (XElement node)
		{
			if (!node.Name.LocalName.Equals ("TestConfiguration"))
				throw new InternalErrorException ();

			var name = node.Attribute ("Name").Value;

			var config = new ConfigurationWrapper (name);

			foreach (var feature in node.Elements ("TestFeature")) {
				config.Add (ReadTestFeature (feature));
			}

			foreach (var category in node.Elements ("TestCategory")) {
				config.Add (ReadTestCategory (category));
			}

			return config;
		}

		class ConfigurationWrapper : ITestConfigurationProvider
		{
			List<TestFeature> features;
			List<TestCategory> categories;

			public void Add (TestFeature feature)
			{
				features.Add (feature);
			}

			public void Add (TestCategory category)
			{
				categories.Add (category);
			}

			public ConfigurationWrapper (string name)
			{
				Name = name;
				features = new List<TestFeature> ();
				categories = new List<TestCategory> ();
			}

			public string Name {
				get;
				private set;
			}

			public IEnumerable<TestFeature> Features {
				get { return features; }
			}

			public IEnumerable<TestCategory> Categories {
				get { return categories; }
			}
		}
	}
}

