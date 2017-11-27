//
// HttpClientTestRunner.cs
//
// Author:
//       Martin Baulig <mabaul@microsoft.com>
//
// Copyright (c) 2017 Xamarin Inc. (http://www.xamarin.com)
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
using System.IO;
using System.Text;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using System.Collections;
using System.Collections.Generic;
using Xamarin.AsyncTests;
using Xamarin.AsyncTests.Constraints;
using Xamarin.AsyncTests.Portable;

namespace Xamarin.WebTests.TestRunners
{
	using ConnectionFramework;
	using HttpFramework;
	using HttpHandlers;
	using TestFramework;
	using HttpClient;
	using Resources;

	[HttpClientTestRunner]
	public class HttpClientTestRunner : AbstractConnection
	{
		public ConnectionTestProvider Provider {
			get;
		}

		protected Uri Uri {
			get;
		}

		protected HttpServerFlags ServerFlags {
			get;
		}

		new public HttpClientTestParameters Parameters {
			get { return (HttpClientTestParameters)base.Parameters; }
		}

		public HttpClientTestType EffectiveType => GetEffectiveType (Parameters.Type);

		static HttpClientTestType GetEffectiveType (HttpClientTestType type)
		{
			if (type == HttpClientTestType.MartinTest)
				return MartinTest;
			return type;
		}

		public HttpServer Server {
			get;
		}

		public string ME {
			get;
		}

		public HttpClientTestRunner (IPortableEndPoint endpoint, HttpClientTestParameters parameters,
					     ConnectionTestProvider provider, Uri uri, HttpServerFlags flags)
			: base (endpoint, parameters)
		{
			Provider = provider;
			Uri = uri;

			ServerFlags = flags | HttpServerFlags.InstrumentationListener;

			Server = new BuiltinHttpServer (uri, endpoint, ServerFlags, parameters, null);

			ME = $"{GetType ().Name}({EffectiveType})";
		}

		const HttpClientTestType MartinTest = HttpClientTestType.GetError;

		static readonly (HttpClientTestType type, HttpClientTestFlags flags)[] TestRegistration = {
			(HttpClientTestType.Simple, HttpClientTestFlags.Working),
			(HttpClientTestType.GetString, HttpClientTestFlags.Working),
			(HttpClientTestType.PostString, HttpClientTestFlags.Working),
			(HttpClientTestType.PostStringWithResult, HttpClientTestFlags.Working),
			(HttpClientTestType.PutString, HttpClientTestFlags.Working),
			(HttpClientTestType.PutChunked, HttpClientTestFlags.Working),
			(HttpClientTestType.SendAsyncEmptyBody, HttpClientTestFlags.Working),
			(HttpClientTestType.SendAsyncGet, HttpClientTestFlags.Working),
			(HttpClientTestType.SendAsyncHead, HttpClientTestFlags.Working),
			(HttpClientTestType.SendLargeBlob, HttpClientTestFlags.Working),
			(HttpClientTestType.SendLargeBlobOddSize, HttpClientTestFlags.Working),
			(HttpClientTestType.ChunkSizeWithLeadingZero, HttpClientTestFlags.Working),
			(HttpClientTestType.PutRedirectEmptyBody, HttpClientTestFlags.Working),
			(HttpClientTestType.PutRedirect, HttpClientTestFlags.NewWebStack),
			(HttpClientTestType.PutRedirectKeepAlive, HttpClientTestFlags.NewWebStack),
			// Fixed in PR #6059 / #6068.
			(HttpClientTestType.SendAsyncObscureVerb, HttpClientTestFlags.WorkingMaster),

			// Martin
			(HttpClientTestType.GetError, HttpClientTestFlags.Ignore)
		};

		public static IList<HttpClientTestType> GetTestTypes (TestContext ctx, ConnectionTestCategory category)
		{
			if (category == ConnectionTestCategory.MartinTest)
				return new[] { MartinTest };

			var setup = DependencyInjector.Get<IConnectionFrameworkSetup> ();
			return TestRegistration.Where (t => Filter (t.flags)).Select (t => t.type).ToList ();

			bool Filter (HttpClientTestFlags flags)
			{
				switch (category) {
				case ConnectionTestCategory.MartinTest:
					return false;
				case ConnectionTestCategory.HttpClient:
					if (flags == HttpClientTestFlags.Working)
						return true;
					if (setup.UsingDotNet || setup.InternalVersion >= 1)
						return flags == HttpClientTestFlags.WorkingMaster;
					return false;
				case ConnectionTestCategory.HttpClientNewWebStack:
					return flags == HttpClientTestFlags.NewWebStack;
				default:
					throw ctx.AssertFail (category);
				}
			}
		}

		static string GetTestName (ConnectionTestCategory category, HttpClientTestType type, params object[] args)
		{
			var sb = new StringBuilder ();
			sb.Append (type);
			foreach (var arg in args) {
				sb.AppendFormat (":{0}", arg);
			}
			return sb.ToString ();
		}

		public static HttpClientTestParameters GetParameters (TestContext ctx, ConnectionTestCategory category,
								      HttpClientTestType type)
		{
			var certificateProvider = DependencyInjector.Get<ICertificateProvider> ();
			var acceptAll = certificateProvider.AcceptAll ();

			var name = GetTestName (category, type);

			var parameters = new HttpClientTestParameters (category, type, name, ResourceManager.SelfSignedServerCertificate) {
				ClientCertificateValidator = acceptAll
			};

			switch (GetEffectiveType (type)) {
			case HttpClientTestType.GetError:
				parameters.ExpectedError = WebExceptionStatus.Success;
				parameters.ExpectedStatus = HttpStatusCode.InternalServerError;
				break;
			default:
				parameters.ExpectedError = WebExceptionStatus.Success;
				parameters.ExpectedStatus = HttpStatusCode.OK;
				break;
			}

			return parameters;
		}

		public async Task Run (TestContext ctx, CancellationToken cancellationToken)
		{
			var me = $"{ME}.{nameof (Run)}()";
			ctx.LogDebug (2, $"{me}");

			var (handler, flags) = CreateHandler (ctx, true);

			ctx.LogDebug (2, $"{me}");

			currentOperation = new Operation (this, handler, flags);
			currentOperation.Start (ctx, cancellationToken);

			try {
				await currentOperation.WaitForCompletion ().ConfigureAwait (false);
				ctx.LogDebug (2, $"{me} operation done");
			} catch (Exception ex) {
				ctx.LogDebug (2, $"{me} operation failed: {ex.Message}");
				throw;
			}

			Server.CloseAll ();
		}

		(Handler handler, HttpOperationFlags flags) CreateHandler (TestContext ctx, bool primary)
		{
			var identifier = EffectiveType.ToString ();
			var hello = new HelloWorldHandler (identifier);
			var helloKeepAlive = new HelloWorldHandler (identifier) {
				Flags = RequestFlags.KeepAlive
			};

			HttpOperationFlags flags = HttpOperationFlags.None;
			Handler handler;

			switch (EffectiveType) {
			case HttpClientTestType.Simple:
				handler = hello;
				break;
			case HttpClientTestType.GetString:
				handler = new GetHandler (identifier, HttpContent.HelloWorld);
				break;
			case HttpClientTestType.PostString:
				handler = new PostHandler (identifier, HttpContent.HelloWorld);
				break;
			case HttpClientTestType.PostStringWithResult:
				handler = new PostHandler (identifier, HttpContent.HelloWorld) {
					ReturnContent = HttpContent.ReturningWorld
				};
				break;
			case HttpClientTestType.PutString:
				handler = new PostHandler (identifier, HttpContent.HelloWorld) {
					Method = "PUT"
				};
				break;
			case HttpClientTestType.PutChunked:
				handler = new PostHandler (identifier, ConnectionHandler.GetLargeChunkedContent (50), TransferMode.Chunked) {
					Method = "PUT"
				};
				break;
			case HttpClientTestType.SendAsyncEmptyBody:
				handler = new PostHandler (identifier, null, TransferMode.ContentLength);
				break;
			case HttpClientTestType.SendAsyncObscureVerb:
				handler = new PostHandler (identifier, null, TransferMode.ContentLength) { Method = "EXECUTE" };
				break;
			case HttpClientTestType.SendAsyncGet:
				handler = new PostHandler (identifier, null) { Method = "GET" };
				break;
			case HttpClientTestType.SendAsyncHead:
				handler = new PostHandler (identifier, null) { Method = "HEAD" };
				break;
			case HttpClientTestType.SendLargeBlob:
				handler = new PostHandler (identifier, BinaryContent.CreateRandom (102400));
				break;
			case HttpClientTestType.SendLargeBlobOddSize:
				handler = new PostHandler (identifier, BinaryContent.CreateRandom (102431));
				break;
			case HttpClientTestType.ChunkSizeWithLeadingZero:
				handler = new PostHandler (identifier, HttpContent.HelloWorld) {
					ReturnContent = new Bug20583Content ()
				};
				break;
			case HttpClientTestType.PutRedirectEmptyBody:
				handler = new PostHandler (identifier, null, TransferMode.ContentLength) { Method = "PUT" };
				handler = new RedirectHandler (handler, HttpStatusCode.TemporaryRedirect);
				break;
			case HttpClientTestType.PutRedirect:
				handler = new PostHandler (identifier, HttpContent.HelloWorld, TransferMode.ContentLength) { Method = "PUT" };
				handler = new RedirectHandler (handler, HttpStatusCode.TemporaryRedirect);
				break;
			case HttpClientTestType.PutRedirectKeepAlive:
				handler = new PostHandler (identifier, HttpContent.HelloWorld, TransferMode.ContentLength) { Method = "PUT" };
				handler = new RedirectHandler (handler, HttpStatusCode.TemporaryRedirect) {
					Flags = RequestFlags.KeepAlive
				};
				break;
			case HttpClientTestType.RedirectCustomContent:
				handler = new PostHandler (identifier, new CustomContent (this, ctx), TransferMode.ContentLength);
				handler = new RedirectHandler (handler, HttpStatusCode.TemporaryRedirect) {
					Flags = RequestFlags.KeepAlive
				};
				break;
			case HttpClientTestType.GetError:
				handler = new GetHandler (identifier, HttpContent.HelloWorld, HttpStatusCode.InternalServerError);
				break;

			default:
				throw ctx.AssertFail (EffectiveType);
			}

			return (handler, flags);
		}

		protected override async Task Initialize (TestContext ctx, CancellationToken cancellationToken)
		{
			await Server.Initialize (ctx, cancellationToken).ConfigureAwait (false);
		}

		protected override async Task Destroy (TestContext ctx, CancellationToken cancellationToken)
		{
			currentOperation?.Dispose ();
			currentOperation = null;
			await Server.Destroy (ctx, cancellationToken).ConfigureAwait (false);
		}

		protected override async Task PreRun (TestContext ctx, CancellationToken cancellationToken)
		{
			await Server.PreRun (ctx, cancellationToken).ConfigureAwait (false);
		}

		protected override async Task PostRun (TestContext ctx, CancellationToken cancellationToken)
		{
			await Server.PostRun (ctx, cancellationToken).ConfigureAwait (false);
		}

		protected override void Stop ()
		{
		}

		class Operation : HttpOperation
		{
			public HttpClientTestRunner Parent {
				get;
			}

			public Operation (HttpClientTestRunner parent, Handler handler, HttpOperationFlags flags)
				: base (parent.Server, $"{parent.EffectiveType}",
				        handler, flags, parent.Parameters.ExpectedStatus, parent.Parameters.ExpectedError)
			{
				Parent = parent;
			}

			protected override Request CreateRequest (TestContext ctx, Uri uri)
			{
				return new HttpClientRequest (uri);
			}

			protected override void ConfigureRequest (TestContext ctx, Uri uri, Request request)
			{
				var httpClientRequest = (HttpClientRequest)request;

				switch (Parent.EffectiveType) {
				case HttpClientTestType.Simple:
				case HttpClientTestType.GetString:
				case HttpClientTestType.SendAsyncEmptyBody:
				case HttpClientTestType.SendAsyncObscureVerb:
				case HttpClientTestType.SendAsyncGet:
				case HttpClientTestType.SendAsyncHead:
				case HttpClientTestType.PutRedirectEmptyBody:
				case HttpClientTestType.PutRedirect:
				case HttpClientTestType.PutRedirectKeepAlive:
					break;
				case HttpClientTestType.PostString:
				case HttpClientTestType.PostStringWithResult:
				case HttpClientTestType.PutString:
				case HttpClientTestType.SendLargeBlob:
				case HttpClientTestType.SendLargeBlobOddSize:
				case HttpClientTestType.ChunkSizeWithLeadingZero:
					httpClientRequest.Content = ((PostHandler)Handler).Content;
					break;
				case HttpClientTestType.PutChunked:
					httpClientRequest.Content = ((PostHandler)Handler).Content.RemoveTransferEncoding ();
					httpClientRequest.SendChunked ();
					break;
				case HttpClientTestType.RedirectCustomContent:
				case HttpClientTestType.GetError:
					break;
				default:
					throw ctx.AssertFail (Parent.EffectiveType);
				}

				Handler.ConfigureRequest (request, uri);

				request.SetProxy (Parent.Server.GetProxy ());
			}

			protected override Task<Response> RunInner (TestContext ctx, Request request, CancellationToken cancellationToken)
			{
				ctx.LogDebug (2, $"{ME} RUN INNER");

				var httpClientRequest = (HttpClientRequest)request;

				switch (Parent.EffectiveType) {
				case HttpClientTestType.Simple:
				case HttpClientTestType.GetString:
					return httpClientRequest.GetString (ctx, cancellationToken);
				case HttpClientTestType.PostString:
				case HttpClientTestType.PostStringWithResult:
				case HttpClientTestType.ChunkSizeWithLeadingZero:
					return httpClientRequest.PostString (ctx, cancellationToken);
				case HttpClientTestType.PutString:
				case HttpClientTestType.PutRedirectEmptyBody:
				case HttpClientTestType.PutRedirect:
				case HttpClientTestType.PutRedirectKeepAlive:
					return httpClientRequest.PutString (ctx, cancellationToken);
				case HttpClientTestType.PutChunked:
				case HttpClientTestType.SendAsyncEmptyBody:
				case HttpClientTestType.SendAsyncObscureVerb:
				case HttpClientTestType.SendAsyncGet:
				case HttpClientTestType.SendAsyncHead:
				case HttpClientTestType.GetError:
					return httpClientRequest.SendAsync (ctx, cancellationToken);
				case HttpClientTestType.SendLargeBlob:
				case HttpClientTestType.SendLargeBlobOddSize:
					return httpClientRequest.PutDataAsync (ctx, cancellationToken);
				case HttpClientTestType.RedirectCustomContent:
					return httpClientRequest.PostString (ctx, cancellationToken);
				default:
					throw ctx.AssertFail (Parent.EffectiveType);
				}
			}

			protected override void Destroy ()
			{
				;
			}
		}

		Operation currentOperation;

		class Bug20583Content : HttpContent
		{
			#region implemented abstract members of HttpContent
			public override bool HasLength => false;
			public override int Length => throw new InvalidOperationException ();
			public override string AsString ()
			{
				return "AAAA";
			}
			public override byte[] AsByteArray ()
			{
				throw new NotSupportedException ();
			}
			public override void AddHeadersTo (HttpMessage message)
			{
				message.TransferEncoding = "chunked";
				message.ContentType = "text/plain";
			}
			public override async Task WriteToAsync (TestContext ctx, Stream stream, CancellationToken cancellationToken)
			{
				await Task.Delay (500).ConfigureAwait (false);
				await stream.WriteAsync ("0");
				await Task.Delay (500);
				await stream.WriteAsync ("4\r\n");
				await stream.WriteAsync ("AAAA\r\n0\r\n\r\n");
			}
			#endregion
		}

		class CustomContent : HttpContent, ICustomHttpContent
		{
			public HttpClientTestRunner Parent {
				get;
			}

			public TestContext Context {
				get;
			}

			public string ME {
				get;
			}

			public CustomContent (HttpClientTestRunner parent, TestContext ctx)
			{
				Parent = parent;
				Context = ctx;
				ME = $"{parent.ME} - CUSTOM CONTENT";
			}

			HttpContent ICustomHttpContent.Content => this;

			public override bool HasLength => true;
			public override int Length => AsByteArray ().Length;

			public override string AsString () => ConnectionHandler.TheQuickBrownFox;

			public override byte[] AsByteArray () => ConnectionHandler.TheQuickBrownFoxBuffer;

			public override void AddHeadersTo (HttpMessage message)
			{
			}

			public override async Task WriteToAsync (TestContext ctx, Stream stream, CancellationToken cancellationToken)
			{
				await stream.WriteAsync (AsString (), cancellationToken).ConfigureAwait (false);
			}

			public override HttpContent RemoveTransferEncoding()
			{
				return this;
			}

			public Task<Stream> CreateContentReadStreamAsync ()
			{
				throw new NotImplementedException ();
			}

			bool first = true;

			public async Task SerializeToStreamAsync (Stream stream)
			{
				byte[] buffer;
				if (first) {
					buffer = AsByteArray ();
					first = false;
				} else {
					buffer = ConnectionHandler.GetTextBuffer (Parent.ME);
				}

				Context.LogDebug (5, $"{ME}: STSA");
				await stream.WriteAsync (buffer, 0, buffer.Length).ConfigureAwait (false);
			}

			public bool TryComputeLength (out long length)
			{
				Context.LogDebug (5, $"{ME}: TCL");
				if (HasLength) {
					length = Length;
					return true;
				}

				length = -1;
				return false;
			}

		}
	}
}
