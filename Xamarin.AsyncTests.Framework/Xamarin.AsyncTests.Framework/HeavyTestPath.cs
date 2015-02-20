//
// HeavyTestPath.cs
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
using System.Xml.Linq;
using System.Reflection;

namespace Xamarin.AsyncTests.Framework
{
	sealed class HeavyTestPath : TestPath
	{
		public string Name {
			get;
			private set;
		}

		public string HostName {
			get;
			private set;
		}

		public TestFlags Flags {
			get;
			private set;
		}

		public HeavyTestPath (HeavyTestHost host, TestPath parent)
			: base (host.TypeKey, parent)
		{
			Name = host.Name;
			HostName = GetHostTypeName (host.Type);
			Flags = host.Flags;
		}

		internal override bool Serialize (XElement node)
		{
			return true;
		}

		protected override void GetTestName (TestNameBuilder builder)
		{
			base.GetTestName (builder);
			if (Name != null)
				builder.PushParameter (Name, HostName, (Flags & TestFlags.Hidden) != 0);
		}

		static string GetHostTypeName (Type type)
		{
			var friendlyAttr = type.GetTypeInfo ().GetCustomAttribute<FriendlyNameAttribute> ();
			if (friendlyAttr != null)
				return friendlyAttr.Name;
			return type.FullName;
		}
	}
}

