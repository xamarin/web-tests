//
// ConnectionTestParameters.cs
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
using System.Security.Cryptography.X509Certificates;
using Xamarin.AsyncTests;

namespace Xamarin.WebTests.TestFramework
{
	using ConnectionFramework;

	public abstract class ConnectionTestParameters : ConnectionParameters, ITestParameter
	{
		public string Identifier {
			get;
		}

		string ITestParameter.Value => Identifier;

		string ITestParameter.FriendlyValue => Identifier;

		public ConnectionTestCategory Category {
			get;
		}

		protected ConnectionTestParameters (ConnectionTestCategory category, string identifier, X509Certificate certificate)
			: base (certificate)
		{
			Category = category;
			Identifier = identifier;
		}

		protected ConnectionTestParameters (ConnectionTestParameters other)
			: base (other)
		{
			Category = other.Category;
		}
	}
}

