//
// ForkAttribute.cs
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

namespace Xamarin.AsyncTests
{
	[AttributeUsage (AttributeTargets.Class | AttributeTargets.Method | AttributeTargets.Parameter, AllowMultiple = false)]
	public class ForkAttribute : Attribute
	{
		public int Count {
			get;
		}

		public ForkType Type {
			get;
		}

		public int RandomDelay {
			get; set;
		}

		public string DomainName {
			get; set;
		}

		public void SanityCheck (TestContext ctx)
		{
			if (DomainName != null && Type != ForkType.Domain)
				throw ctx.AssertFail ($"Cannot use 'DomainName' with '{Type}'.");
			if (RandomDelay != 0 && Type != ForkType.Fork)
				throw ctx.AssertFail ($"Cannot use 'RandomDelay' with '{Type}'.");
			if (Count != 0 && Type != ForkType.Fork)
				throw ctx.AssertFail ($"Cannot use 'Count' with '{Type}'.");
		}

		public ForkAttribute (int count)
		{
			Count = count;
			Type = ForkType.Task;
		}

		public ForkAttribute (ForkType type)
		{
			Type = type;
		}

		public override string ToString ()
		{
			return $"[ForkAttribute({Type}): Count={Count}, RandomDelay={RandomDelay}]";
		}
	}
}
