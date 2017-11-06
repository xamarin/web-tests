//
// FormContent.cs
//
// Author:
//       Martin Baulig <mabaul@microsoft.com>
//
// Copyright (c) 2017 Xamarin Inc. (http://www.xamarin.com)
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
using System.Text;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using Xamarin.AsyncTests;

namespace Xamarin.WebTests.HttpFramework
{
	public class FormContent : HttpContent
	{
		List<(string Key, string Value)> elements;

		public FormContent (params (string,string)[] args)
		{
			elements = new List<(string, string)> ();
			elements.AddRange (args);
		}

		public override bool HasLength => false;

		public override int Length => throw new NotImplementedException ();

		public override void AddHeadersTo (HttpMessage message)
		{
			message.ContentType = "application/x-www-form-urlencoded";
		}

		public override byte[] AsByteArray ()
		{
			return Encoding.UTF8.GetBytes (String.Join ("&", elements.Select (p => p.Key + "=" + Uri.EscapeDataString (p.Value))));
		}

		public override string AsString ()
		{
			throw new NotImplementedException ();
		}

		public override async Task WriteToAsync (TestContext ctx, Stream stream)
		{
			var bytes = AsByteArray ();
			await stream.WriteAsync (bytes, 0, bytes.Length).ConfigureAwait (false);
		}

		public override Task WriteToAsync (TestContext ctx, StreamWriter writer)
		{
			throw new NotImplementedException ();
		}
	}
}
