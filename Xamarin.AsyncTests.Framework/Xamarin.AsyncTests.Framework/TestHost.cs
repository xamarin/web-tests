//
// TestHost.cs
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
using System.Threading;
using System.Threading.Tasks;

namespace Xamarin.AsyncTests.Framework
{
	abstract class TestHost : IPathNode
	{
		public TestFlags Flags {
			get; protected set;
		}

		public virtual string TypeKey {
			get { return GetType ().FullName; }
		}

		public string Identifier {
			get;
			private set;
		}

		public string Name {
			get;
			private set;
		}

		public string ParameterType {
			get;
			private set;
		}

		protected TestHost (string identifier, string name, string parameterType, TestFlags flags = TestFlags.None)
		{
			Identifier = identifier;
			Name = name;
			ParameterType = parameterType;
			Flags = flags;
		}

		internal TestInstance CreateInstance (TestContext ctx, TestInstance parent)
		{
			var instance = CreateInstance (parent);
			instance.Initialize (ctx);
			return instance;
		}

		internal abstract ITestParameter GetParameter (TestInstance instance);

		internal abstract TestInvoker Deserialize (XElement node, TestInvoker invoker);

		internal abstract TestInstance CreateInstance (TestInstance parent);

		internal abstract TestInvoker CreateInvoker (TestInvoker invoker);

		public override string ToString ()
		{
			return string.Format ("[{0}: Flags={1}]", GetType ().Name, Flags);
		}
	}
}

