//
// ForkedTestHost.cs
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
using System.Xml.Linq;

namespace Xamarin.AsyncTests.Framework
{
	class ForkedTestHost : TestHost
	{
		public ForkAttribute Attribute {
			get;
		}

		public TestHost ParameterHost {
			get;
		}

		public ForkedTestHost (string name, ForkAttribute attr, TestHost host = null)
			: base (TestPathType.Fork, name, name, "IFork")
		{
			Attribute = attr;
			ParameterHost = host;
		}

		internal static object GetInstance (ref TestInstance instance)
		{
			var forkedInstance = (ForkedTestInstance)instance;
			instance = instance.Parent;

			if (forkedInstance.Host.ParameterHost == null)
				return forkedInstance;

			var heavyInstance = (HeavyTestInstance)instance;
			instance = instance.Parent;
			return heavyInstance.Instance;
		}

		#region implemented abstract members of TestHost

		internal override TestInstance CreateInstance (TestContext ctx, TestNode node, TestInstance parent)
		{
			var id = long.Parse (node.Parameter.Value);
			var forkedInstance = new ForkedTestInstance (this, node, parent, id);
			return forkedInstance;
		}

		internal override TestInvoker CreateInvoker (TestNode node, TestInvoker invoker, TestFlags flags)
		{
			return new ForkedTestInvoker (this, node, invoker);
		}

		#endregion
	}
}

