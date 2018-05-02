//
// ExternalServerHandler.cs
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
using Xamarin.AsyncTests;

namespace Xamarin.WebTests.ExternalServer
{
	using HttpFramework;
	using Server;

	public sealed class ExternalServerHandler : ITestParameter
	{
		public string Value {
			get;
		}

		string ITestParameter.FriendlyValue => Value;

		public RequestFlags RequestFlags {
			get;
		}

		public Uri Uri {
			get;
			internal set;
		}

		internal ExternalServerHandler (ListenerHandler handler)
		{
			Handler = handler;
			Value = handler.Value;
			RequestFlags = handler.RequestFlags;
		}

		internal ExternalServerHandler (XElement element)
		{
			Value = element.Attribute ("Value").Value;
			Uri = new Uri (element.Attribute ("Uri").Value);
			if (!Enum.TryParse<RequestFlags> (element.Attribute ("RequestFlags").Value, out var flags))
				flags = RequestFlags.None;
		}

		public XElement Serialize ()
		{
			var element = new XElement ("Handler");
			element.Add (new XAttribute ("Value", Value));
			element.Add (new XAttribute ("Uri", Uri));
			element.Add (new XAttribute ("RequestFlags", RequestFlags));
			return element;
		}

		internal ListenerOperation Operation {
			get; set;
		}

		internal ListenerHandler Handler {
			get;
		}

		public override string ToString () => $"[ExternalServerHandler {Value}]";
	}
}
