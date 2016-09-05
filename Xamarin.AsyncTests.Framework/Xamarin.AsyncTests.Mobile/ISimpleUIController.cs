//
// ISimpleUIController.cs
//
// Author:
//       Martin Baulig <martin.baulig@xamarin.com>
//
// Copyright (c) 2016 Xamarin, Inc.
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
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace Xamarin.AsyncTests.Mobile
{
	public interface ISimpleUIController
	{
		void DebugMessage (string message);

		void Message (string message);

		void Message (string format, params object[] args);

		void StatusMessage (string message);

		void StatusMessage (string format, params object[] args);

		void StatisticsMessage (string message);

		void StatisticsMessage (string format, params object [] args);

		bool IsRunning {
			get; set;
		}

		bool CanRun {
			get; set;
		}

		IList<string> Categories {
			get;
		}

		void SetCategories (IList<string> categories, int selected);

		int SelectedCategory {
			get; set;
		}

		event EventHandler SessionChangedEvent;

		event EventHandler<int> CategoryChangedEvent;
	}
}

