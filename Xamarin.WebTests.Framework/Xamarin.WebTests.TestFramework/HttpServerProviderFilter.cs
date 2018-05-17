//
// HttpServerProviderFilter.cs
//
// Author:
//       Martin Baulig <mabaul@microsoft.com>
//
// Copyright (c) 2018 Xamarin Inc. (http://www.xamarin.com)
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
using System.Linq;
using System.Collections.Generic;
using Xamarin.AsyncTests;
using Xamarin.AsyncTests.Constraints;

namespace Xamarin.WebTests.TestFramework
{
	using ConnectionFramework;
	using HttpFramework;

	public class HttpServerProviderFilter
	{
		public HttpServerTestCategory Category {
			get;
		}

		public HttpServerFlags ServerFlags {
			get;
		}

		public HttpServerProviderFilter (TestContext ctx, HttpServerTestCategory category)
		{
			Category = category;

			var setup = DependencyInjector.Get<IConnectionFrameworkSetup> ();
			HasNewWebStack = setup.UsingDotNet || setup.HasNewWebStack;
			SupportsGZip = setup.SupportsGZip;
			IsLegacy = !setup.SupportsTls12;

			if (!ctx.TryGetParameter (out HttpServerFlags serverFlags))
				serverFlags = HttpServerFlags.None;
			ServerFlags = serverFlags;

			UsingHttpListener |= (ServerFlags & HttpServerFlags.HttpListener) != 0;
			RequireRenegotiation = (ServerFlags & HttpServerFlags.RequireRenegotiation) != 0;
			RequireSsl |= RequireRenegotiation;

			switch (Category) {
			case HttpServerTestCategory.HttpListener:
				UsingHttpListener = true;
				SupportsHttp = true;
				DisableSsl = true;
				break;
			case HttpServerTestCategory.MartinTest:
				IsMartinTest = true;
				Optional = true;
				break;
			case HttpServerTestCategory.GZip:
				SupportsHttp = SupportsGZip;
				Optional = true;
				break;
			case HttpServerTestCategory.GZipInstrumentation:
				Optional = true;
				RequireSsl = true;
				break;
			case HttpServerTestCategory.Ignore:
				Optional = true;
				break;
			case HttpServerTestCategory.Default:
				SupportsHttp = true;
				break;
			case HttpServerTestCategory.NewWebStack:
				SupportsHttp = HasNewWebStack;
				Optional = true;
				break;
			case HttpServerTestCategory.Instrumentation:
				RequireSsl = true;
				break;
			case HttpServerTestCategory.NewWebStackInstrumentation:
			case HttpServerTestCategory.RecentlyFixed:
				RequireSsl = true;
				Optional = true;
				break;
			}
		}

		bool SupportsSsl (ConnectionProvider provider)
		{
			if (!provider.SupportsSslStreams)
				return false;
			if (!provider.HasFlag (ConnectionProviderFlags.SupportsTls12))
				return false;
			return true;
		}

		bool SupportsHttpListener (ConnectionProvider provider)
		{
			return provider.HasFlag (ConnectionProviderFlags.SupportsHttpListener);
		}

		bool IsSupported (TestContext ctx, ConnectionProvider provider)
		{
			if (!provider.HasFlag (ConnectionProviderFlags.SupportsHttp))
				return false;
			if (UsingHttpListener && !provider.HasFlag (ConnectionProviderFlags.SupportsHttpListener))
				return false;
			if (RequireRenegotiation && !provider.HasFlag (ConnectionProviderFlags.SupportsServerRenegotiation))
				return false;
			switch (Category) {
			case HttpServerTestCategory.Default:
				return true;
			case HttpServerTestCategory.Stress:
			case HttpServerTestCategory.Instrumentation:
				return SupportsSsl (provider);
			case HttpServerTestCategory.NewWebStackInstrumentation:
			case HttpServerTestCategory.Experimental:
				return HasNewWebStack && SupportsSsl (provider);
			case HttpServerTestCategory.NewWebStack:
				return HasNewWebStack;
			case HttpServerTestCategory.HttpListener:
				return SupportsHttpListener (provider);
			case HttpServerTestCategory.MartinTest:
				return SupportsSsl (provider);
			case HttpServerTestCategory.RecentlyFixed:
				return HasNewWebStack && SupportsSsl (provider);
			case HttpServerTestCategory.GZip:
				return SupportsGZip;
			case HttpServerTestCategory.GZipInstrumentation:
				return SupportsGZip && SupportsSsl (provider);
			case HttpServerTestCategory.Ignore:
				return false;
			default:
				throw ctx.AssertFail (Category);
			}
		}

		public IEnumerable<ConnectionProvider> GetSupportedProviders (TestContext ctx)
		{
			var factory = DependencyInjector.Get<ConnectionProviderFactory> ();
			var providers = factory.GetProviders (p => IsSupported (ctx, p)).ToList ();
			if (providers.Count == 0 && !Optional && !IsLegacy)
				throw ctx.AssertFail ($"No supported ConnectionProvider for `{Category}'.");
			return providers;
		}

		bool HasNewWebStack {
			get;
		}

		bool SupportsGZip {
			get;
		}

		bool Optional {
			get;
		}

		bool IsLegacy {
			get;
		}

		bool DisableSsl {
			get;
		}

		bool RequireSsl {
			get;
		}

		bool UsingHttpListener {
			get;
		}

		bool IsMartinTest {
			get;
		}

		bool SupportsHttp {
			get;
		}

		bool RequireRenegotiation {
			get;
		}

		public IEnumerable<HttpServerProvider> GetProviders (TestContext ctx)
		{
			var setup = DependencyInjector.Get<IConnectionFrameworkSetup> ();
			if ((ServerFlags & HttpServerFlags.InternalVersionTwo) != 0) {
				if (!setup.UsingDotNet && setup.InternalVersion < 2)
					yield break;
			}

			if (setup.UsingDotNet && (ServerFlags & HttpServerFlags.RequireMono) != 0)
				yield break;

			if (!RequireSsl && !IsMartinTest && SupportsHttp) {
				yield return new HttpServerProvider (
					"http", ServerFlags | HttpServerFlags.NoSSL, null);
			}

			if (DisableSsl)
				yield break;

			var supportedProviders = GetSupportedProviders (ctx);
			if (!supportedProviders.Any () && !Optional && !IsLegacy)
				ctx.AssertFail ("Could not find any supported HttpServerProvider.");

			foreach (var provider in supportedProviders) {
				yield return new HttpServerProvider (
					$"https:{provider.Name}",
					ServerFlags | HttpServerFlags.SSL,
					provider.SslStreamProvider);
				if (IsMartinTest)
					yield break;
			}
		}
	}
}

