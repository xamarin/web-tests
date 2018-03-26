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

		public HttpServerProviderFilter (HttpServerTestCategory category)
		{
			Category = category;
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

		bool HasNewWebStack ()
		{
			var setup = DependencyInjector.Get<IConnectionFrameworkSetup> ();
			return setup.UsingDotNet || setup.HasNewWebStack;
		}

		bool SupportsGZip ()
		{
			var setup = DependencyInjector.Get<IConnectionFrameworkSetup> ();
			return setup.SupportsGZip;
		}

		bool IsSupported (TestContext ctx, ConnectionProvider provider)
		{
			if (!provider.HasFlag (ConnectionProviderFlags.SupportsHttp))
				return false;
			switch (Category) {
			case HttpServerTestCategory.Default:
				return true;
			case HttpServerTestCategory.Stress:
			case HttpServerTestCategory.Instrumentation:
				return SupportsSsl (provider);
			case HttpServerTestCategory.NewWebStackInstrumentation:
			case HttpServerTestCategory.Experimental:
				return HasNewWebStack () && SupportsSsl (provider);
			case HttpServerTestCategory.NewWebStack:
				return HasNewWebStack ();
			case HttpServerTestCategory.HttpListener:
				return SupportsHttpListener (provider);
			case HttpServerTestCategory.MartinTest:
				return SupportsSsl (provider);
			case HttpServerTestCategory.RecentlyFixed:
				return HasNewWebStack () && SupportsSsl (provider);
			case HttpServerTestCategory.GZip:
				return SupportsGZip ();
			case HttpServerTestCategory.GZipInstrumentation:
				return SupportsGZip () && SupportsSsl (provider);
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
			if (providers.Count == 0 && !Optional)
				throw ctx.AssertFail ($"No supported ConnectionProvider for `{Category}'.");
			return providers;
		}

		bool Optional {
			get {
				switch (Category) {
				case HttpServerTestCategory.GZip:
				case HttpServerTestCategory.GZipInstrumentation:
				case HttpServerTestCategory.Ignore:
					return true;
				default:
					return false;
				}
			}
		}

		bool UsingSsl {
			get {
				switch (Category) {
				case HttpServerTestCategory.HttpListener:
					return false;
				default:
					return true;
				}
			}
		}

		bool RequireSsl {
			get {
				switch (Category) {
				case HttpServerTestCategory.Instrumentation:
				case HttpServerTestCategory.NewWebStackInstrumentation:
				case HttpServerTestCategory.RecentlyFixed:
				case HttpServerTestCategory.GZipInstrumentation:
					return true;
				default:
					return false;
				}
			}
		}

		bool UsingHttpListener {
			get {
				switch (Category) {
				case HttpServerTestCategory.HttpListener:
					return true;
				default:
					return false;
				}
			}
		}

		bool IsMartinTest {
			get {
				switch (Category) {
				case HttpServerTestCategory.MartinTest:
					return true;
				default:
					return false;
				}
			}
		}

		bool SupportsHttp {
			get {
				switch (Category) {
				case HttpServerTestCategory.Default:
					return true;
				case HttpServerTestCategory.NewWebStack:
					return HasNewWebStack ();
				case HttpServerTestCategory.GZip:
					return SupportsGZip ();
				case HttpServerTestCategory.Ignore:
					return false;
				default:
					return false;
				}
			}
		}

		public IEnumerable<HttpServerProvider> GetProviders (TestContext ctx)
		{
			HttpServerFlags serverFlags = HttpServerFlags.None;
			if (UsingHttpListener)
				serverFlags |= HttpServerFlags.HttpListener;

			if (IsMartinTest) {
				yield return new HttpServerProvider ("https", serverFlags, null);
				yield break;
			}

			if (!RequireSsl && SupportsHttp) {
				yield return new HttpServerProvider (
					"http", serverFlags | HttpServerFlags.NoSSL, null);
			}

			if (!UsingSsl)
				yield break;

			var supportedProviders = GetSupportedProviders (ctx);
			if (supportedProviders.Count () == 0 && !Optional)
				ctx.AssertFail ("Could not find any supported HttpServerProvider.");

			serverFlags |= HttpServerFlags.SSL;
			foreach (var provider in supportedProviders) {
				yield return new HttpServerProvider (
					$"https:{provider.Name}", serverFlags,
					provider.SslStreamProvider);
			}
		}
	}
}

