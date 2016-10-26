//
// BoringX509StoreHost.cs
//
// Author:
//       Martin Baulig <martin.baulig@xamarin.com>
//
// Copyright (c) 2016 Xamarin Inc. (http://www.xamarin.com)

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
using System.Threading.Tasks;
using Xamarin.AsyncTests;

using Mono.Btls.Interface;

namespace Mono.Btls.TestFramework
{
	[BoringX509StoreHost]
	public class BoringX509StoreHost : ITestInstance
	{
		BtlsX509Store store;
		bool loadTrustedRoots;

		public BoringX509StoreHost (bool loadTrustedRoots)
		{
			this.loadTrustedRoots = loadTrustedRoots;
		}

		public BtlsX509Store Instance {
			get {
				if (store == null)
					throw new InvalidOperationException ();
				return store;
			}
		}

		public Task Destroy (TestContext ctx, CancellationToken cancellationToken)
		{
			return Task.Run (() => {
				if (store != null) {
					store.Dispose ();
					store = null;
				}
			});
		}

		public Task Initialize (TestContext ctx, CancellationToken cancellationToken)
		{
			return Task.Run (() => {
				store = BtlsProvider.CreateNativeStore ();
				if (loadTrustedRoots)
					store.AddTrustedRoots ();
			});
		}

		public Task PostRun (TestContext ctx, CancellationToken cancellationToken)
		{
			return Task.FromResult<object> (null);
		}

		public Task PreRun (TestContext ctx, CancellationToken cancellationToken)
		{
			return Task.FromResult<object> (null);
		}
	}
}

