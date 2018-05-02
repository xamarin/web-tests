//
// ReverseForkedHost.cs
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
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using Xamarin.AsyncTests.Constraints;

namespace Xamarin.AsyncTests.FrameworkTests
{
	using Portable;

	[ReverseForkedHost]
	[Fork (ForkType.ReverseDomain)]
	public class ReverseForkedHost : ITestInstance, IForkedTestInstance
	{
		public int IsForked {
			get;
			private set;
		}

		public ReverseForkedHost ()
		{
			IsForked = 1;
		}

		internal static int StaticVariable;

		public void Serialize (TestContext ctx, XElement element)
		{
			ctx.LogMessage ($"{ME} SERIALIZE: {StaticVariable} {IsForked}");

			var hello = element.Element ("Hello");

			// We're called once for the local and remote domain.
			switch (StaticVariable) {
			case 0:
				// First local serialize.
				StaticVariable = 1;
				ctx.Assert (IsForked, Is.EqualTo (1), nameof (IsForked));
				ctx.Assert (hello, Is.Null, nameof (hello));
				hello = new XElement ("Hello");
				element.Add (hello);
				hello.SetAttributeValue ("Text", "World");
				IsForked++;
				break;
			case 2:
				// First remote serialize.
				StaticVariable = 3;
				ctx.Assert (IsForked, Is.EqualTo (5), nameof (IsForked));
				ctx.Assert (hello, Is.Not.Null, nameof (hello));
				hello.SetAttributeValue ("Text", "Remote");
				IsForked++;
				break;
			default:
				throw ctx.AssertFail (nameof (StaticVariable));
			}
		}

		public void Deserialize (TestContext ctx, XElement element)
		{
			ctx.LogMessage ($"{ME} DESERIALIZE: {StaticVariable} {IsForked}");

			var hello = element.Element ("Hello");
			ctx.Assert (hello, Is.Not.Null, "root element");
			var attr = hello.Attribute ("Text");
			ctx.Assert (attr, Is.Not.Null, "attribute");

			switch (StaticVariable) {
			case 0:
				// Remote deserialize
				ctx.Assert (IsForked, Is.EqualTo (1), nameof (IsForked));
				ctx.Assert (attr.Value, Is.EqualTo ("World"), "attribute value");
				StaticVariable = 2;
				IsForked = 5;
				break;
			case 1:
				// Local deserialize, after coming back from the remote.
				ctx.Assert (StaticVariable, Is.EqualTo (1), nameof (StaticVariable));
				ctx.Assert (attr.Value, Is.EqualTo ("Remote"), "attribute value");
				ctx.Assert (IsForked, Is.EqualTo (1), nameof (IsForked));
				IsForked = 6;
				break;
			default:
				throw ctx.AssertFail (nameof (StaticVariable));
			}
		}

		internal static string ME = "REVERSE FORKED HOST";

		static Task FinishedTask => Task.FromResult<object> (null);

		static int preRunCalled, postRunCalled;

		public Task Initialize (TestContext ctx, CancellationToken cancellationToken)
		{
			ctx.LogMessage ($"{ME} INIT: {StaticVariable} {IsForked}");

			switch (StaticVariable) {
			case 1:
				ctx.Assert (IsForked, Is.EqualTo (2).Or.EqualTo (6), nameof (IsForked));
				break;
			case 2:
				// remote init
				ctx.Assert (IsForked, Is.EqualTo (5), nameof (IsForked));
				break;
			default:
				throw ctx.AssertFail (nameof (StaticVariable));
			}

			return FinishedTask;
		}

		public Task PreRun (TestContext ctx, CancellationToken cancellationToken)
		{
			ctx.LogMessage ($"{ME} PRE RUN: {StaticVariable} {IsForked}");
			ctx.TryGetParameter<IFork> (out var fork);
			ctx.Assert (fork, Is.Not.Null, "IFork parameter");

			switch (StaticVariable) {
			case 1:
				ctx.Assert (IsForked, Is.EqualTo (6), nameof (IsForked));
				break;
			case 2:
				// remote init
				ctx.Assert (IsForked, Is.EqualTo (5), nameof (IsForked));
				break;
			default:
				throw ctx.AssertFail (nameof (StaticVariable));
			}

			// ctx.Assert (IsForked, Is.EqualTo (3).Or.EqualTo (6), "only called in forked instance");
			ctx.Assert (Interlocked.Increment (ref preRunCalled), Is.EqualTo (1));
			return FinishedTask;
		}

		public Task PostRun (TestContext ctx, CancellationToken cancellationToken)
		{
			ctx.LogMessage ($"{ME} POST RUN: {StaticVariable} {IsForked}");
			ctx.TryGetParameter<IFork> (out var fork);
			ctx.Assert (fork, Is.Not.Null, "IFork parameter");

			switch (StaticVariable) {
			case 1:
				ctx.Assert (IsForked, Is.EqualTo (2).Or.EqualTo (6), nameof (IsForked));
				break;
			case 3:
				// remote init
				ctx.Assert (IsForked, Is.EqualTo (6), nameof (IsForked));
				break;
			default:
				throw ctx.AssertFail (nameof (StaticVariable));
			}

			ctx.Assert (Interlocked.Increment (ref postRunCalled), Is.EqualTo (1));
			return FinishedTask;
		}

		public Task Destroy (TestContext ctx, CancellationToken cancellationToken)
		{
			ctx.LogMessage ($"{ME} DESTROY: {StaticVariable} {IsForked}");

			switch (StaticVariable) {
			case 1:
				ctx.Assert (IsForked, Is.EqualTo (2).Or.EqualTo (6), nameof (IsForked));
				break;
			case 3:
				// remote init
				ctx.Assert (IsForked, Is.EqualTo (6), nameof (IsForked));
				break;
			default:
				throw ctx.AssertFail (nameof (StaticVariable));
			}

			return FinishedTask;
		}
	}
}
