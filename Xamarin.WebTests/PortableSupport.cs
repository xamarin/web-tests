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
using System.Text;
using System.Reflection;
using System.Diagnostics;
using System.Collections.Generic;
using Xamarin.AsyncTests;

namespace Xamarin.WebTests
{
	public class PortableSupport : IPortableSupport
	{
		#region IPortableSupport implementation

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

		#endregion

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
	}
}

