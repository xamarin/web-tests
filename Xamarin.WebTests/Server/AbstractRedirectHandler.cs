//
// AbstractRedirectHandler.cs
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
using System.Net;

namespace Xamarin.WebTests.Server
{
	using Framework;

	public abstract class AbstractRedirectHandler : Handler
	{
		public Handler Target {
			get;
			private set;
		}

		protected AbstractRedirectHandler (Handler target)
		{
			Target = target;

			if ((target.Flags & RequestFlags.SendContinue) != 0)
				Flags |= RequestFlags.SendContinue;
			else
				Flags &= ~RequestFlags.SendContinue;

			Description = string.Format ("{0}: {1}", GetType ().Name, target.Description);
		}

		public override HttpWebRequest CreateRequest (Uri uri)
		{
			return Target.CreateRequest (uri);
		}

		protected abstract bool HandleRedirect (Connection connection, RequestFlags effectiveFlags);

		protected override bool DoHandleRequest (Connection connection, RequestFlags effectiveFlags)
		{
			var sendContinue = (effectiveFlags & RequestFlags.SendContinue) != 0;
			var postHandler = Target as PostHandler;
			if (!sendContinue && postHandler != null) {
				if (!postHandler.HandlePostRequest (connection, effectiveFlags))
					return false;
			}

			return HandleRedirect (connection, effectiveFlags);
		}
	}
}

