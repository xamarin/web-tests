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
using System.Net.Http;
using System.Net.Sockets;
using System.Net.NetworkInformation;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Reflection;
using System.Diagnostics;
using System.Collections.Generic;
using Mono.Security.Protocol.Ntlm;

using Xamarin.AsyncTests;

namespace Xamarin.WebTests.Portable
{
	using Framework;
	using Portable;
	using Server;

	public class PortableSupportImpl : IPortableSupport, IPortableWebSupport
	{
		PortableSupportImpl ()
		{
		}

		#region Misc

		public string CurrentThreadId {
			get { return Thread.CurrentThread.ManagedThreadId.ToString (); }
		}

		public bool IsMicrosoftRuntime {
			get { return isMsRuntime; }
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

		public static void Initialize ()
		{
			var instance = new PortableSupportImpl ();
			PortableSupport.Initialize (instance, instance);
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

		#endregion

		#region Stack Trace

		[HideStackFrame]
		public string GetStackTrace (bool full)
		{
			var trace = new StackTrace (true);
			var frames = new List<string> ();
			int top = 0;

			for (int i = 0; i < trace.FrameCount; i++) {
				var frame = trace.GetFrame (i);
				var formatted = FormatFrame (frame);
				if (full) {
					frames.Add (formatted);
					continue;
				}

				var method = frame.GetMethod ();
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

				default:
					break;
				}

				frames.Add (formatted);

				var hideAttr = method.GetCustomAttributes (typeof(HideStackFrameAttribute), true);
				if (hideAttr.Length > 0) {
					top = i + 1;
					continue;
				}

				var asyncAttr = method.GetCustomAttributes (typeof(AsyncTestAttribute), true);
				if (asyncAttr.Length > 0)
					break;

				var entryPointAttr = method.GetCustomAttributes (typeof(StackTraceEntryPointAttribute), true);
				if (entryPointAttr.Length > 0)
					break;
			}

			var sb = new StringBuilder ();
			for (int i = top; i < frames.Count; i++) {
				sb.Append (frames [i]);
				sb.AppendLine ();
			}
			return sb.ToString ();
		}

		string FormatFrame (StackFrame frame)
		{
			var sb = new StringBuilder ();
			sb.Append ("   at ");

			var method = frame.GetMethod ();
			if (method != null) {
				FormatType (sb, method.DeclaringType);
				sb.Append (".");
				sb.Append (method.Name);
				sb.Append ("(");

				var p = method.GetParameters ();
				for (int j = 0; j < p.Length; ++j) {
					if (j > 0)
						sb.Append (", ");
					Type pt = p [j].ParameterType;
					bool byref = pt.IsByRef;
					if (byref)
						pt = pt.GetElementType ();
					if (byref)
						sb.Append ("&");
					FormatType (sb, pt);
				}
				sb.Append (")");
			} else {
				sb.Append ("<unknown method>");
			}

			sb.AppendFormat ("+0x{0:x}", frame.GetILOffset ());

			var fname = frame.GetFileName ();
			if (fname != null && fname != "<filename unknown>")
				sb.AppendFormat (" in {0}:{1}", fname, frame.GetFileLineNumber ());

			return sb.ToString ();
		}

		string FormatType (Type type)
		{
			var sb = new StringBuilder ();
			FormatType (sb, type);
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

		void FormatType (StringBuilder sb, Type type)
		{
			var builtin = FormatBuiltinType (type);
			if (builtin != null) {
				sb.Append (builtin);
				return;
			}

			if (!string.IsNullOrEmpty (type.Namespace)) {
				sb.Append (type.Namespace);
				sb.Append (".");
			}
			sb.Append (type.Name);

			var args = type.GetGenericArguments ();
			if (args == null || args.Length == 0)
				return;

			sb.Append ("<");
			for (int i = 0; i < args.Length; i++) {
				if (i > 0)
					sb.Append (",");
				FormatType (sb, args [i]);
			}
			sb.Append (">");
		}

		#endregion

		#region Networking

		public bool HasNetwork {
			get { return hasNetwork; }
		}

		public IPortableEndPoint GetLoopbackEndpoint (int port)
		{
			return new PortableEndpoint (new IPEndPoint (IPAddress.Loopback, port));
		}

		public IPortableEndPoint GetEndpoint (int port)
		{
			return new PortableEndpoint (new IPEndPoint (address, port));
		}

		public static IPEndPoint GetEndpoint (IPortableEndPoint endpoint)
		{
			return (PortableEndpoint)endpoint;
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

		class PortableEndpoint : IPortableEndPoint
		{
			readonly IPEndPoint endpoint;

			public PortableEndpoint (IPEndPoint endpoint)
			{
				this.endpoint = endpoint;
			}

			public int Port {
				get { return endpoint.Port; }
			}

			public string Address {
				get { return endpoint.Address.ToString (); }
			}

			public bool IsLoopback {
				get { return IPAddress.IsLoopback (endpoint.Address); }
			}

			public IPortableEndPoint CopyWithPort (int port)
			{
				return new PortableEndpoint (new IPEndPoint (endpoint.Address, port));
			}

			public static implicit operator IPEndPoint (PortableEndpoint portable)
			{
				return portable.endpoint;
			}

			public override string ToString ()
			{
				return string.Format ("[PortableEndpoint {0}]", endpoint);
			}
		}

		IPortableProxy IPortableWebSupport.CreateProxy (Uri uri)
		{
			return new PortableProxy (uri);
		}

		void IPortableWebSupport.SetProxy (HttpWebRequest request, IPortableProxy proxy)
		{
			request.Proxy = (PortableProxy)proxy;
		}

		void IPortableWebSupport.SetProxy (HttpClientHandler handler, IPortableProxy proxy)
		{
			handler.Proxy = (PortableProxy)proxy;
		}

		class PortableProxy : IPortableProxy, IWebProxy
		{
			readonly Uri uri;

			public PortableProxy (Uri uri)
			{
				this.uri = uri;
			}

			public Uri Uri {
				get { return uri; }
			}

			public ICredentials Credentials {
				get; set;
			}

			public Uri GetProxy (Uri destination)
			{
				return uri;
			}

			public bool IsBypassed (Uri host)
			{
				return false;
			}
		}

		void IPortableWebSupport.SetAllowWriteStreamBuffering (HttpWebRequest request, bool value)
		{
			request.AllowWriteStreamBuffering = value;
		}

		void IPortableWebSupport.SetKeepAlive (HttpWebRequest request, bool value)
		{
			request.KeepAlive = value;
		}

		void IPortableWebSupport.SetSendChunked (HttpWebRequest request, bool value)
		{
			request.SendChunked = value;
		}

		void IPortableWebSupport.SetContentLength (HttpWebRequest request, long length)
		{
			request.ContentLength = length;
		}

		Stream IPortableWebSupport.GetRequestStream (HttpWebRequest request)
		{
			return request.GetRequestStream ();
		}

		Task<Stream> IPortableWebSupport.GetRequestStreamAsync (HttpWebRequest request)
		{
			return request.GetRequestStreamAsync ();
		}

		HttpWebResponse IPortableWebSupport.GetResponse (HttpWebRequest request)
		{
			return (HttpWebResponse)request.GetResponse ();
		}

		async Task<HttpWebResponse> IPortableWebSupport.GetResponseAsync (HttpWebRequest request)
		{
			return (HttpWebResponse)await request.GetResponseAsync ();
		}

		IWebClient IPortableWebSupport.CreateWebClient ()
		{
			return new PortableWebClient ();
		}

		class PortableWebClient : IWebClient
		{
			WebClient client = new WebClient ();

			public Task<string> UploadStringTaskAsync (Uri uri, string data)
			{
				return client.UploadStringTaskAsync (uri, data);
			}

			public void Dispose ()
			{
				Dispose (true);
				GC.SuppressFinalize (this);
			}

			public void Dispose (bool disposing)
			{
				if (client != null) {
					client.Dispose ();
					client = null;
				}
			}

			~PortableWebClient ()
			{
				Dispose (false);
			}
		}

		#endregion

		#region Listeners

		IListener IPortableWebSupport.CreateHttpListener (IPortableEndPoint endpoint, IHttpServer server, bool reuseConnection, bool ssl)
		{
			return new HttpListener (endpoint, server, reuseConnection, ssl);
		}

		IListener IPortableWebSupport.CreateProxyListener (IListener httpListener, IPortableEndPoint proxyEndpoint, AuthenticationType authType)
		{
			return new ProxyListener ((HttpListener)httpListener, proxyEndpoint, authType);
		}

		#endregion

		#region NTLM Authentication

		bool IPortableWebSupport.HandleNTLM (ref byte[] bytes, ref bool haveChallenge)
		{
			if (haveChallenge) {
				// FIXME: We don't actually check the result.
				var message = new Type3Message (bytes);
				if (message.Type != 3)
					throw new InvalidOperationException ();

				return true;
			} else {
				var message = new Type1Message (bytes);
				if (message.Type != 1)
					throw new InvalidOperationException ();

				var type2 = new Type2Message ();
				haveChallenge = true;
				bytes = type2.GetBytes ();
				return false;
			}
		}

		#endregion
	}
}

