//
// ValidationTestRunner.cs
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
using System.Text;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;
using Mono.Security.Interface;
using Xamarin.AsyncTests;
using Xamarin.AsyncTests.Constraints;
using Xamarin.WebTests.Resources;
using Xamarin.WebTests.ConnectionFramework;
using Xamarin.WebTests.MonoConnectionFramework;

namespace Xamarin.WebTests.MonoTestFramework
{
	using MonoTestFeatures;

	public abstract class ValidationTestRunner : ITestInstance, IDisposable
	{
		public ValidationTestParameters Parameters {
			get;
			private set;
		}

		public ValidationTestRunner (ValidationTestParameters parameters)
		{
			Parameters = parameters;
		}

		public abstract void Run (TestContext ctx);

		protected X509CertificateCollection GetCertificates ()
		{
			var certs = new X509CertificateCollection ();
			foreach (var type in Parameters.Types)
				certs.Add (new X509Certificate (ResourceManager.GetCertificateData (type)));
			return certs;
		}

		protected internal static Task FinishedTask {
			get { return Task.FromResult<object> (null); }
		}

		protected virtual void PreRun (TestContext ctx)
		{
		}

		protected virtual void PostRun (TestContext ctx)
		{
		}

		#region ITestInstance implementation

		public Task Initialize (TestContext ctx, CancellationToken cancellationToken)
		{
			return FinishedTask;
		}

		public Task PreRun (TestContext ctx, CancellationToken cancellationToken)
		{
			return Task.Run (() => PreRun (ctx));
		}

		public virtual Task PostRun (TestContext ctx, CancellationToken cancellationToken)
		{
			return Task.Run (() => PostRun (ctx));
		}

		public Task Destroy (TestContext ctx, CancellationToken cancellationToken)
		{
			return Task.Run (() => {
				Dispose ();
			});
		}

		#endregion

		public void Dispose ()
		{
			Dispose (true);
			GC.SuppressFinalize (this);
		}

		bool disposed;

		protected virtual void Dispose (bool disposing)
		{
			lock (this) {
				if (disposed)
					return;
				disposed = true;
			}
		}

		public override string ToString ()
		{
			return string.Format ("[{0}: {1}]", GetType ().Name, Parameters.Identifier);
		}
	}
}

