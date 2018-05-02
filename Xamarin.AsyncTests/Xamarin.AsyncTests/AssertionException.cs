//
// AssertionException.cs
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
using System.Threading;

namespace Xamarin.AsyncTests
{
	using Portable;

	public class AssertionException : Exception
	{
		static long nextId;

		internal AssertionException (string message, string stackTrace)
			: base (message)
		{
			StackTrace = stackTrace ?? base.StackTrace;
			ID = Interlocked.Increment (ref nextId);
		}

		internal AssertionException (string message, Exception inner, string stackTrace)
			: base (message, inner)
		{
			StackTrace = stackTrace ?? base.StackTrace;
			ID = Interlocked.Increment (ref nextId);
		}

		public AssertionException (string message)
			: this (message, GetStackTrace ())
		{
		}

		public AssertionException (string message, Exception inner)
			: this (message, inner, GetStackTrace ())
		{
		}

		[HideStackFrame]
		public static string GetStackTrace ()
		{
			var support = DependencyInjector.Get<IPortableSupport> ();
			return support.GetStackTrace (false);
		}

		[HideStackFrame]
		public static string GetStackTrace (Exception error)
		{
			var support = DependencyInjector.Get<IPortableSupport> ();
			return support.GetStackTrace (error, false);
		}

		public sealed override string StackTrace {
			get;
		}

		public long ID {
			get;
		}

		public override string ToString ()
		{
			string s = Message;

			if (InnerException != null)
				s = s + " ---> " + InnerException + Environment.NewLine +
				"   --- End of inner exception stack trace ---";

			if (StackTrace != null)
				s += Environment.NewLine + StackTrace;

			return s;
		}
	}
}

