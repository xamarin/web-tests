//
// SslStreamTestFixture.cs
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
using System.Net;
using System.Xml.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xamarin.AsyncTests;

namespace Xamarin.WebTests.SslStreamTests
{
	using TestFramework;
	using TestAttributes;
	using TestRunners;

	[AsyncTestFixture (Prefix = "SslStreamTests")]
	[Fork (ForkType.FromContext)]
	[ConnectionTestFlags (ConnectionTestFlags.RequireSslStream)]
	public abstract class SslStreamTestFixture : SslStreamTestRunner, IForkedTestInstance
	{
		protected override string LogCategory => LogCategories.StreamInstrumentationTestRunner;

		[FixtureParameter]
		public virtual ForkType ForkType => ForkType.None;

		protected bool IsForked {
			get;
			private set;
		}

		[AsyncTest]
		public static Task Run (
			TestContext ctx, CancellationToken cancellationToken,
			ConnectionTestProvider provider,
			SslStreamTestFixture fixture)
		{
			return fixture.Run (ctx, cancellationToken);
		}

		[AsyncTest]
		[Martin (null, UseFixtureName = true)]
		public static Task MartinTest (
			TestContext ctx, CancellationToken cancellationToken,
			ConnectionTestProvider provider,
			SslStreamTestFixture fixture)
		{
			return fixture.Run (ctx, cancellationToken);
		}

		protected virtual void Serialize (TestContext ctx, XElement element)
		{
		}

		protected virtual void Deserialize (TestContext ctx, XElement element)
		{
			IsForked = true;
		}

		void IForkedTestInstance.Serialize (TestContext ctx, XElement element) => Serialize (ctx, element);

		void IForkedTestInstance.Deserialize (TestContext ctx, XElement element) => Deserialize (ctx, element);

		protected override Task StartClient (TestContext ctx, CancellationToken cancellationToken)
		{
			return base.StartClient (ctx, cancellationToken);
		}

		protected override Task StartServer (TestContext ctx, CancellationToken cancellationToken)
		{
			return base.StartServer (ctx, cancellationToken);
		}

		bool CheckForkedContext ()
		{
			if (ForkType != ForkType.None && !IsForked)
				return false;
			return true;
		}

		protected override Task PreRun (TestContext ctx, CancellationToken cancellationToken)
		{
			if (!CheckForkedContext ())
				return FinishedTask;
			return base.PreRun (ctx, cancellationToken);
		}

		protected override Task PostRun (TestContext ctx, CancellationToken cancellationToken)
		{
			if (!CheckForkedContext ())
				return FinishedTask;
			return base.PostRun (ctx, cancellationToken);
		}

		protected sealed override Task Initialize (TestContext ctx, CancellationToken cancellationToken)
		{
			if (!CheckForkedContext ())
				return FinishedTask;
			return base.Initialize (ctx, cancellationToken);
		}

		protected override Task Destroy (TestContext ctx, CancellationToken cancellationToken)
		{
			if (!CheckForkedContext ())
				return FinishedTask;
			return base.Destroy (ctx, cancellationToken);
		}

		protected override void Stop ()
		{
			base.Stop ();
		}
	}
}
