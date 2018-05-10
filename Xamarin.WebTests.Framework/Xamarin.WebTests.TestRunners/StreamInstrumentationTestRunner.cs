﻿﻿﻿//
// StreamInstrumentationTestRunner.cs
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
using System.Net;
using System.Net.Sockets;
using System.Net.Security;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using Xamarin.AsyncTests;
using Xamarin.AsyncTests.Constraints;
using Xamarin.AsyncTests.Framework;
using Xamarin.AsyncTests.Portable;

namespace Xamarin.WebTests.TestRunners
{
	using ConnectionFramework;
	using TestAttributes;
	using TestFramework;
	using Resources;

	[StreamInstrumentationTestRunner]
	public class StreamInstrumentationTestRunner : ClientAndServer, IConnectionInstrumentation
	{
		new public StreamInstrumentationParameters Parameters => (StreamInstrumentationParameters)base.Parameters;

		public StreamInstrumentationType EffectiveType {
			get {
				if (Parameters.Type == StreamInstrumentationType.MartinTest)
					return MartinTest;
				return Parameters.Type;
			}
		}

		internal InstrumentationFlags EffectiveFlags => GetFlags (EffectiveType);

		public ConnectionHandler ConnectionHandler {
			get;
		}

		public string ME {
			get;
			private set;
		}

		public StreamInstrumentationTestRunner ()
		{
			ConnectionHandler = new DefaultConnectionHandler (this);
		}

		protected override ConnectionParameters CreateParameters (TestContext ctx)
		{
			var parameters = ctx.GetParameter<StreamInstrumentationParameters> ();
			ME = $"StreamInstrumentationTestRunner({parameters.Type})";
			return parameters;
		}

		const StreamInstrumentationType MartinTest = StreamInstrumentationType.VerifyAsyncStreamCalls;

		public static IEnumerable<StreamInstrumentationType> GetStreamInstrumentationTypes (TestContext ctx, ConnectionTestCategory category)
		{
			var setup = DependencyInjector.Get<IConnectionFrameworkSetup> ();

			switch (category) {
			case ConnectionTestCategory.SslStreamInstrumentation:
				yield return StreamInstrumentationType.ClientHandshake;
				yield return StreamInstrumentationType.ReadDuringClientAuth;
				yield return StreamInstrumentationType.CloseBeforeClientAuth;
				yield return StreamInstrumentationType.CloseDuringClientAuth;
				yield return StreamInstrumentationType.DisposeDuringClientAuth;
				yield return StreamInstrumentationType.InvalidDataDuringClientAuth;
				yield return StreamInstrumentationType.ShortReadDuringClientAuth;
				yield return StreamInstrumentationType.ShortReadAndClose;
				yield return StreamInstrumentationType.RemoteClosesConnectionDuringRead;
				if (setup.UsingDotNet || setup.InternalVersion >= 1) {
					yield return StreamInstrumentationType.Flush;
					yield return StreamInstrumentationType.WriteDoesNotFlush;
					yield return StreamInstrumentationType.FlushAfterDispose;
					yield return StreamInstrumentationType.PropertiesAfterDispose;
				}
				yield break;

			case ConnectionTestCategory.SslStreamInstrumentationRecentlyFixed:
				yield break;

			case ConnectionTestCategory.SslStreamInstrumentationShutdown:
				yield return StreamInstrumentationType.CleanShutdown;
				yield return StreamInstrumentationType.DoubleShutdown;
				yield return StreamInstrumentationType.WriteAfterShutdown;
				yield return StreamInstrumentationType.WaitForShutdown;
				yield return StreamInstrumentationType.ReadAfterShutdown;
				yield return StreamInstrumentationType.ConnectionReuse;
				yield return StreamInstrumentationType.ConnectionReuseWithShutdown;
				yield break;

			case ConnectionTestCategory.SslStreamInstrumentationServerShutdown:
				yield return StreamInstrumentationType.ServerRequestsShutdown;
				yield break;

			case ConnectionTestCategory.SslStreamInstrumentationExperimental:
				yield break;

			case ConnectionTestCategory.SslStreamInstrumentationNewWebStack:
				yield return StreamInstrumentationType.VerifyAsyncStreamCalls;
				yield break;

			case ConnectionTestCategory.SslStreamInstrumentationMono:
				yield break;

			case ConnectionTestCategory.MartinTest:
				yield return StreamInstrumentationType.MartinTest;
				yield break;

			default:
				throw ctx.AssertFail (category);
			}
		}

		static string GetTestName (ConnectionTestCategory category, StreamInstrumentationType type, params object[] args)
		{
			var sb = new StringBuilder ();
			sb.Append (type);
			if (type == StreamInstrumentationType.MartinTest)
				sb.Append ($"({MartinTest})");
			foreach (var arg in args) {
				sb.Append ($":{arg}");
			}
			return sb.ToString ();
		}

		public static StreamInstrumentationParameters GetParameters (TestContext ctx, ConnectionTestCategory category,
									     StreamInstrumentationType type)
		{
			var certificateProvider = DependencyInjector.Get<ICertificateProvider> ();
			var acceptAll = certificateProvider.AcceptAll ();
			var effectiveType = type == StreamInstrumentationType.MartinTest ? MartinTest : type;

			var name = GetTestName (category, type);

			var parameters = new StreamInstrumentationParameters (category, type, name, ResourceManager.SelfSignedServerCertificate) {
				ClientCertificateValidator = acceptAll
			};

			switch (effectiveType) {
			case StreamInstrumentationType.ServerRequestsShutdown:
			case StreamInstrumentationType.ServerRequestsShutdownDuringWrite:
				parameters.SslStreamFlags |= SslStreamFlags.CleanShutdown;
				break;
			}

			if (category == ConnectionTestCategory.SslStreamInstrumentationShutdown)
				parameters.SslStreamFlags = SslStreamFlags.CleanShutdown;

			return parameters;
		}

		StreamInstrumentation clientInstrumentation;
		StreamInstrumentation serverInstrumentation;

		protected override Task PreRun (TestContext ctx, CancellationToken cancellationToken)
		{
			if (HasFlag (InstrumentationFlags.ServerHandshakeFails))
				Parameters.ExpectServerException = true;
			if (HasFlag (InstrumentationFlags.ClientHandshakeFails))
				Parameters.ExpectClientException = true;
			return base.PreRun (ctx, cancellationToken);
		}

		protected override Task PostRun (TestContext ctx, CancellationToken cancellationToken)
		{
			if (clientInstrumentation != null) {
				clientInstrumentation.Dispose ();
				clientInstrumentation = null;
			}
			if (serverInstrumentation != null) {
				serverInstrumentation.Dispose ();
				serverInstrumentation = null;
			}

			return base.PostRun (ctx, cancellationToken);
		}

		protected override void InitializeConnection (TestContext ctx)
		{
			ConnectionHandler.InitializeConnection (ctx);
			base.InitializeConnection (ctx);
		}

		protected sealed override async Task MainLoop (TestContext ctx, CancellationToken cancellationToken)
		{
			LogDebug (ctx, 4, "MainLoop()");
			if (!HasFlag (InstrumentationFlags.SkipMainLoop)) {
				await ConnectionHandler.MainLoop (ctx, cancellationToken).ConfigureAwait (false);
				return;
			}

			switch (EffectiveType) {
			case StreamInstrumentationType.CloseBeforeClientAuth:
			case StreamInstrumentationType.CloseDuringClientAuth:
			case StreamInstrumentationType.DisposeDuringClientAuth:
			case StreamInstrumentationType.InvalidDataDuringClientAuth:
				return;
			case StreamInstrumentationType.Flush:
			case StreamInstrumentationType.WriteDoesNotFlush:
			case StreamInstrumentationType.FlushAfterDispose:
				await FlushInstrumentation (ctx, cancellationToken).ConfigureAwait (false);
				break;
			case StreamInstrumentationType.DisposeClosesInnerStream:
			case StreamInstrumentationType.PropertiesAfterDispose:
				await DisposeInstrumentation (ctx, cancellationToken).ConfigureAwait (false);
				break;
			case StreamInstrumentationType.ServerRequestsShutdown:
			case StreamInstrumentationType.ServerRequestsShutdownDuringWrite:
				await Instrumentation_ServerRequestsShutdown (ctx, cancellationToken);
				break;
			default:
				throw ctx.AssertFail (EffectiveType);
			}
		}

		[Flags]
		internal enum InstrumentationFlags {
			None = 0,
			ClientInstrumentation = 1,
			ServerInstrumentation = 2,
			ClientStream = 4,
			ServerStream = 8,
			ClientHandshake = 16,
			ServerHandshake = 32,
			ClientShutdown = 64,
			ServerShutdown = 128,
			ServerHandshakeFails = 256,
			ClientHandshakeFails = 512,
			SkipMainLoop = 1024,
			ReuseClientSocket = 2048,
			ReuseServerSocket = 4096,
			SkipShutdown = 8192,

			HandshakeFails = ClientHandshakeFails | ServerHandshakeFails,

			NeedClientInstrumentation = ClientInstrumentation | ClientStream | ClientShutdown,
			NeedServerInstrumentation = ServerInstrumentation | ServerStream | ServerShutdown
		}

		static InstrumentationFlags GetFlags (StreamInstrumentationType type)
		{
			switch (type) {
			case StreamInstrumentationType.ClientHandshake:
			case StreamInstrumentationType.ReadDuringClientAuth:
			case StreamInstrumentationType.ShortReadDuringClientAuth:
				return InstrumentationFlags.ClientInstrumentation | InstrumentationFlags.ClientStream;
			case StreamInstrumentationType.CloseBeforeClientAuth:
			case StreamInstrumentationType.CloseDuringClientAuth:
			case StreamInstrumentationType.DisposeDuringClientAuth:
			case StreamInstrumentationType.InvalidDataDuringClientAuth:
				return InstrumentationFlags.ClientInstrumentation | InstrumentationFlags.ServerInstrumentation |
					InstrumentationFlags.ClientHandshake | InstrumentationFlags.SkipMainLoop |
					InstrumentationFlags.ServerHandshake | InstrumentationFlags.ServerHandshakeFails |
					InstrumentationFlags.ClientHandshakeFails;
			case StreamInstrumentationType.ShortReadAndClose:
				return InstrumentationFlags.ClientShutdown | InstrumentationFlags.ServerShutdown;
			case StreamInstrumentationType.RemoteClosesConnectionDuringRead:
				return InstrumentationFlags.ClientShutdown | InstrumentationFlags.ServerShutdown;
			case StreamInstrumentationType.CleanShutdown:
			case StreamInstrumentationType.DoubleShutdown:
			case StreamInstrumentationType.WriteAfterShutdown:
			case StreamInstrumentationType.ReadAfterShutdown:
			case StreamInstrumentationType.WaitForShutdown:
				return InstrumentationFlags.ClientShutdown | InstrumentationFlags.ServerShutdown;
			case StreamInstrumentationType.ConnectionReuse:
			case StreamInstrumentationType.ConnectionReuseWithShutdown:
				return InstrumentationFlags.ClientShutdown | InstrumentationFlags.ServerShutdown |
					InstrumentationFlags.ReuseClientSocket | InstrumentationFlags.ReuseServerSocket;
			case StreamInstrumentationType.Flush:
			case StreamInstrumentationType.WriteDoesNotFlush:
			case StreamInstrumentationType.FlushAfterDispose:
			case StreamInstrumentationType.DisposeClosesInnerStream:
			case StreamInstrumentationType.PropertiesAfterDispose:
				return InstrumentationFlags.ClientInstrumentation | InstrumentationFlags.SkipMainLoop |
					InstrumentationFlags.SkipShutdown;
			case StreamInstrumentationType.ServerRequestsShutdown:
			case StreamInstrumentationType.ServerRequestsShutdownDuringWrite:
				return InstrumentationFlags.ClientInstrumentation | InstrumentationFlags.SkipMainLoop |
					InstrumentationFlags.SkipShutdown;
			case StreamInstrumentationType.VerifyAsyncStreamCalls:
				return InstrumentationFlags.ClientStream;
			default:
				throw new InternalErrorException ();
			}
		}

		bool HasFlag (InstrumentationFlags flag)
		{
			var flags = GetFlags (EffectiveType);
			return (flags & flag) == flag;
		}

		bool HasAnyFlag (params InstrumentationFlags[] flags)
		{
			return flags.Any (f => HasFlag (f));
		}

		void LogDebug (TestContext ctx, int level, string message, params object[] args)
		{
			LogDebug (ctx, level, string.Format (message, args));
		}

		void LogDebug (TestContext ctx, int level, string message)
		{
			ctx.LogDebug (LogCategories.StreamInstrumentationTestRunner, level, $"{ME}: {message}");
		}

		protected override Task StartClient (TestContext ctx, CancellationToken cancellationToken)
		{
			if ((EffectiveFlags & InstrumentationFlags.NeedClientInstrumentation) != 0)
				return Client.Start (ctx, this, cancellationToken);
			return Client.Start (ctx, null, cancellationToken);
		}

		protected override Task StartServer (TestContext ctx, CancellationToken cancellationToken)
		{
			if ((EffectiveFlags & InstrumentationFlags.NeedServerInstrumentation) != 0)
				return Server.Start (ctx, this, cancellationToken);
			return Server.Start (ctx, null, cancellationToken);
		}

		protected override Task ClientShutdown (TestContext ctx, CancellationToken cancellationToken)
		{
			if (HasFlag (InstrumentationFlags.SkipShutdown))
				return FinishedTask;
			if (!HasAnyFlag (InstrumentationFlags.ClientShutdown)) {
				if (HasAnyFlag (InstrumentationFlags.HandshakeFails))
					return FinishedTask;
				return Client.Shutdown (ctx, cancellationToken);
			}

			switch (EffectiveType) {
			case StreamInstrumentationType.ShortReadAndClose:
				return Instrumentation_ShortReadAndClose (ctx);
			case StreamInstrumentationType.CleanShutdown:
			case StreamInstrumentationType.DoubleShutdown:
			case StreamInstrumentationType.WriteAfterShutdown:
			case StreamInstrumentationType.ReadAfterShutdown:
			case StreamInstrumentationType.WaitForShutdown:
				return Instrumentation_CleanClientShutdown (ctx, cancellationToken);
			case StreamInstrumentationType.RemoteClosesConnectionDuringRead:
				return Instrumentation_RemoteClosesConnectionDuringRead (ctx, cancellationToken);
			case StreamInstrumentationType.ConnectionReuse:
			case StreamInstrumentationType.ConnectionReuseWithShutdown:
				return ClientShutdown_ConnectionReuse (ctx, cancellationToken);
			default:
				throw ctx.AssertFail (EffectiveType);
			}
		}

		protected override Task ServerShutdown (TestContext ctx, CancellationToken cancellationToken)
		{
			if (HasFlag (InstrumentationFlags.SkipShutdown))
				return FinishedTask;
			if (!HasAnyFlag (InstrumentationFlags.ServerShutdown)) {
				if (HasAnyFlag (InstrumentationFlags.HandshakeFails))
					return FinishedTask;
				return Server.Shutdown (ctx, cancellationToken);
			}

			LogDebug (ctx, 4, "ServerShutdown()");

			switch (EffectiveType) {
			case StreamInstrumentationType.CleanShutdown:
			case StreamInstrumentationType.DoubleShutdown:
			case StreamInstrumentationType.WriteAfterShutdown:
			case StreamInstrumentationType.ReadAfterShutdown:
			case StreamInstrumentationType.WaitForShutdown:
				return Instrumentation_CleanServerShutdown (ctx, cancellationToken);
			case StreamInstrumentationType.RemoteClosesConnectionDuringRead:
			case StreamInstrumentationType.ShortReadAndClose:
				return FinishedTask;
			case StreamInstrumentationType.ConnectionReuse:
			case StreamInstrumentationType.ConnectionReuseWithShutdown:
				return ServerShutdown_ConnectionReuse (ctx, cancellationToken);
			default:
				throw ctx.AssertFail (EffectiveType);
			}
		}

		public Stream CreateClientStream (TestContext ctx, Connection connection, Socket socket)
		{
			if ((EffectiveFlags & InstrumentationFlags.NeedClientInstrumentation) == 0)
				throw ctx.AssertFail ("CreateClientStream()");

			var name = string.Format ("Client:{0}", EffectiveType);
			var ownsSocket = !HasFlag (InstrumentationFlags.ReuseClientSocket);
			var instrumentation = new StreamInstrumentation (ctx, name, socket, ownsSocket);

			var setup = DependencyInjector.Get<IConnectionFrameworkSetup> ();

			switch (EffectiveType) {
			case StreamInstrumentationType.VerifyAsyncStreamCalls:
				instrumentation.RequireAsync = true;
				if (setup.UsingDotNet) {
					instrumentation.AllowBeginEndAsync = true;
					instrumentation.AllowSyncFlush = true;
				}
				break;
			}

			if (Interlocked.CompareExchange (ref clientInstrumentation, instrumentation, null) != null)
				throw new InternalErrorException ();

			LogDebug (ctx, 4, "CreateClientStream()");

			if (!HasFlag (InstrumentationFlags.ClientStream))
				return instrumentation;

			instrumentation.OnNextRead (ReadHandler);

			return instrumentation;

			async Task<int> ReadHandler (byte[] buffer, int offset, int size,
						     StreamInstrumentation.AsyncReadFunc func,
						     CancellationToken cancellationToken)
			{
				ctx.Assert (Client.Stream, Is.Not.Null);
				ctx.Assert (Client.SslStream, Is.Not.Null);
				ctx.Assert (Client.SslStream.IsAuthenticated, Is.False);

				switch (EffectiveType) {
				case StreamInstrumentationType.ClientHandshake:
					LogDebug (ctx, 4, "CreateClientStream(): client handshake");
					break;
				case StreamInstrumentationType.ReadDuringClientAuth:
					await ctx.AssertException<InvalidOperationException> (ReadClient).ConfigureAwait (false);
					break;
				case StreamInstrumentationType.ShortReadDuringClientAuth:
					if (size <= 5)
						instrumentation.OnNextRead (ReadHandler);
					size = 1;
					break;
				case StreamInstrumentationType.VerifyAsyncStreamCalls:
					break;
				default:
					throw ctx.AssertFail (EffectiveType);
				}

				return await func (buffer, offset, size, cancellationToken);
			}

			Task<int> ReadClient ()
			{
				const int bufferSize = 100;
				return Client.Stream.ReadAsync (new byte[bufferSize], 0, bufferSize);
			}
		}

		public Stream CreateServerStream (TestContext ctx, Connection connection, Socket socket)
		{
			if ((EffectiveFlags & InstrumentationFlags.NeedServerInstrumentation) == 0)
				throw ctx.AssertFail ("CreateServerStream()");

			var name = string.Format ("Server:{0}", EffectiveType);
			var ownsSocket = !HasFlag (InstrumentationFlags.ReuseServerSocket);
			var instrumentation = new StreamInstrumentation (ctx, name, socket, ownsSocket);

			if (Interlocked.CompareExchange (ref serverInstrumentation, instrumentation, null) != null)
				throw new InternalErrorException ();

			LogDebug (ctx, 4, "CreateServerStream()");

			return instrumentation;
		}

		public async Task<bool> ClientHandshake (TestContext ctx, Func<Task> handshake, Connection connection)
		{
			if (!HasFlag (InstrumentationFlags.ClientHandshake))
				return false;

			int readCount = 0;

			clientInstrumentation.OnNextRead (ReadHandler);

			LogDebug (ctx, 4, "ClientHandshake()");
				  
			Constraint constraint;
			switch (EffectiveType) {
			case StreamInstrumentationType.InvalidDataDuringClientAuth:
				constraint = Is.InstanceOf<AuthenticationException> ();
				break;
			case StreamInstrumentationType.DisposeDuringClientAuth:
				constraint = Is.InstanceOf<ObjectDisposedException> ().Or.InstanceOfType<IOException> ();
				break;
			default:
				constraint = Is.InstanceOf<IOException> ();
				break;
			}

			await ctx.AssertException (handshake, constraint, "client handshake").ConfigureAwait (false);

			Server.Dispose ();

			return true;

			async Task<int> ReadHandler (byte[] buffer, int offset, int size,
						     StreamInstrumentation.AsyncReadFunc func,
						     CancellationToken cancellationToken)
			{
				ctx.Assert (Client.Stream, Is.Not.Null);
				ctx.Assert (Client.SslStream, Is.Not.Null);
				ctx.Assert (Client.SslStream.IsAuthenticated, Is.False);

				switch (EffectiveType) {
				case StreamInstrumentationType.ClientHandshake:
					LogDebug (ctx, 4, "ClientHandshake(): client handshake");
					break;
				case StreamInstrumentationType.CloseBeforeClientAuth:
					return 0;
				case StreamInstrumentationType.CloseDuringClientAuth:
					if (Interlocked.Increment (ref readCount) > 0)
						return 0;
					clientInstrumentation.OnNextRead (ReadHandler);
					break;
				case StreamInstrumentationType.InvalidDataDuringClientAuth:
					clientInstrumentation.OnNextRead (ReadHandler);
					break;
				case StreamInstrumentationType.DisposeDuringClientAuth:
					Client.SslStream.Dispose ();
					break;
				default:
					throw ctx.AssertFail (EffectiveType);
				}

				var ret = await func (buffer, offset, size, cancellationToken).ConfigureAwait (false);

				if (EffectiveType == StreamInstrumentationType.InvalidDataDuringClientAuth) {
					if (ret > 50) {
						for (int i = 10; i < 40; i++)
							buffer[i] = 0xAA;
					}
				}

				return ret;
			}
		}

		public async Task<bool> ServerHandshake (TestContext ctx, Func<Task> handshake, Connection connection)
		{
			if (!HasAnyFlag (InstrumentationFlags.ServerHandshakeFails))
				return false;

			LogDebug (ctx, 4, "ServerHandshake() - expecting failure");

			Constraint constraint;
			if (EffectiveType == StreamInstrumentationType.DisposeDuringClientAuth)
				constraint = Is.InstanceOf<ObjectDisposedException> ().Or.InstanceOfType<IOException> ();
			else
				constraint = Is.InstanceOf<ObjectDisposedException> ();

			await ctx.AssertException (handshake, constraint, "server handshake").ConfigureAwait (false);

			Client.Close ();

			return true;
		}

		async Task Instrumentation_RemoteClosesConnectionDuringRead (TestContext ctx, CancellationToken cancellationToken)
		{
			clientInstrumentation.OnNextRead ((buffer, offset, count, func, innerCancellationToken) => {
				return ctx.Assert (
					() => func (buffer, offset, count, innerCancellationToken),
					Is.EqualTo (0), "inner read returns zero");
			});

			var outerCts = new CancellationTokenSource (5000);

			var readBuffer = new byte[256];
			var readTask = Client.Stream.ReadAsync (readBuffer, 0, readBuffer.Length, outerCts.Token);

			Server.Close ();

			await ctx.Assert (() => readTask, Is.EqualTo (0), "read returns zero").ConfigureAwait (false);
		}

		TaskCompletionSource<bool> clientTcs = new TaskCompletionSource<bool> ();
		TaskCompletionSource<bool> serverTcs = new TaskCompletionSource<bool> ();

		async Task Instrumentation_CleanServerShutdown (TestContext ctx, CancellationToken cancellationToken)
		{
			var me = "Instrumentation_CleanServerShutdown";
			LogDebug (ctx, 4, me);

			switch (EffectiveType) {
			case StreamInstrumentationType.CleanShutdown:
			case StreamInstrumentationType.WriteAfterShutdown:
			case StreamInstrumentationType.DoubleShutdown:
				return;
			case StreamInstrumentationType.ReadAfterShutdown:
				await ReadAfterShutdown ().ConfigureAwait (false);
				return;
			case StreamInstrumentationType.WaitForShutdown:
				await WaitForShutdown ().ConfigureAwait (false);
				break;
			default:
				throw ctx.AssertFail (EffectiveType);
			}

			async Task ReadAfterShutdown ()
			{
				cancellationToken.ThrowIfCancellationRequested ();
				var ok = await clientTcs.Task.ConfigureAwait (false);
				LogDebug (ctx, 4, "{0} - client finished {1}", me, ok);

				cancellationToken.ThrowIfCancellationRequested ();

				await ConnectionHandler.WriteBlob (ctx, Server, cancellationToken).ConfigureAwait (false);
				LogDebug (ctx, 4, "{0} - write done", me);
			}

			async Task WaitForShutdown ()
			{
				cancellationToken.ThrowIfCancellationRequested ();
				var ok = await clientTcs.Task.ConfigureAwait (false);
				LogDebug (ctx, 4, "{0} - client finished {1}", me, ok);

				cancellationToken.ThrowIfCancellationRequested ();

				var buffer = new byte[256];
				await ctx.Assert (() => Server.Stream.ReadAsync (buffer, 0, buffer.Length), Is.EqualTo (0), "wait for shutdown");
			}
		}

		async Task Instrumentation_CleanClientShutdown (TestContext ctx, CancellationToken cancellationToken)
		{
			var me = "Instrumentation_CleanClientShutdown";
			LogDebug (ctx, 4, me);

			int bytesWritten = 0;

			clientInstrumentation.OnNextWrite (WriteHandler);

			try {
				cancellationToken.ThrowIfCancellationRequested ();
				await Client.Shutdown (ctx, cancellationToken).ConfigureAwait (false);
				LogDebug (ctx, 4, "{0} - done", me);
			} catch (OperationCanceledException) {
				LogDebug (ctx, 4, "{0} - canceled");
				clientTcs.TrySetCanceled ();
				throw;
			} catch (Exception ex) {
				LogDebug (ctx, 4, "{0} - error", me, ex);
				clientTcs.TrySetException (ex);
				throw;
			}

			var ok = ctx.Expect (bytesWritten, Is.GreaterThan (0), "{0} - bytes written", me);
			ok &= ctx.Expect (Client.SslStream.CanWrite, Is.False, "{0} - SslStream.CanWrite", me);

			clientTcs.TrySetResult (ok);

			cancellationToken.ThrowIfCancellationRequested ();

			switch (EffectiveType) {
			case StreamInstrumentationType.CleanShutdown:
				break;
			case StreamInstrumentationType.DoubleShutdown:
				await DoubleShutdown ();
				break;
			case StreamInstrumentationType.WriteAfterShutdown:
				await WriteAfterShutdown ();
				break;
			case StreamInstrumentationType.ReadAfterShutdown:
				await ReadAfterShutdown ();
				break;
			case StreamInstrumentationType.WaitForShutdown:
				break;
			default:
				throw ctx.AssertFail (EffectiveType);
			}

			Task DoubleShutdown ()
			{
				// Don't use Client.Shutdown() as that might throw a different exception.
				var setup = DependencyInjector.Get<IConnectionFrameworkSetup> ();
				return ctx.AssertException<InvalidOperationException> (
					() => setup.ShutdownAsync (Client.SslStream), "double shutdown");
			}

			Task WriteAfterShutdown ()
			{
				return ctx.AssertException<InvalidOperationException> (() => ConnectionHandler.WriteBlob (ctx, Client, cancellationToken));
			}

			async Task ReadAfterShutdown ()
			{
				LogDebug (ctx, 4, "{0} reading", me);
				await ConnectionHandler.ExpectBlob (ctx, Client, cancellationToken).ConfigureAwait (false);
				LogDebug (ctx, 4, "{0} reading done", me);
			}

			async Task WriteHandler (byte[] buffer, int offset, int size,
			                         StreamInstrumentation.AsyncWriteFunc func,
						 CancellationToken innerCancellationToken)
			{
				cancellationToken.ThrowIfCancellationRequested ();
				innerCancellationToken.ThrowIfCancellationRequested ();
				LogDebug (ctx, 4, "{0} - write handler: {1} {2}", me, offset, size);
				await func (buffer, offset, size, innerCancellationToken).ConfigureAwait (false);
				LogDebug (ctx, 4, "{0} - write handler done", me);
				bytesWritten += size;
			}
		}

		async Task ServerShutdown_WaitForClient (TestContext ctx, CancellationToken cancellationToken)
		{
			// We just wait until the client is done with stuff.
			await clientTcs.Task.ConfigureAwait (false);
		}

		async Task Instrumentation_ShortReadAndClose (TestContext ctx)
		{
			var me = "Instrumentation_ShortReadAndClose()";
			LogDebug (ctx, 4, me);

			bool readDone = false;

			clientInstrumentation.OnNextRead (ReadHandler);

			var writeBuffer = ConnectionHandler.TheQuickBrownFoxBuffer;
			var readBuffer = new byte[writeBuffer.Length + 256];

			var readTask = ClientRead ();

			await Server.Stream.WriteAsync (writeBuffer, 0, writeBuffer.Length);

			Server.Close ();

			LogDebug (ctx, 4, "{0} - closed server", me);

			await ctx.Assert (() => readTask, Is.EqualTo (writeBuffer.Length), "first client read").ConfigureAwait (false);

			LogDebug (ctx, 4, "{0} - first read done", me);

			readDone = true;

			await ctx.Assert (ClientRead, Is.EqualTo (0), "second client read").ConfigureAwait (false);

			LogDebug (ctx, 4, "{0} - second read done", me);

			async Task<int> ReadHandler (byte[] buffer, int offset, int size,
						     StreamInstrumentation.AsyncReadFunc func,
						     CancellationToken cancellationToken)
			{
				clientInstrumentation.OnNextRead (ReadHandler);

				LogDebug (ctx, 4, "{0} - read handler: {1} {2}", me, offset, size);
				var ret = await func (buffer, offset, size, cancellationToken).ConfigureAwait (false);
				LogDebug (ctx, 4, "{0} - read handler done: {1}", me, ret);

				if (readDone)
					ctx.Assert (ret, Is.EqualTo (0), "inner read returns zero");
				return ret;
			}

			Task<int> ClientRead ()
			{
				return Client.Stream.ReadAsync (readBuffer, 0, readBuffer.Length, CancellationToken.None);
			}
		}

		async Task ServerShutdown_ConnectionReuse (TestContext ctx, CancellationToken cancellationToken)
		{
			var me = $"ServerShutdown_ConnectionReuse({EffectiveType})";
			LogDebug (ctx, 4, me);

			Task<int> serverRead;
			if (EffectiveType == StreamInstrumentationType.ConnectionReuseWithShutdown) {
				var buffer = new byte[256];
				serverRead = Server.Stream.ReadAsync (buffer, 0, buffer.Length);
			} else {
				serverRead = Task.FromResult (0);
			}

			await ctx.Assert (() => serverRead, Is.EqualTo (0), "read shutdown notify").ConfigureAwait (false);

			Server.Close ();

			LogDebug (ctx, 4, "{0} - restarting: {1}", me, serverInstrumentation.Socket.LocalEndPoint);

			serverInstrumentation = null;

			serverTcs.TrySetResult (true);

			try {
				await ((DotNetConnection)Server).Restart (ctx, cancellationToken).ConfigureAwait (false);
				LogDebug (ctx, 4, "{0} - done restarting", me);
			} catch (Exception ex) {
				LogDebug (ctx, 4, "{0} - failed to restart: {1}", me, ex);
				throw;
			}

			await clientTcs.Task;

			Server.Close ();

			LogDebug (ctx, 4, "{0} done", me);
		}

		async Task ClientShutdown_ConnectionReuse (TestContext ctx, CancellationToken cancellationToken)
		{
			var me = "ClientShutdown_ConnectionReuse()";
			LogDebug (ctx, 4, me);

			if (EffectiveType == StreamInstrumentationType.ConnectionReuseWithShutdown) {
				await Client.Shutdown (ctx, cancellationToken);
				LogDebug (ctx, 4, "{0} - client shutdown done", me);
			}

			Client.Close ();

			clientInstrumentation = null;

			await serverTcs.Task.ConfigureAwait (false);
			LogDebug (ctx, 4, "{0} - server ready", me);

			try {
				LogDebug (ctx, 4, "{0} - restarting client", me);
				await ((DotNetConnection)Client).Restart (ctx, cancellationToken).ConfigureAwait (false);
				LogDebug (ctx, 4, "{0} - done restarting client", me);
			} catch (Exception ex) {
				LogDebug (ctx, 4, "{0} - restarting client failed: {1}", me, ex);
				throw;
			}

			Client.Close ();

			clientTcs.TrySetResult (true);
		}

		async Task FlushInstrumentation (TestContext ctx, CancellationToken cancellationToken)
		{
			var me = $"{nameof (FlushInstrumentation)}";
			LogDebug (ctx, 4, $"{me}");

			int flushCalled = 0;
			clientInstrumentation.OnNextFlush (FlushHandler);

			if (EffectiveType == StreamInstrumentationType.WriteDoesNotFlush) {
				var text = ConnectionHandler.TheQuickBrownFoxBuffer;
				await Client.SslStream.WriteAsync (text, 0, text.Length, cancellationToken).ConfigureAwait (false);
				ctx.Assert (flushCalled, Is.EqualTo (0), "WriteAsync() does not flush the inner stream");
			}

			if (EffectiveType == StreamInstrumentationType.FlushAfterDispose)
				Client.SslStream.Dispose ();

			await Client.SslStream.FlushAsync ().ConfigureAwait (false);
			ctx.Assert (flushCalled, Is.EqualTo (1), "Flush() called on inner stream");

			LogDebug (ctx, 4, $"{me} done");

			Task FlushHandler (StreamInstrumentation.AsyncFlushFunc func,
			                   CancellationToken innerCancellationToken)
			{
				LogDebug (ctx, 4, $"FLUSH HANDLER!");
				Interlocked.Increment (ref flushCalled);
				return FinishedTask;
			}
		}

		async Task DisposeInstrumentation (TestContext ctx, CancellationToken cancellationToken)
		{
			var me = $"{nameof (DisposeInstrumentation)}";
			LogDebug (ctx, 4, $"{me}");

			await FinishedTask;

			int disposeCalled = 0;
			clientInstrumentation.OnDispose (DisposeHandler);

			Client.SslStream.Dispose ();

			ctx.Expect (disposeCalled, Is.EqualTo (1), "called Dispose() on inner stream");

			ctx.Expect (() => Client.SslStream.CanRead, Is.False, "SslStream.CanRead after dispose");
			ctx.Expect (() => Client.SslStream.CanWrite, Is.False, "SslStream.CanWrite after dispose");
			ctx.Assert (() => Client.SslStream.CanTimeout, Is.True, "SslStream.CanTimeout after dispose");

			void DisposeHandler (StreamInstrumentation.DisposeFunc func)
			{
				Interlocked.Increment (ref disposeCalled);
				func ();
			}
		}

		async Task Instrumentation_ServerRequestsShutdown (TestContext ctx, CancellationToken cancellationToken)
		{
			var me = "Instrumentation_ServerRequestsShutdown";
			LogDebug (ctx, 4, me);

			cancellationToken.ThrowIfCancellationRequested ();

			var writeEvent = new TaskCompletionSource<object> ();
			var shutdownEvent = new TaskCompletionSource<object> ();
			cancellationToken.Register (() => writeEvent.TrySetCanceled ());
			cancellationToken.Register (() => shutdownEvent.TrySetCanceled ());

			clientInstrumentation.OnNextWrite (WriteHandler);

			int totalBytes = 0;

			if (EffectiveType == StreamInstrumentationType.ServerRequestsShutdownDuringWrite) {
				var largeBuffer = ConnectionHandler.GetLargeText (512);
				var largeBlob = Encoding.UTF8.GetBytes (largeBuffer);

				totalBytes = largeBlob.Length;
				LogDebug (ctx, 4, $"{me} writing {totalBytes} bytes");
				var writeTask = Client.SslStream.WriteAsync (largeBlob, 0, largeBlob.Length, cancellationToken);

				cancellationToken.ThrowIfCancellationRequested ();
				await writeEvent.Task.ConfigureAwait (false);
				LogDebug (ctx, 4, $"{me} done writing");
			}

			cancellationToken.ThrowIfCancellationRequested ();

			LogDebug (ctx, 4, $"{me}: server shutdown");
			await Server.Shutdown (ctx, cancellationToken).ConfigureAwait (false);
			LogDebug (ctx, 4, $"{me}: server shutdown done");

			cancellationToken.ThrowIfCancellationRequested ();

			var readBuffer = new byte[32768];
			var ret = await Client.SslStream.ReadAsync (readBuffer, 0, readBuffer.Length, cancellationToken);
			LogDebug (ctx, 4, $"{me}: client read: {ret}");

			shutdownEvent.TrySetResult (null);

			cancellationToken.ThrowIfCancellationRequested ();

			int remainingBytes = totalBytes;
			while (remainingBytes > 0) {
				LogDebug (ctx, 4, $"{me}: server read - {remainingBytes} / {totalBytes} remaining.");

				ret = await Server.SslStream.ReadAsync (readBuffer, 0, readBuffer.Length, cancellationToken);
				LogDebug (ctx, 4, $"{me}: server read returned: {ret}");

				if (ret <= 0)
					break;

				remainingBytes -= ret;
			}

			if (remainingBytes > 0)
				LogDebug (ctx, 4, $"{me} server read - connection closed with {remainingBytes} bytes remaining.");
			else
				LogDebug (ctx, 4, $"{me} server read complete.");

			async Task WriteHandler (byte[] buffer, int offset, int count,
			                         StreamInstrumentation.AsyncWriteFunc func,
			                         CancellationToken innerCancellationToken)
			{
				innerCancellationToken.ThrowIfCancellationRequested ();

				var writeMe = $"{me}: write handler ({ctx.GetUniqueId ()})";
				LogDebug (ctx, 4, $"{writeMe}: {offset} {count}");

				clientInstrumentation.OnNextWrite (WriteHandler);

				const int shortWrite = 4096;
				if (count > shortWrite) {
					await func (buffer, offset, shortWrite, innerCancellationToken).ConfigureAwait (false);
					offset += shortWrite;
					count -= shortWrite;

					LogDebug (ctx, 4, $"{writeMe}: short write done");
					writeEvent.TrySetResult (null);

					await shutdownEvent.Task;

					LogDebug (ctx, 4, $"{writeMe}: remaining {offset} {count}");
				}

				await func (buffer, offset, count, innerCancellationToken).ConfigureAwait (false);
				LogDebug (ctx, 4, $"{writeMe}: done");

				await FinishedTask;
			}
		}
	}
}
