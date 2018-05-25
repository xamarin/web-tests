//
// HttpStressTestRunner.cs
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
using System.Linq;
using System.Text;
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
	using TestAttributes;
	using HttpFramework;
	using HttpHandlers;
	using HttpOperations;
	using TestFramework;
	using Resources;
	using Server;

	[HttpStressTestRunner]
	public class HttpStressTestRunner : AbstractConnection
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

		public HttpStressTestParameters Parameters {
			get;
		}

		public HttpStressTestType EffectiveType => GetEffectiveType (Parameters.Type);

		static HttpStressTestType GetEffectiveType (HttpStressTestType type)
		{
			if (type == HttpStressTestType.MartinTest)
				return MartinTest;
			return type;
		}

		public HttpServer Server {
			get;
		}

		public string ME {
			get;
		}

		internal const string LogCategory = LogCategories.Stress;

		public HttpStressTestRunner (EndPoint endpoint, HttpStressTestParameters parameters,
					     ConnectionTestProvider provider, Uri uri, HttpServerFlags flags)
		{
			Parameters = parameters;
			Provider = provider;
			ServerFlags = flags | HttpServerFlags.ParallelListener;
			Uri = uri;

			Server = new BuiltinHttpServer (uri, endpoint, ServerFlags, parameters, null);

			ME = $"{GetType ().Name}({EffectiveType})";
		}

		const HttpStressTestType MartinTest = HttpStressTestType.Simple;

		static readonly (HttpStressTestType type, HttpStressTestFlags flags)[] TestRegistration = {
			(HttpStressTestType.Simple, HttpStressTestFlags.Unstable),
		};

		public static IList<HttpStressTestType> GetStressTypes (TestContext ctx, ConnectionTestCategory category)
		{
			if (category == ConnectionTestCategory.MartinTest)
				return new[] { MartinTest };

			var setup = DependencyInjector.Get<IConnectionFrameworkSetup> ();
			return TestRegistration.Where (t => Filter (t.flags)).Select (t => t.type).ToList ();

			bool Filter (HttpStressTestFlags flags)
			{
				switch (category) {
				case ConnectionTestCategory.MartinTest:
					return false;
				case ConnectionTestCategory.HttpStress:
					return flags == HttpStressTestFlags.Working;
				case ConnectionTestCategory.HttpStressExperimental:
					return flags == HttpStressTestFlags.Unstable;
				default:
					throw ctx.AssertFail (category);
				}
			}
		}

		static string GetTestName (ConnectionTestCategory category, HttpStressTestType type, params object[] args)
		{
			var sb = new StringBuilder ();
			sb.Append (type);
			foreach (var arg in args) {
				sb.AppendFormat (":{0}", arg);
			}
			return sb.ToString ();
		}

		public static HttpStressTestParameters GetParameters (TestContext ctx, ConnectionTestCategory category,
									       HttpStressTestType type)
		{
			var certificateProvider = DependencyInjector.Get<ICertificateProvider> ();
			var acceptAll = certificateProvider.AcceptAll ();

			var name = GetTestName (category, type);

			var parameters = new HttpStressTestParameters (category, type, name, ResourceManager.SelfSignedServerCertificate) {
				ClientCertificateValidator = acceptAll
			};

			switch (GetEffectiveType (type)) {
			case HttpStressTestType.Simple:
				parameters.MaxServicePoints = 100;
				parameters.CountParallelTasks = 75;
				parameters.RepeatCount = 2500;
				break;
			default:
				throw ctx.AssertFail (GetEffectiveType (type));
			}

			return parameters;
		}

		public async Task Run (TestContext ctx, CancellationToken cancellationToken)
		{
			var me = $"{ME}.{nameof (Run)}({EffectiveType})";
			ctx.LogDebug (LogCategory, 2, $"{me}");

			switch (EffectiveType) {
			case HttpStressTestType.Simple:
				await RunSimple ().ConfigureAwait (false);
				break;
			default:
				throw ctx.AssertFail (EffectiveType);
			}

			ctx.LogDebug (LogCategory, 2, $"{me} DONE");

			async Task RunSimple ()
			{
				if (Parameters.MaxServicePoints != null)
					ServicePointManager.MaxServicePoints = Parameters.MaxServicePoints.Value;

				var tasks = new Task[Parameters.CountParallelTasks + 1];
				for (int i = 0; i < tasks.Length; i++) {
					tasks[i] = RunLoop (i);
				}

				ctx.LogDebug (LogCategory, 2, $"{me} GOT TASKS");

				await Task.WhenAll (tasks);

			}

			async Task RunLoop (int idx)
			{
				var loopMe = $"{me} LOOP {idx}";

				for (int i = 0; i < Parameters.RepeatCount; i++) {
					await LoopOperation (i).ConfigureAwait (false);
					if ((i % 10) == 0)
						ctx.LogMessage ($"{loopMe} {i} DONE");
				}
			}

			async Task LoopOperation (int idx)
			{
				Handler handler;
				HttpOperation loopOperation;

				switch (EffectiveType) {
				case HttpStressTestType.Simple:
					handler = HelloWorldHandler.GetSimple (me);
					loopOperation = new TraditionalOperation (Server, handler, true);
					break;

				default:
					throw ctx.AssertFail (EffectiveType);
				}

				await loopOperation.Run (ctx, cancellationToken).ConfigureAwait (false);
			}
		}

		protected override async Task Initialize (TestContext ctx, CancellationToken cancellationToken)
		{
			await Server.Initialize (ctx, cancellationToken).ConfigureAwait (false);
		}

		protected override async Task Destroy (TestContext ctx, CancellationToken cancellationToken)
		{
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
	}
}
