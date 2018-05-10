//
// ConnectionTestFeatures.cs
//
// Author:
//       Martin Baulig <martin.baulig@xamarin.com>
//
// Copyright (c) 2015 Xamarin, Inc.
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
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using Xamarin.AsyncTests;
using Xamarin.AsyncTests.Constraints;
using Xamarin.AsyncTests.Portable;

namespace Xamarin.WebTests.TestFramework
{
	using TestRunners;
	using ConnectionFramework;

	public static class ConnectionTestHelper {
		static readonly ConnectionProviderFactory Factory;

		static ConnectionTestHelper ()
		{
			Factory = DependencyInjector.Get<ConnectionProviderFactory> ();
		}

		public static ConnectionProviderFlags GetProviderFlags (ConnectionProviderType type)
		{
			return Factory.GetProviderFlags (type);
		}

		public static IPortableEndPoint GetEndPoint ()
		{
			var support = DependencyInjector.Get<IPortableEndPointSupport> ();
			var port = TestContext.GetUniquePort ();
			return support.GetLoopbackEndpoint (port);
		}

		public static bool IsMicrosoftRuntime {
			get { return DependencyInjector.Get<IPortableSupport> ().IsMicrosoftRuntime; }
		}

		public static IEnumerable<R> Join<T, U, R> (IEnumerable<T> first, IEnumerable<U> second, Func<T, U, R> resultSelector, bool filterOutNull = true)
		{
			foreach (var e1 in first) {
				foreach (var e2 in second) {
					var result = resultSelector (e1, e2);
					if (!filterOutNull || result != null)
						yield return result;
				}
			}
		}
	}
}

