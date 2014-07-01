//
// CapturedTestInstance.cs
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
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Xamarin.AsyncTests.Framework
{
	class CapturedTestInstance : ParameterizedTestInstance
	{
		bool hasNext;
		object current;

		new public CapturedTestHost Host {
			get { return (CapturedTestHost)base.Host; }
		}

		public CapturedTestInstance (CapturedTestHost host, TestInstance parent)
			: base (host, parent)
		{
		}

		#region implemented abstract members of ParameterizedTestInstance

		public override void Initialize (TestContext context)
		{
			current = Host.CapturedInstance;
			var cloneable = current as ICloneable;
			if (cloneable != null)
				current = cloneable.Clone ();
			hasNext = true;
			base.Initialize (context);
		}

		public override bool HasNext ()
		{
			return hasNext;
		}

		public override bool MoveNext (TestContext context)
		{
			if (!hasNext)
				return false;
			hasNext = false;
			return true;
		}

		public override object Current {
			get { return current; }
		}

		#endregion
	}
}

