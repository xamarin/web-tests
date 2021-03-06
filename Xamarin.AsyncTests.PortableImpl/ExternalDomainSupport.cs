﻿//
// ExternalDomainSupport.cs
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
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using System.Reflection;

namespace Xamarin.AsyncTests.Portable
{
	using Framework;
	using Remoting;

#if __IOS__
	[Foundation.Preserve (AllMembers = true)]
#endif
	class ExternalDomainSupport : MarshalByRefObject, IExternalDomainSupport
	{
		public Assembly Assembly {
			get;
		}

		public Assembly[] Dependencies {
			get;
		}

		public IForkedDomainSetup DomainSetup {
			get;
		}

		public ExternalDomainSupport (Assembly assembly, Assembly[] dependencies)
		{
			Assembly = assembly;
			Dependencies = dependencies;
			if (DependencyInjector.TryGet<IForkedDomainSetup> (out var setup))
				DomainSetup = setup;
		}

		public virtual IExternalDomainHost Create (TestApp app, string name)
		{
			var domain = AppDomain.CreateDomain (name);
			var host = new ExternalDomainHost (app);
			var server = (ExternalDomainServer)domain.CreateInstanceAndUnwrap (
				typeof (ExternalDomainServer).Assembly.FullName,
				typeof (ExternalDomainServer).FullName,
				false, BindingFlags.CreateInstance | BindingFlags.NonPublic |
				BindingFlags.Instance,
				null, new object[] { this, host }, null, null);
			host.Initialize (domain, server);
			return host;
		}
	}
}
