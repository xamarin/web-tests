//
// ChunkedOperationTypeAttribute.cs
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
using System.Collections.Generic;
using Xamarin.AsyncTests;
using Xamarin.AsyncTests.Portable;

namespace Xamarin.WebTests.HttpOperations
{
	using TestFramework;

	[AttributeUsage (AttributeTargets.Parameter | AttributeTargets.Property, AllowMultiple = false)]
	public class ChunkedOperationTypeAttribute : TestParameterAttribute, ITestParameterSource<ChunkedOperationType>
	{
		public ChunkedOperationTypeAttribute (string filter = null, TestFlags flags = TestFlags.Browsable | TestFlags.ContinueOnError)
			: base (filter, flags)
		{
		}

		public ChunkedOperationTypeAttribute (ChunkedOperationType type, TestFlags flags = TestFlags.Browsable | TestFlags.ContinueOnError)
			: base (null, flags)
		{
			Type = type;
		}

		public ChunkedOperationType? Type {
			get;
			private set;
		}

		public bool ServerError {
			get; set;
		}

		public IEnumerable<ChunkedOperationType> GetParameters (TestContext ctx, string filter)
		{
			if (Type != null) {
				yield return Type.Value;
				yield break;
			}

			var includeNotWorking = ctx.IsEnabled (IncludeNotWorkingAttribute.Instance) || ctx.CurrentCategory == NotWorkingAttribute.Instance;

			if (ServerError) {
				if (includeNotWorking)
					yield return ChunkedOperationType.SyncReadTimeout;
				yield break;
			}

			yield return ChunkedOperationType.SyncRead;
			yield return ChunkedOperationType.NormalChunk;

			var support = DependencyInjector.Get<IPortableSupport> ();
			if (!support.IsMicrosoftRuntime) {
				// Doesn't work on .NET.
				yield return ChunkedOperationType.ServerAbort;
			}

			if (includeNotWorking) {
				yield return ChunkedOperationType.TruncatedChunk;
				yield return ChunkedOperationType.MissingTrailer;
				yield return ChunkedOperationType.BeginEndAsyncRead;
			}

			yield return ChunkedOperationType.BeginEndAsyncReadNoWait;
		}
	}
}

