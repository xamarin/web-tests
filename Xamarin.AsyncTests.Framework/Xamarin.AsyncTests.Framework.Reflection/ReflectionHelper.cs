//
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
using System.Collections.Generic;

namespace Xamarin.AsyncTests.Framework.Reflection
{
	public static class ReflectionHelper
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

		public static IMemberInfo GetMethodInfo (MethodInfo member)
		{
			return new _MethodInfo (member);
		}

		public static IMemberInfo GetTypeInfo (TypeInfo member)
		{
			return new _TypeInfo (member);
		}

		class _ParameterInfo : IMemberInfo
		{
			public readonly ParameterInfo Member;

			public _ParameterInfo (ParameterInfo member)
			{
				Member = member;
				Type = member.ParameterType.GetTypeInfo ();
			}

			public string Name {
				get { return Member.Name; }
			}

			public TypeInfo Type {
				get;
				private set;
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
				Type = member.PropertyType.GetTypeInfo ();
			}

			public string Name {
				get { return Member.Name; }
			}

			public TypeInfo Type {
				get;
				private set;
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

		class _MethodInfo : IMemberInfo
		{
			public readonly MethodInfo Member;

			public _MethodInfo (MethodInfo member)
			{
				Member = member;
				Type = member.ReturnType.GetTypeInfo ();
			}

			public string Name {
				get { return Member.Name; }
			}

			public TypeInfo Type {
				get;
				private set;
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

		class _TypeInfo : IMemberInfo
		{
			public readonly TypeInfo Member;

			public _TypeInfo (TypeInfo member)
			{
				Member = member;
			}

			public string Name {
				get { return Member.Name; }
			}

			public TypeInfo Type {
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
		}

		#endregion

		#region Test Hosts

		internal static TestHost ResolveParameter (
			ReflectionTestFixtureBuilder builder, ParameterInfo member)
		{
			return ResolveParameter (builder, new _ParameterInfo (member));
		}

		internal static TestHost ResolveParameter (
			ReflectionTestFixtureBuilder builder, PropertyInfo member)
		{
			return ResolveParameter (builder, new _PropertyInfo (member));
		}

		static TestHost ResolveParameter (
			ReflectionTestFixtureBuilder fixture, IMemberInfo member)
		{
			if (typeof(ITestInstance).GetTypeInfo ().IsAssignableFrom (member.Type)) {
				var hostAttr = member.GetCustomAttribute<TestHostAttribute> ();
				if (hostAttr == null)
					hostAttr = member.Type.GetCustomAttribute<TestHostAttribute> ();
				if (hostAttr == null)
					throw new InternalErrorException ();

				return CreateCustomHost (fixture.Type, member, hostAttr);
			}

			var paramAttrs = member.GetCustomAttributes<TestParameterAttribute> ().ToArray ();
			if (paramAttrs.Length == 1)
				return CreateParameterAttributeHost (fixture.Type, member, paramAttrs[0]);
			else if (paramAttrs.Length > 1)
				throw new InternalErrorException ();

			paramAttrs = member.Type.GetCustomAttributes<TestParameterAttribute> ().ToArray ();
			if (paramAttrs.Length == 1)
				return CreateParameterAttributeHost (fixture.Type, member, paramAttrs [0]);
			else if (paramAttrs.Length > 1)
				throw new InternalErrorException ();

			if (member.Type.AsType ().Equals (typeof(bool)))
				return CreateBoolean (member);

			if (member.Type.IsEnum)
				return CreateEnum (member);

			throw new InternalErrorException ();
		}

		static Type GetCustomHostType (
			TypeInfo fixtureType, IMemberInfo member, TestHostAttribute attr,
			out bool useFixtureInstance)
		{
			var hostType = typeof(ITestHost<>).GetTypeInfo ();
			var genericInstance = hostType.MakeGenericType (member.Type.AsType ()).GetTypeInfo ();

			if (attr.HostType != null) {
				if (!genericInstance.IsAssignableFrom (attr.HostType.GetTypeInfo ()))
					throw new InternalErrorException ();
				useFixtureInstance = genericInstance.IsAssignableFrom (fixtureType);
				return attr.HostType;
			}

			if (genericInstance.IsAssignableFrom (fixtureType)) {
				useFixtureInstance = true;
				return null;
			}

			throw new InternalErrorException ();
		}

		static TestHost CreateCustomHost (TypeInfo fixture, IMemberInfo member, TestHostAttribute attr)
		{
			bool useFixtureInstance;
			var hostType = GetCustomHostType (fixture, member, attr, out useFixtureInstance);

			var type = typeof(CustomTestHost<>).MakeGenericType (member.Type.AsType ());
			return (TestHost)Activator.CreateInstance (
				type, member.Name, hostType, useFixtureInstance);
		}

		static void Debug (string format, params object[] args)
		{
			System.Diagnostics.Debug.WriteLine (string.Format (format, args));
		}

		static Type GetParameterHostType (IMemberInfo member, TestParameterAttribute attr, out object sourceInstance)
		{
			var hostType = typeof(ITestParameterSource<>).GetTypeInfo ();
			var genericInstance = hostType.MakeGenericType (member.Type.AsType ()).GetTypeInfo ();

			var attrType = attr.GetType ();

			if (genericInstance.IsAssignableFrom (attrType.GetTypeInfo ())) {
				sourceInstance = attr;
				return attrType;
			}

			if (member.Type.AsType ().Equals (typeof(bool))) {
				sourceInstance = null;
				return typeof(BooleanTestSource);
			} else if (member.Type.IsEnum) {
				sourceInstance = null;
				return typeof(EnumTestSource<>).MakeGenericType (member.Type.AsType ());
			}

			throw new InternalErrorException ();
		}

		static TestHost CreateParameterAttributeHost (
			TypeInfo fixtureType, IMemberInfo member, TestParameterAttribute attr)
		{
			object sourceInstance;
			var sourceType = GetParameterHostType (member, attr, out sourceInstance);

			IParameterSerializer serializer;
			if (!GetParameterSerializer (member.Type, sourceType, out serializer))
				throw new InternalErrorException ();

			if (sourceInstance == null)
				sourceInstance = Activator.CreateInstance (sourceType);

			var type = typeof(ParameterSourceHost<>).MakeGenericType (member.Type.AsType ());
			return (TestHost)Activator.CreateInstance (
				type, member.Name, sourceInstance, serializer, attr.Filter, attr.Flags);
		}

		static TestHost CreateEnum (IMemberInfo member)
		{
			if (!member.Type.IsEnum || member.Type.GetCustomAttribute<FlagsAttribute> () != null)
				throw new InternalErrorException ();

			var type = member.Type.AsType ();
			var sourceType = typeof(EnumTestSource<>).MakeGenericType (type);
			var hostType = typeof(ParameterSourceHost<>).MakeGenericType (type);

			var serializerType = typeof(EnumSerializer<>).MakeGenericType (type);
			var serializer = (IParameterSerializer)Activator.CreateInstance (serializerType);

			var source = Activator.CreateInstance (sourceType);
			return (ParameterizedTestHost)Activator.CreateInstance (
				hostType, member.Name, source, serializer, TestFlags.None);
		}

		static TestHost CreateBoolean (IMemberInfo member)
		{
			return new ParameterSourceHost<bool> (
				member.Name, new BooleanTestSource (),
				GetBooleanSerializer (), null, TestFlags.None);
		}

		class BooleanTestSource : ITestParameterSource<bool>
		{
			public IEnumerable<bool> GetParameters (TestContext ctx, string filter)
			{
				yield return false;
				yield return true;
			}
		}

		class EnumTestSource<T> : ITestParameterSource<T>
		{
			public IEnumerable<T> GetParameters (TestContext ctx, string filter)
			{
				foreach (var value in Enum.GetValues (typeof (T)))
					yield return (T)value;
			}
		}

		static bool GetParameterSerializer (TypeInfo type, Type sourceType, out IParameterSerializer serializer)
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

		class BooleanSerializer : IParameterSerializer
		{
			public bool Serialize (XElement node, object value)
			{
				node.Add (new XAttribute ("Value", (bool)value));
				return true;
			}
			public object Deserialize (XElement node)
			{
				if (node == null)
					throw new InternalErrorException ();
				return bool.Parse (node.Attribute ("Value").Value);
			}
		}

		class IntegerSerializer : IParameterSerializer
		{
			public bool Serialize (XElement node, object value)
			{
				node.Add (new XAttribute ("Value", (int)value));
				return true;
			}
			public object Deserialize (XElement node)
			{
				return int.Parse (node.Attribute ("Value").Value);
			}
		}

		class StringSerializer : IParameterSerializer
		{
			public bool Serialize (XElement node, object value)
			{
				node.Add (new XAttribute ("Value", (string)value));
				return true;
			}
			public object Deserialize (XElement node)
			{
				return node.Attribute ("Value").Value;
			}
		}

		class EnumSerializer<T> : IParameterSerializer
			where T : struct
		{
			public bool Serialize (XElement node, object value)
			{
				node.Add (new XAttribute ("Value", (T)value));
				return true;
			}
			public object Deserialize (XElement node)
			{
				var value = node.Attribute ("Value").Value;
				T result;
				if (!Enum.TryParse (value, out result))
					throw new InternalErrorException ();
				return result;
			}
		}

		#endregion

		#region Misc

		internal static IEnumerable<TestCategory> GetCategories (IMemberInfo member)
		{
			var cattrs = member.GetCustomAttributes<TestCategoryAttribute> ();
			if (cattrs.Count () == 0)
				return null;
			return cattrs.Select (c => c.Category);
		}

		internal static IEnumerable<TestFeature> GetFeatures (IMemberInfo member)
		{
			foreach (var cattr in member.GetCustomAttributes<TestFeatureAttribute> ())
				yield return cattr.Feature;
		}

		internal static TestFilter CreateTestFilter (TestFilter parent, IMemberInfo member)
		{
			var categories = ReflectionHelper.GetCategories (member);
			var features = ReflectionHelper.GetFeatures (member);

			return new TestFilter (parent, categories, features);
		}

		internal static TestHost CreateRepeatHost (int repeat)
		{
			return new RepeatedTestHost (repeat, TestFlags.Browsable);
		}

		#endregion
	}
}

