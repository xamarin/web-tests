//
// ExternalServerRegistration.cs
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
using System.Xml.Linq;
using System.Collections.Generic;
using Xamarin.AsyncTests;
using Xamarin.AsyncTests.Portable;

namespace Xamarin.WebTests.ExternalServer
{
	using System.Threading;
	using System.Threading.Tasks;
	using HttpFramework;
	using Server;

	public sealed class ExternalServerRegistration : ITestParameter
	{
		public string Name {
			get;
		}

		public HttpServerFlags Flags {
			get;
		}

		public int ParallelConnections {
			get;
		}

		string ITestParameter.Value => Name;

		string ITestParameter.FriendlyValue => Name;

		internal ExternalServerRegistration (string name, int parallel = 0, HttpServerFlags flags = HttpServerFlags.None)
		{
			Name = name;
			Flags = flags;
			ParallelConnections = parallel;

			HandlerRegistry = new Registry<ExternalServerHandler> ();
		}

		internal ExternalServerRegistration (XElement element)
		{
			Name = element.Attribute ("Name").Value;
			Uri = new Uri (element.Attribute ("Uri").Value);
			Flags = HttpServerFlags.None;
			ParallelConnections = int.Parse (element.Attribute ("ParallelConnections").Value);
			if (!Enum.TryParse<HttpServerFlags> (element.Attribute ("EffectiveFlags").Value, out var flags))
				flags = HttpServerFlags.None;
			EffectiveFlags = flags;

			HandlerRegistry = new Registry<ExternalServerHandler> ();

			foreach (var handlerElement in element.Elements ("Handler")) {
				var handler = new ExternalServerHandler (handlerElement);
				HandlerRegistry.Add (handler.Value, handler);
			}

			Initialized = true;
		}

		public HttpServerFlags EffectiveFlags {
			get;
			internal set;
		}

		public IPortableEndPoint EndPoint {
			get;
			internal set;
		}

		public Uri Uri {
			get;
			internal set;
		}

		public BuiltinHttpServer Server {
			get;
			internal set;
		}

		internal bool Initialized {
			get; set;
		}

		public XElement Serialize ()
		{
			var element = new XElement ("Server");
			element.Add (new XAttribute ("Name", Name));
			element.Add (new XAttribute ("Uri", Uri));
			element.Add (new XAttribute ("EffectiveFlags", EffectiveFlags));
			element.Add (new XAttribute ("ParallelConnections", ParallelConnections));
			foreach (var handler in HandlerRegistry)
				element.Add (handler.Serialize ());
			return element;
		}

		public Registry<ExternalServerHandler> HandlerRegistry {
			get;
		}

		internal void AddHandler (ExternalServerHandler handler)
		{
			if (Initialized)
				throw new InternalErrorException ();
			HandlerRegistry.Add (handler.Value, handler);
		}

		public ExternalServerRegistration RegisterHandler (
			string name, RequestFlags flags, Func<TestContext, HttpConnection, HttpRequest, HttpResponse> func)
		{
			return RegisterHandler (name, flags, (ctx, c, r, __) => Task.FromResult (func (ctx, c, r)));
		}

		public ExternalServerRegistration RegisterHandler (
			string name, RequestFlags flags, Func<TestContext, HttpRequest, HttpResponse> func)
		{
			return RegisterHandler (name, flags, (ctx, _, r, __) => Task.FromResult (func (ctx, r)));
		}

		public ExternalServerRegistration RegisterHandler (
			string name, RequestFlags flags, Func<HttpConnection, HttpRequest, HttpResponse> func)
		{
			return RegisterHandler (name, flags, (_, c, r, __) => Task.FromResult (func (c, r)));
		}

		public ExternalServerRegistration RegisterHandler (
			string name, RequestFlags flags, Func<HttpRequest, HttpResponse> func)
		{
			return RegisterHandler (name, flags, (_, __, r, ___) => Task.FromResult (func (r)));
		}

		public ExternalServerRegistration RegisterHandler (string name, RequestFlags flags, ExternalServerHandlerDelegate func)
		{
			var handler = new DelegateHandler (name, flags, func);
			AddHandler (new ExternalServerHandler (handler));
			return this;
		}

		class DelegateHandler : ListenerHandler
		{
			public ExternalServerHandlerDelegate Delegate {
				get;
			}

			public string Value {
				get;
			}

			public RequestFlags RequestFlags {
				get;
			}

			public DelegateHandler (string name, RequestFlags flags, ExternalServerHandlerDelegate func)
			{
				Value = name;
				RequestFlags = flags;
				Delegate = func;
			}

			public Task<HttpResponse> HandleRequest (TestContext ctx, HttpOperation operation, HttpConnection connection, HttpRequest request, RequestFlags effectiveFlags, CancellationToken cancellationToken)
			{
				return Delegate (ctx, connection, request, cancellationToken);
			}
		}

		public override string ToString () => $"[ExternalServerRegistration {Name}]";
	}
}
