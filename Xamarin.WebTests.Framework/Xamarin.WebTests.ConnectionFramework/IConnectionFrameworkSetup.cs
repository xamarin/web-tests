﻿//
// IConnectionFrameworkSetup.cs
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
using System.Net.Security;
using System.Threading;
using System.Threading.Tasks;
using Xamarin.AsyncTests;

namespace Xamarin.WebTests.ConnectionFramework
{
	public interface IConnectionFrameworkSetup : ISingletonInstance
	{
		string Name {
			get;
		}

		bool InstallDefaultCertificateValidator {
			get;
		}

		bool SupportsTls12 {
			get;
		}

		bool SupportsCleanShutdown {
			get;
		}

		bool UsingAppleTls {
			get;
		}

		bool UsingBtls {
			get;
		}

		bool HasNewWebStack {
			get;
		}

		bool UsingDotNet {
			get;
		}

		bool SupportsGZip {
			get;
		}

		int InternalVersion {
			get;
		}

		void Initialize (ConnectionProviderFactory factory);

		Task ShutdownAsync (SslStream stream);

		bool SupportsRenegotiation {
			get;
		}

		bool CanRenegotiate (SslStream stream);

		Task RenegotiateAsync (SslStream stream, CancellationToken cancellationToken);
	}
}

