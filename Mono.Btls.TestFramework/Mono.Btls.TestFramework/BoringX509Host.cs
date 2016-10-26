//
// BoringX509Host.cs
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
	[BoringX509Host]
	public class BoringX509Host : ITestInstance
	{
		BtlsX509 x509;
		readonly byte[] data;
		readonly BtlsX509Format format;

		public BoringX509Host (byte[] data, BtlsX509Format format)
		{
			this.data = data;
			this.format = format;
		}

		public BtlsX509 Instance {
			get {
				if (x509 == null)
					throw new InvalidOperationException ();
				return x509;
			}
		}

		public Task Destroy (TestContext ctx, CancellationToken cancellationToken)
		{
			return Task.Run (() => {
				if (x509 != null) {
					x509.Dispose ();
					x509 = null;
				}
			});
		}

		public Task Initialize (TestContext ctx, CancellationToken cancellationToken)
		{
			return Task.Run (() => {
				x509 = BtlsProvider.CreateNative (data, format);
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

