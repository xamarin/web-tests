//
// NoRenegotiation.cs
//
// Author:
//       Martin Baulig <mabaul@microsoft.com>
//
// Copyright (c) 2018 Xamarin Inc. (http://www.xamarin.com)
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
using System.Security.Authentication;
using System.Threading;
using System.Threading.Tasks;
using Mono.Security.Interface;
using Xamarin.AsyncTests;
using Xamarin.AsyncTests.Constraints;

namespace Xamarin.WebTests.RenegotiationTests
{
	using ConnectionFramework;

	public class NoRenegotiation : RenegotiationTestFixture
	{
		protected override void CreateParameters (TestContext ctx, ConnectionParameters parameters)
		{
			parameters.AllowRenegotiation = false;
			base.CreateParameters (ctx, parameters);
		}

		protected override async Task HandleClientRead (TestContext ctx, CancellationToken cancellationToken)
		{
			var me = $"{nameof (HandleClientRead)}";

			try {
				await ExpectBlob (ctx, Client, TestCompletedString, TestCompletedBlob, cancellationToken).ConfigureAwait (false);
				throw ctx.AssertFail ($"{me} expected AuthenticationException");
			} catch (Exception ex) {
				var exception = TestContext.CleanupException (ex);

				if (ctx.Expect (exception, Is.Not.Null, me) &&
				    ctx.Expect (exception, Is.InstanceOf<AuthenticationException> (), me) &&
				    ctx.Expect (exception.InnerException, Is.InstanceOf<TlsException> (), me)) {
					var tlsExc = (TlsException)exception.InnerException;
					ctx.Assert (tlsExc.Alert.Description, Is.EqualTo (AlertDescription.NoRenegotiation), me);
				}
			} finally {
				StartClientWrite (false);
				Client.Dispose ();
			}
		}

		protected override async Task HandleServer (TestContext ctx, CancellationToken cancellationToken)
		{
			var me = $"{nameof (HandleServer)}";

			try {
				await ctx.AssertException<IOException> (() => MonoServer.RenegotiateAsync (cancellationToken)).ConfigureAwait (false);
			} finally {
				StartServerRead (false);
				StartServerWrite (false);
			}
		}

		protected override Task ClientShutdown (TestContext ctx, CancellationToken cancellationToken)
		{
			return FinishedTask;
		}

		protected override Task ServerShutdown (TestContext ctx, CancellationToken cancellationToken)
		{
			return FinishedTask;
		}
	}
}
