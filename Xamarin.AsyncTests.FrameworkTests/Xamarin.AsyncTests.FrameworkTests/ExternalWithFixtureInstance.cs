//
// ExternalWithFixtureInstance.cs
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
using System.Xml.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xamarin.AsyncTests.Constraints;

namespace Xamarin.AsyncTests.FrameworkTests
{
	[AsyncTestFixture (Prefix = "FrameworkTests")]
	public class ExternalWithFixtureInstance : IForkedTestInstance
	{
		public int ID {
			get;
		}

		static int nextId;

		public ExternalWithFixtureInstance ()
		{
			ID = Interlocked.Increment (ref nextId);
			System.Diagnostics.Debug.WriteLine ($"CONSTRUCTOR: {ID}");
		}

		[AsyncTest]
		// [Martin (null, UseFixtureName = true)]
		public void Test (
			TestContext ctx, [Fork (ForkType.Internal)] IFork fork)
		{
			ctx.LogMessage ($"Martin Test: {ctx.FriendlyName} {fork}");
			ctx.LogMessage ($"INSTANCE: {ID} {IsForked}");
			ctx.Assert (ID, Is.EqualTo (2));
			ctx.Assert (IsForked);
		}

		public bool IsForked {
			get;
			private set;
		}

		public bool Serialize (TestContext ctx, XElement element)
		{
			element.Add (new XElement ("Hello"));
			element.Add (new XElement ("World"));
			ctx.LogMessage ($"MY HOST SERIALIZE: {ID}");
			return true;
		}

		public void Deserialize (TestContext ctx, XElement element)
		{
			IsForked = true;
			ctx.LogMessage ($"MY HOST DESERIALIZE: {ID} {element}");
		}
	}
}
