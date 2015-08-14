//
// DefaultConnectionProviderFactory.cs
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
using System.Net;
using System.Collections.Generic;
using System.Security.Authentication;
using Xamarin.AsyncTests;
using Xamarin.AsyncTests.Portable;

namespace Xamarin.WebTests.ConnectionFramework
{
	using Server;
	using Providers;

	class DefaultConnectionProviderFactory : ConnectionProviderFactory
	{
		readonly DotNetSslStreamProvider dotNetStreamProvider;
		readonly DefaultHttpProvider defaultHttpProvider;
		readonly DotNetConnectionProvider defaultConnectionProvider;
		readonly DotNetConnectionProvider newTlsConnectionProvider;
		readonly DotNetConnectionProvider platformDefaultConnectionProvider;

		internal DefaultConnectionProviderFactory ()
		{
			dotNetStreamProvider = new DotNetSslStreamProvider ();
			defaultHttpProvider = new DefaultHttpProvider (dotNetStreamProvider);

			defaultConnectionProvider = new DotNetConnectionProvider (this, ConnectionProviderType.DotNet, dotNetStreamProvider, defaultHttpProvider);
			Install (defaultConnectionProvider);

			var support = DependencyInjector.Get<IPortableSupport> ();
			if (support.IsMicrosoftRuntime) {
				newTlsConnectionProvider = new DotNetConnectionProvider (this, ConnectionProviderType.NewTLS, dotNetStreamProvider, defaultHttpProvider);
				Install (newTlsConnectionProvider);
			}

			platformDefaultConnectionProvider = new DotNetConnectionProvider (this, ConnectionProviderType.PlatformDefault, dotNetStreamProvider, defaultHttpProvider);
			Install (platformDefaultConnectionProvider);

			var manual = new ManualConnectionProvider (this, ConnectionProviderFlags.IsExplicit);
			Install (manual);
		}

		public override IHttpProvider DefaultHttpProvider {
			get { return defaultHttpProvider; }
		}

		public override ISslStreamProvider DefaultSslStreamProvider {
			get { return dotNetStreamProvider; }
		}
	}
}

