//
// PortableSupport.cs
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
using System.Net;
using System.Net.Sockets;
using System.Net.NetworkInformation;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Reflection;
using System.Diagnostics;
using System.Collections.Generic;
using System.Collections.Specialized;

using Xamarin.AsyncTests;

namespace Xamarin.AsyncTests.Portable
{
	using System.Runtime.CompilerServices;
	using Framework;

	class PortableSupportImpl : IPortableSupport
	{
		#region Misc

		public string CurrentThreadId => Thread.CurrentThread.ManagedThreadId.ToString ();

		public string CurrentDomain => AppDomain.CurrentDomain.FriendlyName;

		public string CurrentProcess {
			get {
				var process = Process.GetCurrentProcess ();
				return $"{process.ProcessName}:{process.Id}";
			}
		}

		public bool IsMicrosoftRuntime {
			get { return isMsRuntime; }
		}

		public bool IsAndroid {
			get {
				#if __ANDROID__
				return true;
				#else
				return false;
				#endif
			}
		}

		public bool IsMobile {
			get {
				#if __MOBILE__
				return true;
				#else
				return false;
				#endif
			}
		}

		public bool IsIOS {
			get {
				#if __IOS__
				return true;
				#else
				return false;
				#endif
			}
		}

		public Version MonoRuntimeVersion {
			get { return runtimeVersion; }
		}

		static PortableSupportImpl ()
		{
			try {
				address = LookupAddress ();
				hasNetwork = !IPAddress.IsLoopback (address);
			} catch {
				address = IPAddress.Loopback;
				hasNetwork = false;
			}

			try {
				runtimeVersion = GetRuntimeVersion ();
			} catch {
				;
			}

			isMsRuntime = Environment.OSVersion.Platform == PlatformID.Win32NT && runtimeVersion == null;
		}

		static readonly bool hasNetwork;
		static readonly IPAddress address;

		static readonly bool isMsRuntime;
		static readonly Version runtimeVersion;

		static Version GetRuntimeVersion ()
		{
			string version;
			#if __MOBILE__
			version = Mono.Runtime.GetDisplayName ();
			#else
			Type type = Type.GetType ("Mono.Runtime", false);
			if (type == null)
			return null;

			var method = type.GetMethod ("GetDisplayName", BindingFlags.NonPublic | BindingFlags.Static);
			if (method == null)
			return null;

			version = (string)method.Invoke (null, null);
			#endif

			var match = Regex.Match (version, @"^(\d+)\.(\d+)(?:\.(\d+))?\b");
			if (!match.Success)
				return null;

			var major = int.Parse (match.Groups [1].Value);
			var minor = int.Parse (match.Groups [2].Value);
			int build = 0;

			if (match.Groups.Count > 2 && match.Groups [3].Success)
				build = int.Parse (match.Groups [3].Value);

			return new Version (major, minor, build);
		}

		public Encoding ASCIIEncoding {
			get { return Encoding.ASCII; }
		}

		public void Close (Stream stream)
		{
			stream.Close ();
		}

		#endregion

		#region Stack Trace

		[HideStackFrame]
		public string GetStackTrace (Exception error, bool full)
		{
			var trace = new StackTrace (error, true);
			if (error is AssertionException)
				return error.StackTrace;
			return GetStackTrace (trace, full);
		}

		[HideStackFrame]
		public string GetStackTrace (bool full)
		{
			var trace = new StackTrace (true);
			return GetStackTrace (trace, full);
		}

		string GetStackTrace (StackTrace trace, bool full)
		{
			var frames = new List<string> ();
			int top = 0;

			for (int i = 0; i < trace.FrameCount; i++) {
				var frame = trace.GetFrame (i);
				var method = frame.GetMethod ();
				var formatted = FormatFrame (frame, method);
				if (full) {
					frames.Add (formatted);
					continue;
				}

				if (method == null) {
					frames.Add (formatted);
					continue;
				}

				var ns = method.DeclaringType.Namespace;
				switch (ns) {
				case "System.Runtime.CompilerServices":
				case "System.Threading.Tasks":
				case "System.Reflection":
					continue;
				}

				CheckAsyncContinuation (frame, ref method, ref formatted);

				frames.Add (formatted);

				var hideAttr = method.GetCustomAttributes (typeof (HideStackFrameAttribute), true);
				if (hideAttr.Length > 0) {
					top = i + 1;
					continue;
				}

				var asyncAttr = method.GetCustomAttributes (typeof (AsyncTestAttribute), true);
				if (asyncAttr.Length > 0)
					break;

				var entryPointAttr = method.GetCustomAttributes (typeof (StackTraceEntryPointAttribute), true);
				if (entryPointAttr.Length > 0)
					break;
			}

			var sb = new StringBuilder ();
			for (int i = top; i < frames.Count; i++) {
				sb.Append (frames[i]);
				sb.AppendLine ();
			}
			return sb.ToString ();
		}

		void CheckAsyncContinuation (StackFrame frame, ref MethodBase method, ref string formatted)
		{
			if (method.DeclaringType.GetCustomAttribute<CompilerGeneratedAttribute> () == null)
				return;
			// Try to detect async task continuations.
			var match = Regex.Match (method.DeclaringType.FullName, @"(.*)\+\<(.*)\>d__\d+");
			if (!match.Success)
				return;
			var type = method.DeclaringType.Assembly.GetType (match.Groups[1].Value);
			if (type == null)
				return;

			var fields = method.DeclaringType.GetFields ();
			if (fields.Length < 3)
				return;

			if (fields[0].FieldType != typeof (int))
				return;
			if (fields[1].FieldType != typeof (AsyncTaskMethodBuilder))
				return;

			var arguments = new List<Type> ();
			for (var i = 2; i < fields.Length - 1; i++) {
				arguments.Add (fields[i].FieldType);
			}

			method = type.GetMethod (match.Groups[2].Value, arguments.ToArray ());
			if (method == null)
				return;
			formatted = FormatFrame (frame, method);
		}

		string FormatFrame (StackFrame frame, MethodBase method)
		{
			var sb = new StringBuilder ();
			sb.Append ("   at ");

			if (method != null) {
				FormatMethod (sb, method, true);
			} else {
				sb.Append ("<unknown method>");
			}

			sb.AppendFormat ("+0x{0:x}", frame.GetILOffset ());

			var fname = frame.GetFileName ();
			if (fname != null && fname != "<filename unknown>")
				sb.AppendFormat (" in {0}:{1}", fname, frame.GetFileLineNumber ());

			return sb.ToString ();
		}

		public string FormatMethod (MethodBase method, bool includeNamespace)
		{
			var sb = new StringBuilder ();
			FormatMethod (sb, method, includeNamespace);
			return sb.ToString ();
		}

		void FormatMethod (StringBuilder sb, MethodBase method, bool includeNamespace)
		{
			FormatType (sb, method.DeclaringType, includeNamespace);
			sb.Append (".");
			sb.Append (method.Name);
			sb.Append ("(");

			var p = method.GetParameters ();
			for (int j = 0; j < p.Length; ++j) {
				if (j > 0)
					sb.Append (", ");
				Type pt = p[j].ParameterType;
				bool byref = pt.IsByRef;
				if (byref)
					pt = pt.GetElementType ();
				if (byref)
					sb.Append ("&");
				FormatType (sb, pt, includeNamespace);
			}
			sb.Append (")");
		}

		public string FormatType (Type type, bool includeNamespace)
		{
			var sb = new StringBuilder ();
			FormatType (sb, type, includeNamespace);
			return sb.ToString ();
		}

		string FormatBuiltinType (Type type)
		{
			switch (type.FullName) {
			case "System.Byte":
				return "byte";
			case "System.SByte":
				return "sbyte";
			case "System.Char":
				return "char";
			case "System.Int16":
				return "short";
			case "System.UInt16":
				return "ushort";
			case "System.Int32":
				return "int";
			case "System.UInt32":
				return "uint";
			case "System.Int64":
				return "long";
			case "System.UInt64":
				return "ulong";
			case "System.Single":
				return "float";
			case "System.Double":
				return "double";
			case "System.Void":
				return "void";
			case "System.String":
				return "string";
			default:
				return null;
			}
		}

		void FormatType (StringBuilder sb, Type type, bool includeNamespace)
		{
			var builtin = FormatBuiltinType (type);
			if (builtin != null) {
				sb.Append (builtin);
				return;
			}

			if (includeNamespace && !string.IsNullOrEmpty (type.Namespace)) {
				sb.Append (type.Namespace);
				sb.Append (".");
			}

			int pos = type.Name.IndexOf ("`", StringComparison.Ordinal);
			if (pos > 0)
				sb.Append (type.Name.Substring (0, pos));
			else
				sb.Append (type.Name);

			var args = type.GetGenericArguments ();
			if (args == null || args.Length == 0)
				return;

			sb.Append ("<");
			for (int i = 0; i < args.Length; i++) {
				if (i > 0)
					sb.Append (",");
				FormatType (sb, args [i], includeNamespace);
			}
			sb.Append (">");
		}

		#endregion

		#region Networking

		public bool HasNetwork {
			get { return hasNetwork; }
		}

		public static IPAddress LocalAddress {
			get { return address; }
		}

		static IPAddress LookupAddress ()
		{
			try {
				#if __IOS__
				var interfaces = NetworkInterface.GetAllNetworkInterfaces ();
				foreach (var iface in interfaces) {
					if (iface.NetworkInterfaceType != NetworkInterfaceType.Ethernet && iface.NetworkInterfaceType != NetworkInterfaceType.Wireless80211)
						continue;
					foreach (var address in iface.GetIPProperties ().UnicastAddresses) {
						if (address.Address.AddressFamily == AddressFamily.InterNetwork && !IPAddress.IsLoopback (address.Address))
							return address.Address;
					}
				}
				#else
				var hostname = Dns.GetHostName ();
				var hostent = Dns.GetHostEntry (hostname);
				foreach (var address in hostent.AddressList) {
					if (address.AddressFamily == AddressFamily.InterNetwork && !IPAddress.IsLoopback (address))
						return address;
				}
				#endif
			} catch {
				;
			}

			return IPAddress.Loopback;
		}

		#endregion
	}
}

