﻿//
// Response.cs
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
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Xamarin.AsyncTests;
using Xamarin.AsyncTests.Constraints;

namespace Xamarin.WebTests.HttpHandlers
{
	using HttpFramework;

	public abstract class Response : IDisposable
	{
		public Request Request {
			get;
			private set;
		}

		public Response (Request request)
		{
			Request = request;
			Interlocked.Increment (ref countInstances);
		}

		public abstract HttpStatusCode Status {
			get;
		}

		public abstract bool IsSuccess {
			get;
		}

		public abstract Exception Error {
			get;
		}

		public abstract HttpContent Content {
			get;
		}

		int disposed;
		static int countInstances;

		protected virtual void Close ()
		{
			Interlocked.Decrement (ref countInstances);
		}

		public static void CheckLeakingInstances (TestContext ctx)
		{
			ctx.Assert (countInstances, Is.EqualTo (0), $"Leaking `{typeof (Response)}' instances.");
		}

		public void Dispose ()
		{
			if (Interlocked.CompareExchange (ref disposed, 1, 0) != 0)
				return;

			Close ();
		}
	}
}

