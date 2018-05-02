﻿//
// ReflectionHelper.cs
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
using System.Xml.Linq;
using System.Text;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace Xamarin.AsyncTests.Framework.Reflection
{
	static class ReflectionHelper
	{
		#region Type Info

		public static string GetMethodSignatureFullName (MethodInfo method)
		{
			var sb = new StringBuilder ();
			sb.Append (method.Name);
			sb.Append ("(");
			var parameters = method.GetParameters ();
			for (int i = 0; i < parameters.Length; i++) {
				if (i > 0)
					sb.Append (",");
				sb.Append (parameters [i].ParameterType.Name);
			}
			sb.Append (")");
			return sb.ToString ();
		}

		public static string GetTypeFullName (TypeInfo type)
		{
			if (!type.IsGenericType)
				return type.FullName;
			var tdecl = type.GetGenericTypeDefinition ();
			var targs = type.GenericTypeArguments;
			var sb = new StringBuilder ();
			sb.Append (tdecl.FullName);
			sb.Append ("<");
			for (int i = 0; i < targs.Length; i++) {
				if (i > 0)
					sb.Append (",");
				sb.Append (GetTypeFullName (targs [i].GetTypeInfo ()));
			}
			sb.Append (">");
			return sb.ToString ();
		}

		public static IMemberInfo GetMethodInfo (MethodInfo member)
		{
			return new _MethodInfo (member);
		}

		public static IMemberInfo GetTypeInfo (TypeInfo member)
		{
			return new _TypeInfo (member);
		}

		public static IMemberInfo GetParameterInfo (ParameterInfo member)
		{
			return new _ParameterInfo (member);
		}

		class _ParameterInfo : IMemberInfo
		{
			public readonly ParameterInfo Member;

			public _ParameterInfo (ParameterInfo member)
			{
				Member = member;
				Type = member.ParameterType;
				TypeInfo = Type.GetTypeInfo ();
			}

			public string Name {
				get { return Member.Name; }
			}

			public Type Type {
				get;
			}

			public TypeInfo TypeInfo {
				get;
			}

			public T GetCustomAttribute<T> () where T : Attribute
			{
				return Member.GetCustomAttribute<T> ();
			}

			public IEnumerable<T> GetCustomAttributes<T> () where T : Attribute
			{
				return Member.GetCustomAttributes<T> ();
			}
		}

		class _PropertyInfo : IMemberInfo
		{
			public readonly PropertyInfo Member;

			public _PropertyInfo (PropertyInfo member)
			{
				Member = member;
				Type = member.PropertyType;
				TypeInfo = Type.GetTypeInfo ();
			}

			public string Name {
				get { return Member.Name; }
			}

			public Type Type {
				get;
			}

			public TypeInfo TypeInfo {
				get;
			}

			public T GetCustomAttribute<T> () where T : Attribute
			{
				return Member.GetCustomAttribute<T> ();
			}

			public IEnumerable<T> GetCustomAttributes<T> () where T : Attribute
			{
				return Member.GetCustomAttributes<T> ();
			}

			public override string ToString ()
			{
				return $"[PropertyInfo: {Type.Name}.{Name}]";
			}
		}

		class _MethodInfo : IMemberInfo
		{
			public readonly MethodInfo Member;

			public _MethodInfo (MethodInfo member)
			{
				Member = member;
				Type = member.ReturnType;
				TypeInfo = Type.GetTypeInfo ();
			}

			public string Name {
				get { return Member.Name; }
			}

			public Type Type {
				get;
			}

			public TypeInfo TypeInfo {
				get;
			}

			public T GetCustomAttribute<T> () where T : Attribute
			{
				return Member.GetCustomAttribute<T> ();
			}

			public IEnumerable<T> GetCustomAttributes<T> () where T : Attribute
			{
				return Member.GetCustomAttributes<T> ();
			}

			public override string ToString ()
			{
				return $"[MethodInfo: {Type.Name}.{Name}]";
			}
		}

		class _TypeInfo : IMemberInfo {
			public readonly TypeInfo Member;

			public _TypeInfo (TypeInfo member)
			{
				Member = member;
				Type = Member.AsType ();
			}

			public string Name {
				get { return Member.Name; }
			}

			public Type Type {
				get;
			}

			public TypeInfo TypeInfo {
				get { return Member; }
			}

			public T GetCustomAttribute<T> () where T : Attribute
			{
				return Member.GetCustomAttribute<T> ();
			}

			public IEnumerable<T> GetCustomAttributes<T> () where T : Attribute
			{
				return Member.GetCustomAttributes<T> ();
			}

			public override string ToString ()
			{
				return $"[TypeInfo: {Name}]";
			}
		}

		#endregion

		#region Test Hosts

		internal static TestHost ResolveFixtureProperty (
			TypeInfo fixtureType, PropertyInfo property, bool instance)
		{
			if (!property.CanRead)
				return null;

			var isStatic = property.GetMethod.IsStatic;
			if (instance && isStatic)
				return null;
			if (!instance && !isStatic)
				return null;

			if (property.PropertyType.Equals (typeof (IForkedObjectClient)))
				return null;

			var member = new _PropertyInfo (property);
			var fixtureParamAttr = property.GetCustomAttribute<FixtureParameterAttribute> ();
			if (fixtureParamAttr != null) {
				IParameterSerializer serializer;
				if (!GetParameterSerializer (member.TypeInfo, out serializer))
					throw new InternalErrorException ();

				return new FixturePropertyHost (
					property, serializer, TestFlags.Browsable, isStatic);
			}

			if (!property.CanWrite || !property.GetMethod.IsPublic ||
			    !property.SetMethod.IsPublic)
				return null;

			var host = ResolveFixtureParameter (fixtureType, member);
			if (host == null)
				throw new InternalErrorException ();

			return new ReflectionPropertyHost (property, host);
		}

		static T GetCustomAttributeForType<T> (TypeInfo type)
			where T : Attribute
		{
			var attrs = type.GetCustomAttributes<T> (false).ToArray ();
			if (attrs.Length == 1)
				return attrs [0];
			else if (attrs.Length > 1)
				throw new InvalidOperationException ();

			attrs = type.GetCustomAttributes<T> (true).ToArray ();
			if (attrs.Length == 1)
				return attrs [0];
			else if (attrs.Length > 1)
				throw new InvalidOperationException ();

			return null;
		}

		static ParameterizedTestHost ResolveFixtureParameter (
			TypeInfo fixtureType, IMemberInfo member)
		{
			var paramAttrs = member.GetCustomAttributes<TestParameterAttribute> ().ToArray ();
			if (paramAttrs.Length == 1)
				return CreateParameterAttributeHost (member, paramAttrs[0]);
			if (paramAttrs.Length > 1) 
				throw new InternalErrorException ();

			var typeAttr = GetCustomAttributeForType<TestParameterAttribute> (member.TypeInfo);
			if (typeAttr != null)
				return CreateParameterAttributeHost (member, typeAttr);

			if (member.Type.Equals (typeof (bool)))
				return CreateBoolean (member);

			if (member.TypeInfo.IsEnum)
				return CreateEnum (member);

			return null;
		}

		internal static TestHost ResolveParameter (
			TypeInfo fixtureType, IMemberInfo member,
			string parameterFilter)
		{
			if (typeof (ITestInstance).GetTypeInfo ().IsAssignableFrom (member.TypeInfo)) {
				var hostAttr = member.GetCustomAttribute<TestHostAttribute> ();
				if (hostAttr == null)
					hostAttr = member.TypeInfo.GetCustomAttribute<TestHostAttribute> ();
				if (hostAttr == null)
					throw new InternalErrorException ();

				var hostType = GetCustomHostType (
					fixtureType, member, hostAttr,
					out var staticHost, out var useFixtureInstance);

				return new CustomTestHost (
					hostAttr.Identifier ?? member.Name, member.Type,
					hostType, hostAttr.TestFlags, hostAttr,
					staticHost, useFixtureInstance);
			}

			var host = ResolveFixtureParameter (fixtureType, member);
			if (host != null)
				return host;

			var parameterSourceType = typeof (ITestParameterSource<>).MakeGenericType (member.Type).GetTypeInfo ();
			if (parameterSourceType.IsAssignableFrom (fixtureType)) {
				if (!GetParameterSerializer (member.TypeInfo, out var serializer))
					throw new InternalErrorException ();

				var type = typeof (ParameterSourceHost<>).MakeGenericType (member.Type);
				return (ParameterizedTestHost)Activator.CreateInstance (
					type, member.Name, null, serializer,
					parameterFilter, true, TestFlags.None);
			}

			throw new InternalErrorException ();
		}

		static Type GetCustomHostType (
			TypeInfo fixtureType, IMemberInfo member, TestHostAttribute attr,
			out ITestHost<ITestInstance> staticHost, out bool useFixtureInstance)
		{
			var hostType = typeof(ITestHost<>).GetTypeInfo ();
			var genericInstance = hostType.MakeGenericType (member.Type).GetTypeInfo ();

			if (attr.HostType != null) {
				if (!genericInstance.IsAssignableFrom (attr.HostType.GetTypeInfo ()))
					throw new InternalErrorException ();
				useFixtureInstance = genericInstance.IsAssignableFrom (fixtureType);
				staticHost = attr as ITestHost<ITestInstance>;
				return attr.HostType;
			}

			if (genericInstance.IsAssignableFrom (fixtureType)) {
				useFixtureInstance = true;
				staticHost = null;
				return null;
			}

			var attrType = attr.GetType ();
			if (genericInstance.IsAssignableFrom (attrType.GetTypeInfo ())) {
				useFixtureInstance = false;
				staticHost = (ITestHost<ITestInstance>)attr;
				return attrType;
			}

			throw new InternalErrorException ();
		}

		static void Debug (string format, params object[] args)
		{
			System.Diagnostics.Debug.WriteLine (string.Format (format, args));
		}

		static Type GetParameterHostType (IMemberInfo member, TestParameterAttribute attr, out object sourceInstance)
		{
			var hostType = typeof(ITestParameterSource<>).GetTypeInfo ();
			var genericInstance = hostType.MakeGenericType (member.Type).GetTypeInfo ();

			var attrType = attr.GetType ();

			if (genericInstance.IsAssignableFrom (attrType.GetTypeInfo ())) {
				sourceInstance = attr;
				return attrType;
			}

			if (member.Type.Equals (typeof(bool))) {
				sourceInstance = null;
				return typeof(BooleanTestSource);
			} else if (member.TypeInfo.IsEnum) {
				sourceInstance = null;
				return typeof(EnumParameterSource<>).MakeGenericType (member.Type);
			}

			throw new InternalErrorException ();
		}

		static ParameterizedTestHost CreateParameterAttributeHost (
			IMemberInfo member, TestParameterAttribute attr)
		{
			var sourceType = GetParameterHostType (member, attr, out object sourceInstance);

			if (!GetParameterSerializer (member.TypeInfo, out IParameterSerializer serializer))
				throw new InternalErrorException ();

			if (sourceInstance == null)
				sourceInstance = Activator.CreateInstance (sourceType);

			var type = typeof (ParameterSourceHost<>).MakeGenericType (member.Type);
			return (ParameterizedTestHost)Activator.CreateInstance (
				type, attr.Identifier ?? member.Name, sourceInstance, serializer, attr.Filter, false, attr.Flags);
		}

		internal static TestHost CreateFixedParameterHost (FixedTestParameterAttribute attr)
		{
			var typeInfo = attr.Type.GetTypeInfo ();

			IParameterSerializer serializer;
			if (!GetParameterSerializer (typeInfo, out serializer))
				throw new InternalErrorException ();

			var hostType = typeof(FixedParameterHost<>).MakeGenericType (attr.Type);
			return (TestHost)Activator.CreateInstance (
				hostType, attr.Identifier, typeInfo, serializer, attr, attr.TestFlags);
		}

		static ParameterizedTestHost CreateEnum (IMemberInfo member)
		{
			if (!member.TypeInfo.IsEnum || member.TypeInfo.GetCustomAttribute<FlagsAttribute> () != null)
				throw new InternalErrorException ();

			var type = member.Type;
			var sourceType = typeof(EnumParameterSource<>).MakeGenericType (type);
			var hostType = typeof(ParameterSourceHost<>).MakeGenericType (type);

			var serializerType = typeof(EnumSerializer<>).MakeGenericType (type);
			var serializer = (IParameterSerializer)Activator.CreateInstance (serializerType);

			var source = Activator.CreateInstance (sourceType);
			return (ParameterizedTestHost)Activator.CreateInstance (
				hostType, member.Name, source, serializer, null, false,
				TestFlags.ContinueOnError | TestFlags.Browsable);
		}

		static ParameterizedTestHost CreateBoolean (IMemberInfo member)
		{
			return new ParameterSourceHost<bool> (
				member.Name, new BooleanTestSource (),
				GetBooleanSerializer (), null, false, TestFlags.None);
		}

		class BooleanTestSource : ITestParameterSource<bool>
		{
			public IEnumerable<bool> GetParameters (TestContext ctx, string filter)
			{
				yield return false;
				yield return true;
			}
		}

		static bool GetParameterSerializer (TypeInfo type, out IParameterSerializer serializer)
		{
			if (typeof(ITestParameter).GetTypeInfo ().IsAssignableFrom (type)) {
				serializer = null;
				return true;
			}

			if (type.Equals (typeof(bool))) {
				serializer = new BooleanSerializer ();
				return true;
			} else if (type.Equals (typeof(int))) {
				serializer = new IntegerSerializer ();
				return true;
			} else if (type.Equals (typeof(string))) {
				serializer = new StringSerializer ();
				return true;
			} else if (type.IsEnum) {
				var serializerType = typeof(EnumSerializer<>).MakeGenericType (type.AsType ());
				serializer = (IParameterSerializer)Activator.CreateInstance (serializerType);
				return true;
			}

			serializer = null;
			return false;
		}

		internal static IParameterSerializer GetBooleanSerializer ()
		{
			return new BooleanSerializer ();
		}

		internal static IParameterSerializer GetIntegerSerializer ()
		{
			return new IntegerSerializer ();
		}

		static string FormatEnum (Type type, object enumValue)
		{
			var underlyingType = Enum.GetUnderlyingType (type);
			if (underlyingType != typeof (int))
				return enumValue.ToString ();

			var intValue = (int)enumValue;
			var values = (int[])Enum.GetValues (type);
			var names = Enum.GetNames (type);

			for (int i = 0; i < values.Length; i++) {
				if (intValue == values[i])
					return names[i];
			}

			var list = new List<string> ();
			for (int i = 0; i < values.Length; i++) {
				if (values[i] == 0)
					continue;
				if ((intValue & values[i]) == values[i])
					list.Add (names[i]);
			}

			return string.Join ("|", list);
		}

		class ParameterWrapper : ITestParameter, ITestParameterWrapper
		{
			public object Value {
				get;
			}

			string StringValue {
				get;
			}

			public string FriendlyValue {
				get;
			}

			string ITestParameter.Value => StringValue;

			public ParameterWrapper (object value, string stringValue, string friendlyValue)
			{
				Value = value;
				StringValue = stringValue;
				FriendlyValue = friendlyValue;
			}
		}

		abstract class ParameterSerializer<T> : IParameterSerializer
		{
			public abstract ITestParameter ObjectToParameter (object value);

			public object ParameterToObject (ITestParameter value)
			{
				return ((ParameterWrapper)value).Value;
			}

			protected abstract string Serialize (T value);

			protected abstract T Deserialize (string value);

			public bool Serialize (XElement node, object value)
			{
				var serialized = Serialize ((T)value);
				node.Add (new XAttribute ("Parameter", serialized));
				return true;
			}

			public object Deserialize (XElement node)
			{
				if (node == null)
					throw new InternalErrorException ();
				var value = node.Attribute ("Parameter").Value;
				return Deserialize (value);
			}
		}

		class BooleanSerializer : ParameterSerializer<bool>
		{
			public override ITestParameter ObjectToParameter (object value)
			{
				return new ParameterWrapper (value, value.ToString (), value.ToString ());
			}
			protected override string Serialize (bool value)
			{
				return value.ToString ();
			}
			protected override bool Deserialize (string value)
			{
				return bool.Parse (value);
			}
		}

		class IntegerSerializer : ParameterSerializer<int>
		{
			public override ITestParameter ObjectToParameter (object value)
			{
				return new ParameterWrapper (value, value.ToString (), value.ToString ());
			}
			protected override string Serialize (int value)
			{
				return value.ToString ();
			}
			protected override int Deserialize (string value)
			{
				return int.Parse (value);
			}
		}

		class StringSerializer : ParameterSerializer<string>
		{
			public override ITestParameter ObjectToParameter (object value)
			{
				var stringValue = (string)value;
				var friendlyValue = '"' + TestSerializer.Escape (stringValue) + '"';
				return new ParameterWrapper (value, stringValue, friendlyValue);
			}
			protected override string Serialize (string value)
			{
				return value;
			}
			protected override string Deserialize (string value)
			{
				return value;
			}
		}

		class EnumSerializer<T> : ParameterSerializer<T>
			where T : struct
		{
			public override ITestParameter ObjectToParameter (object value)
			{
				var friendlyName = FormatEnum (typeof (T), value);
				return new ParameterWrapper (value, value.ToString (), friendlyName);
			}
			protected override string Serialize (T value)
			{
				return Enum.GetName (typeof(T), value);
			}
			protected override T Deserialize (string value)
			{
				T result;
				if (!Enum.TryParse (value, out result))
					throw new InternalErrorException ();
				return result;
			}
		}

		#endregion

		#region Misc

		static IReadOnlyCollection<TestCategoryAttribute> GetCategories (IMemberInfo member)
		{
			return member.GetCustomAttributes<TestCategoryAttribute> ().ToList ();
		}

		static IEnumerable<TestFeature> GetFeatures (IMemberInfo member)
		{
			foreach (var cattr in member.GetCustomAttributes<TestFeatureAttribute> ())
				yield return cattr.Feature;
		}

		internal static TestFilter CreateTestFilter (TestBuilder builder, TestFilter parent, bool mustMatch, IMemberInfo member)
		{
			var categories = GetCategories (member);
			var features = GetFeatures (member).ToList ();

			return new TestFilter (builder, parent, mustMatch, categories, features);
		}

		internal static TestHost CreateRepeatHost (int repeat)
		{
			return new RepeatedTestHost (repeat, TestFlags.None);
		}

		internal static bool IsFixedTestHost (TestHost host)
		{
			var typeInfo = host.GetType ().GetTypeInfo ();
			if (!typeInfo.IsGenericType)
				return false;

			var genericType = typeInfo.GetGenericTypeDefinition ();
			return genericType == typeof(FixedParameterHost<>);
		}

		internal static EnumValueFilter GetEnumValueFilter (TestCategory category)
		{
			return new CategoryEnumValueFilter (category);
		}

		internal static EnumValueFilter GetEnumValueFilter (TestFeature feature)
		{
			return new FeatureEnumValueFilter (feature);
		}

		internal static EnumValueFilter GetEnumValueFilter (IReadOnlyList<EnumValueFilter> filters)
		{
			return new CombinedEnumValueFilter (filters);
		}

		class CategoryEnumValueFilter : EnumValueFilter
		{
			public TestCategory Category {
				get;
			}

			public CategoryEnumValueFilter (TestCategory category)
			{
				Category = category;
			}

			public override bool Filter (TestContext ctx, object value)
			{
				return ctx.CurrentCategory == Category;
			}
		}

		class FeatureEnumValueFilter : EnumValueFilter
		{
			public TestFeature Feature {
				get;
			}

			public FeatureEnumValueFilter (TestFeature feature)
			{
				Feature = feature;
			}

			public override bool Filter (TestContext ctx, object value)
			{
				return ctx.IsEnabled (Feature);
			}
		}

		class CombinedEnumValueFilter : EnumValueFilter
		{
			public IReadOnlyList<EnumValueFilter> Filters {
				get;
			}

			public CombinedEnumValueFilter (IReadOnlyList<EnumValueFilter> filters)
			{
				Filters = filters;
			}

			public override bool Filter (TestContext ctx, object value)
			{
				return Filters.All (f => f.Filter (ctx, value));
			}
		}

		#endregion
	}
}

