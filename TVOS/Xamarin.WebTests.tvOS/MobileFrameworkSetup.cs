//
// MobileFrameworkSetup.cs
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

namespace Xamarin.WebTests.tvOS
{
	using MonoTestProvider;
	using ConnectionFramework;
	using MonoConnectionFramework;

	class MobileFrameworkSetup : MonoConnectionFrameworkSetup
	{
		public override string Name {
			get { return "Xamarin.WebTests.tvOS"; }
		}


		public override string TlsProviderName {
			get {
#if APPLETLS
				return "appletls";
#else
				return "old";
#endif
			}
		}

		public override Guid TlsProvider {
			get {
#if APPLETLS
				return ConnectionProviderFactory.AppleTlsGuid;
#else
				return ConnectionProviderFactory.MobileLegacyTlsGuid;
#endif
			}
		}

		public override ISslStreamProvider DefaultSslStreamProvider {
			get {
				return null;
			}
		}

		public override bool SupportsTls12 {
			get {
#if APPLETLS
				return true;
#else
				return false;
#endif
			}
		}
	}
}

