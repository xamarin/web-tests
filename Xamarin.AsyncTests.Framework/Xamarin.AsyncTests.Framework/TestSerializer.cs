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
using System.Reflection;
using System.Collections.Generic;
using System.Xml.Linq;

namespace Xamarin.AsyncTests.Framework
{
	using Reflection;

	public static class TestSerializer
	{
		static readonly XName ParameterName = "TestParameter";
		static readonly XName PathName = "TestPath";

		internal const string TestCaseIdentifier = "test";
		internal const string FixtureInstanceIdentifier = "instance";
		internal const string TestFixtureIdentifier = "fixture";
		internal const string TestSuiteIdentifier = "suite";

		internal static void Debug (string message, params object[] args)
		{
			// System.Diagnostics.Debug.WriteLine (string.Format (message, args));
		}

		internal static XElement SerializePath (TestPath path)
		{
			var node = new XElement (PathName);

			while (path != null) {
				var element = WritePathNode (path.Host, path.Parameter);
				node.AddFirst (element);
				path = path.Parent;
			}

			return node;
		}

		internal static TestPathNode DeserializePath (TestSuite suite, TestContext ctx, XElement root)
		{
			if (!root.Name.Equals (PathName))
				throw new InternalErrorException ();

			var resolver = (IPathResolver)suite;

			foreach (var element in root.Elements (ParameterName)) {
				var node = ReadPathNode (element);
				var parameterAttr = element.Attribute ("Parameter");
				var parameter = parameterAttr != null ? parameterAttr.Value : null;

				resolver = resolver.Resolve (ctx, node, parameter);
			}

			return resolver.Node;
		}

		public static string GetFriendlyName (Type type)
		{
			if (type == null)
				return null;
			var friendlyAttr = type.GetTypeInfo ().GetCustomAttribute<FriendlyNameAttribute> ();
			if (friendlyAttr != null)
				return friendlyAttr.Name;
			return type.FullName;
		}

		public static ITestParameter GetStringParameter (string value)
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

		static IPathNode ReadPathNode (XElement element)
		{
			var node = new PathNodeWrapper ();
			node.Identifier = element.Attribute ("Identifier").Value;

			var nameAttr = element.Attribute ("Name");
			if (nameAttr != null)
				node.Name = nameAttr.Value;

			var paramTypeAttr = element.Attribute ("ParameterType");
			if (paramTypeAttr != null)
				node.ParameterType = paramTypeAttr.Value;

			var paramValueAttr = element.Attribute ("Parameter");
			if (paramValueAttr != null)
				node.ParameterValue = paramValueAttr.Value;

			return node;
		}

		static XElement WritePathNode (IPathNode node, ITestParameter parameter)
		{
			var element = new XElement (ParameterName);
			element.Add (new XAttribute ("Identifier", node.Identifier));
			if (node.Name != null)
				element.Add (new XAttribute ("Name", node.Name));
			if (node.ParameterType != null)
				element.Add (new XAttribute ("ParameterType", node.ParameterType));
			if (parameter != null)
				element.Add (new XAttribute ("Parameter", parameter.Value));
			return element;
		}

		class PathNodeWrapper : IPathNode
		{
			public string Identifier {
				get; set;
			}
			public string Name {
				get; set;
			}
			public string ParameterType {
				get; set;
			}
			public string ParameterValue {
				get; set;
			}

			public override string ToString ()
			{
				string parameter = ParameterValue != null ? string.Format (", Parameter={0}", ParameterValue) : string.Empty;
				return string.Format ("[TestPathNode: Identifier={0}, Name={1}{2}]", Identifier, Name, parameter);
			}
		}

		class PathWrapper : ITestPath
		{
			readonly XElement node;

			public PathWrapper (XElement node)
			{
				this.node = node;
				if (node == null)
					throw new InvalidOperationException();
			}

			public XElement SerializePath ()
			{
				return node;
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

		public static TestName ReadTestName (XElement node)
		{
			if (!node.Name.LocalName.Equals ("TestName"))
				throw new InternalErrorException ();

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

		public static XElement WriteTestName (TestName instance)
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

			var name = TestSerializer.ReadTestName (node.Element ("TestName"));
			var status = (TestStatus)Enum.Parse (typeof(TestStatus), node.Attribute ("Status").Value);

			var result = new TestResult (name, status);

			var path = node.Element ("TestPath");
			if (path != null)
				result.Path = new PathWrapper (path);

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

			element.Add (TestSerializer.WriteTestName (instance.Name));

			if (instance.Path != null) {
				element.Add (instance.Path.SerializePath ());
			}

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

			var name = node.Element ("TestName");
			if (name != null)
				instance.Name = TestSerializer.ReadTestName (name);

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
				element.Add (TestSerializer.WriteTestName (instance.Name));

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

