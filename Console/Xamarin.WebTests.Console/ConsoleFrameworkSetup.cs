//
// ConsoleFrameworkSetup.cs
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
using System.Net;
using System.Security.Cryptography.X509Certificates;
using Mono.Security.Interface;

namespace Xamarin.WebTests.Console
{
	using TestFramework;
	using MonoTestProvider;
	using MonoConnectionFramework;
	using ConnectionFramework;

	class ConsoleFrameworkSetup : MonoConnectionFrameworkSetup
	{
		DotNetSslStreamProvider dotNetStreamProvider;

		public ConsoleFrameworkSetup ()
		{
			dotNetStreamProvider = new DotNetSslStreamProvider ();
		}

		public override string Name {
			get { return "Xamarin.WebTests.Console"; }
		}

		public override string TlsProviderName {
			get {
				return "legacy";
			}
		}

		public override Guid TlsProvider {
			get {
				return ConnectionProviderFactory.LegacyTlsGuid;
			}
		}

		public override ISslStreamProvider DefaultSslStreamProvider {
			get { return dotNetStreamProvider; }
		}

		public override bool SupportsTls12 {
			get {
#if BTLS
				return true;
#else
				return false;
#endif
			}
		}
	}
}

