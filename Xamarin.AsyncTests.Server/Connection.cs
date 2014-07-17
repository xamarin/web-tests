//
// TestServer.cs
//
// Author:
//       Martin Baulig <martin.baulig@xamarin.com>
//
// Copyright (c) 2014 Xamarin Inc. (http://www.xamarin.com)
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
using System.Xml;
using System.Xml.Linq;
using System.Linq;
using System.Text;
using System.ComponentModel;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Xamarin.AsyncTests.Server
{
	using Framework;

	public abstract class Connection
	{
		readonly Stream stream;
		readonly TestContext context;
		readonly bool isServer;
		CancellationTokenSource cancelCts;
		TaskCompletionSource<bool> commandTcs;
		Queue<QueuedMessage> messageQueue;
		bool shutdownRequested;
		TestSuite suite;

		public Connection (TestContext context, Stream stream, bool isServer)
		{
			this.isServer = isServer;
			this.context = context;
			this.stream = stream;
			cancelCts = new CancellationTokenSource ();
			messageQueue = new Queue<QueuedMessage> ();
		}

		public TestContext Context {
			get { return context; }
		}

		#region Public Client API

		public async Task LogMessage (string message)
		{
			var command = new LogMessageCommand { Argument = message };
			await command.Send (this);
		}

		public async Task SetLogLevel (int level, CancellationToken cancellationToken)
		{
			Context.DebugLevel = level;
			var command = new SetLogLevelCommand { Argument = level.ToString () };
			await command.Send (this, cancellationToken);
		}

		public Task<SettingsBag> GetSettings (CancellationToken cancellationToken)
		{
			return new GetSettingsCommand ().Send (this, cancellationToken);
		}

		public async Task Shutdown ()
		{
			suite = null;
			Context.CurrentTestSuite = null;
			await new ShutdownCommand ().Send (this);
		}

		public async Task<TestSuite> LoadTestSuite (CancellationToken cancellationToken)
		{
			if (suite != null)
				return suite;
			suite = await new LoadTestSuiteCommand ().Send (this, cancellationToken);
			await OnTestSuiteLoaded (suite, cancellationToken);
			return suite;
		}

		public async Task<bool> RunTest (TestCase test, TestResult result, CancellationToken cancellationToken)
		{
			var command = new RunTestCommand { Argument = test };

			try {
				var remoteResult = await command.Send (this, cancellationToken);
				result.AddChild (remoteResult);
				result.MergeStatus (remoteResult.Status);
				return true;
			} catch (Exception ex) {
				Debug ("SEND COMMAND ERROR: {0}", ex);
				result.AddError (ex);
				return false;
			}
		}

		public Task<TestResult> RunTestSuite (CancellationToken cancellationToken)
		{
			return new RunTestSuiteCommand ().Send (this, cancellationToken);
		}

		public async Task Hello (Handshake handshake, CancellationToken cancellationToken)
		{
			var hello = new HelloCommand { Argument = handshake };
			var retval = await hello.Send (this, cancellationToken);

			if (retval.Settings != null) {
				Context.Settings.Merge (retval.Settings);
				Context.Settings.PropertyChanged += OnSettingsChanged;
			}

			if (handshake.WantStatisticsEvents)
				context.Statistics.StatisticsEvent += OnStatisticsEvent;

			Debug ("Handshake complete.");
		}

		#endregion

		#region Command Handlers

		protected internal virtual void OnShutdown ()
		{
			shutdownRequested = true;
		}

		protected internal abstract void OnLogMessage (string message);

		protected abstract void OnDebug (int level, string message);

		internal void OnSetLogLevel (int level)
		{
			var serverLogger = context.Logger as ServerLogger;
			if (level < 0) {
				if (serverLogger != null)
					context.Logger = serverLogger.Parent;
			} else if (serverLogger == null) {
				context.Logger = new ServerLogger (this, context.Logger);
			}
			Context.DebugLevel = level;
		}

		internal SettingsBag OnGetSettings ()
		{
			return Context.Settings;
		}

		async void OnSettingsChanged (object sender, PropertyChangedEventArgs e)
		{
			if (shutdownRequested || cancelCts.IsCancellationRequested)
				return;

			await new SyncSettingsCommand { Argument = (SettingsBag)sender }.Send (this);
		}

		internal void OnSyncSettings (SettingsBag newSettings)
		{
			lock (this) {
				Context.Settings.Merge (newSettings);
			}
		}

		internal void OnCancel (long objectID)
		{
			lock (this) {
				ServerOperation operation;
				if (!serverOperations.TryGetValue (objectID, out operation))
					return;
				operation.CancelCts.Cancel ();
			}
		}

		protected internal abstract Task<TestSuite> OnLoadTestSuite (CancellationToken cancellationToken);

		protected internal virtual Task OnTestSuiteLoaded (TestSuite suite, CancellationToken cancellationToken)
		{
			Context.CurrentTestSuite = suite;
			return Task.FromResult<object> (null);
		}

		protected internal async Task<TestResult> OnRun (TestCase test, CancellationToken cancellationToken)
		{
			var result = new TestResult (test.Name);

			try {
				await test.Run (Context, result, cancellationToken).ConfigureAwait (false);
			} catch (OperationCanceledException) {
				result.Status = TestStatus.Canceled;
			} catch (Exception ex) {
				result.AddError (ex);
			}

			return result;
		}

		protected internal virtual Task<TestResult> OnRunTestSuite (CancellationToken cancellationToken)
		{
			return OnRun (suite, cancellationToken);
		}

		internal async Task<Handshake> OnHello (Handshake handshake, CancellationToken cancellationToken)
		{
			lock (this) {
				if (handshake.WantStatisticsEvents)
					Context.Statistics.StatisticsEvent += OnStatisticsEvent;
				if (handshake.Settings == null) {
					Context.Settings.PropertyChanged += OnSettingsChanged;
					handshake.Settings = Context.Settings;
				} else {
					Context.Settings.Merge (handshake.Settings);
					handshake.Settings = null;
				}
			}

			if (handshake.TestSuite != null) {
				await OnTestSuiteLoaded (handshake.TestSuite, cancellationToken);
				handshake.TestSuite = null;
			} else {
				handshake.TestSuite = await OnLoadTestSuite (cancellationToken);
			}

			return handshake;
		}

		async void OnStatisticsEvent (object sender, TestStatistics.StatisticsEventArgs e)
		{
			if (e.IsRemote)
				return;
			await new NotifyStatisticsEventCommand { Argument = e }.Send (this);
		}

		#endregion

		#region Helper Methods

		protected internal static void Debug (string message)
		{
			System.Diagnostics.Debug.WriteLine (message);
		}

		protected internal static void Debug (string message, params object[] args)
		{
			System.Diagnostics.Debug.WriteLine (message, args);
		}

		public static SettingsBag LoadSettings (XElement node)
		{
			return Serializer.ReadSettings (node);
		}

		public static XElement WriteSettings (SettingsBag settings)
		{
			return Serializer.WriteSettings (settings);
		}

		public static TestResult ReadTestResult (XElement node)
		{
			return Serializer.TestResult.Read (null, node);
		}

		public static XElement WriteTestResult (TestResult result)
		{
			return Serializer.TestResult.Write (null, result);
		}

		#endregion

		#region Start and Stop

		protected virtual Task Start (CancellationToken cancellationToken)
		{
			return Task.FromResult<object> (null);
		}

		public async Task Run (CancellationToken cancellationToken)
		{
			var cts = CancellationTokenSource.CreateLinkedTokenSource (cancellationToken);
			cts.Token.Register (() => {
				shutdownRequested = true;
				Stop ();
			});

			var tcs = new TaskCompletionSource<object> ();

			TaskScheduler scheduler;
			if (SynchronizationContext.Current != null)
				scheduler = TaskScheduler.FromCurrentSynchronizationContext ();
			else
				scheduler = TaskScheduler.Current;

			await Task.Factory.StartNew (async () => {
				try {
					await MainLoop ();
					tcs.SetResult (null);
				} catch (OperationCanceledException) {
					tcs.SetCanceled ();
				} catch (Exception ex) {
					tcs.SetException (ex);
				}
			}, CancellationToken.None, TaskCreationOptions.None, scheduler);

			await Start (cancellationToken);

			try {
				await tcs.Task;
			} catch (Exception ex) {
				if (!shutdownRequested)
					Debug ("SERVER ERROR: {0}", ex);
			}
		}

		public virtual void Stop ()
		{
			lock (this) {
				if (shutdownRequested)
					return;
				shutdownRequested = true;
				cancelCts.Cancel ();

				Context.Settings.PropertyChanged -= OnSettingsChanged;
				context.Statistics.StatisticsEvent -= OnStatisticsEvent;

				foreach (var queued in messageQueue)
					queued.Task.TrySetCanceled ();
				foreach (var operation in clientOperations.Values)
					operation.Task.TrySetCanceled ();
			}
		}

		#endregion

		#region Sending Commands and Main Loop

		internal async Task<bool> SendCommand (Command command, Response response, CancellationToken cancellationToken)
		{
			Task<bool> responseTask = null;
			if (!command.IsOneWay)
				responseTask = RegisterResponse (command, response, cancellationToken);

			try {
				await SendMessage (command);

				if (responseTask == null)
					return false;

				return await responseTask;
			} catch (Exception ex) {
				Debug ("SEND COMMAND EX: {0} {1}", command, ex);
				throw;
			}
		}

		async Task SendMessage (Message message)
		{
			Task queuedTask;
			var queued = new QueuedMessage (message);
			lock (this) {
				if (commandTcs != null) {
					messageQueue.Enqueue (queued);
					queuedTask = queued.Task.Task;
				} else {
					commandTcs = queued.Task;
					queuedTask = null;
				}
			}

			if (queuedTask == null) {
				var innerTask = Task.Factory.StartNew (async () => {
					try {
						await RunQueue (queued);
						queued.Task.SetResult (true);
					} catch (OperationCanceledException) {
						queued.Task.SetCanceled ();
					} catch (Exception ex) {
						queued.Task.SetException (ex);
					}

					await RunQueue ();
				});

				await queued.Task.Task;
				await innerTask;
			} else {
				await queuedTask;
			}
		}

		async Task RunQueue ()
		{
			while (true) {
				QueuedMessage message;
				lock (this) {
					if (messageQueue.Count == 0) {
						commandTcs = null;
						return;
					}
					message = messageQueue.Dequeue ();
					commandTcs = message.Task;
				}

				await RunQueue (message);
			}
		}

		async Task RunQueue (QueuedMessage message)
		{
			var doc = message.Message.Write (this);

			var sb = new StringBuilder ();
			var settings = new XmlWriterSettings ();
			settings.OmitXmlDeclaration = true;

			using (var writer = XmlWriter.Create (sb, settings)) {
				doc.WriteTo (writer);
			}

			var bytes = new UTF8Encoding ().GetBytes (sb.ToString ());

			var header = BitConverter.GetBytes (bytes.Length);
			if (bytes.Length == 0)
				throw new InvalidOperationException ();

			await stream.WriteAsync (header, 0, 4).ConfigureAwait (false);
			await stream.FlushAsync ();

			await stream.WriteAsync (bytes, 0, bytes.Length);

			await stream.FlushAsync ();
		}

		async Task<byte[]> ReadBuffer (int length)
		{
			var buffer = new byte [length];
			int pos = 0;
			while (pos < length) {
				var ret = await stream.ReadAsync (buffer, pos, length-pos, cancelCts.Token);
				if (ret <= 0)
					throw new IOException ("Read failed");
				pos += ret;
			}
			return buffer;
		}

		async Task MainLoop ()
		{
			while (!shutdownRequested && !cancelCts.IsCancellationRequested) {
				var header = await ReadBuffer (4);
				var len = BitConverter.ToInt32 (header, 0);
				if (len == 0)
					return;

				var body = await ReadBuffer (len);
				var content = new UTF8Encoding ().GetString (body, 0, body.Length);

				var doc = XDocument.Load (new StringReader (content));
				var element = doc.Root;

				if (element.Name.LocalName.Equals ("Response")) {
					var objectID = element.Attribute ("ObjectID").Value;
					var operation = GetResponse (long.Parse (objectID));
					operation.Response.Read (this, element);
					operation.Task.SetResult (true);
					continue;
				}

				var command = Command.Create (this, element);

				cancelCts.Token.ThrowIfCancellationRequested ();

				if (command.IsOneWay) {
					await command.Run (this, cancelCts.Token);
					continue;
				}

				HandleCommand (command);
			}
		}

		async void HandleCommand (Command command)
		{
			cancelCts.Token.ThrowIfCancellationRequested ();

			await Task.Yield ();

			var token = cancelCts.Token;
			var responseID = command.IsOneWay ? 0 : command.ResponseID;

			ServerOperation operation = null;
			lock (this) {
				if (responseID > 0) {
					operation = new ServerOperation (command.ResponseID, token);
					serverOperations.Add (responseID, operation);
					token = operation.CancelCts.Token;
				}
			}

			Response response;
			try {
				response = await command.Run (this, token);
			} catch (Exception ex) {
				response = new Response {
					ObjectID = command.ResponseID, Success = false, Error = ex.ToString ()
				};
			}

			try {
				if (command.IsOneWay || command.ResponseID == 0 || response == null)
					return;

				await SendMessage (response);
			} catch (Exception ex) {
				Debug ("ERROR WHILE SENDING RESPONSE: {0}", ex);
			} finally {
				lock (this) {
					if (operation != null) {
						operation.CancelCts.Dispose ();
						serverOperations.Remove (operation.ObjectID);
					}
				}
			}
		}

		#endregion

		#region Serializer

		static long next_id;
		long GetNextObjectId ()
		{
			if (isServer)
				return ++next_id;
			else
				return --next_id;
		}

		Dictionary<long, ClientOperation> clientOperations = new Dictionary<long, ClientOperation> ();
		Dictionary<long, ServerOperation> serverOperations = new Dictionary<long, ServerOperation> ();

		internal Task<bool> RegisterResponse (Command command, Response response, CancellationToken cancellationToken)
		{
			ClientOperation operation;
			lock (this) {
				var objectID = GetNextObjectId ();
				operation = new ClientOperation (objectID, response);
				clientOperations.Add (objectID, operation);
				command.ResponseID = objectID;
			}

			var cts = CancellationTokenSource.CreateLinkedTokenSource (cancellationToken);
			cts.Token.Register (async () => {
				try {
					if (shutdownRequested)
						return;
					await new CancelCommand { ObjectID = operation.ObjectID }.Send (this);
				} catch (Exception ex) {
					if (shutdownRequested)
						return;
					Debug ("CANCEL COMMAND EX: {0}", ex);
					throw;
				}
			});

			return operation.Task.Task;
		}

		ClientOperation GetResponse (long objectID)
		{
			lock (this) {
				var operation = clientOperations [objectID];
				clientOperations.Remove (operation.ObjectID);
				return operation;
			}
		}

		class ServerOperation
		{
			public readonly long ObjectID;
			public CancellationTokenSource CancelCts;

			public ServerOperation (long objectId, CancellationToken cancellationToken)
			{
				ObjectID = objectId;
				CancelCts = CancellationTokenSource.CreateLinkedTokenSource (cancellationToken);
			}
		}

		class ClientOperation
		{
			public readonly long ObjectID;
			public readonly Response Response;
			public TaskCompletionSource<bool> Task;

			public ClientOperation (long objectID, Response response)
			{
				ObjectID = objectID;
				Response = response;
				Task = new TaskCompletionSource<bool> ();
			}
		}

		class QueuedMessage
		{
			public readonly Message Message;
			public readonly TaskCompletionSource<bool> Task;

			public QueuedMessage (Message message)
			{
				Message = message;
				Task = new TaskCompletionSource<bool> ();
			}
		}

		Dictionary<long,object> remoteObjects = new Dictionary<long,object> ();

		internal long RegisterRemoteObject (object obj)
		{
			lock (this) {
				if (remoteObjects.ContainsValue (obj))
					return remoteObjects.First (e => e.Value == obj).Key;

				var remoteObj = obj as IRemoteObject;
				if (remoteObj != null)
					return remoteObj.ObjectID;

				var id = GetNextObjectId ();
				remoteObjects.Add (id, obj);
				return id;
			}
		}

		internal bool TryGetRemoteObject<T> (long id, out T value)
			where T : class
		{
			lock (this) {
				object obj;
				if (!remoteObjects.TryGetValue (id, out obj)) {
					value = null;
					return false;
				}

				value = (T)obj;
				return true;
			}
		}

		internal void UnregisterRemoteObject (object obj)
		{
			lock (this) {
				var remoteObj = obj as IRemoteObject;
				if (remoteObj != null) {
					remoteObjects.Remove (remoteObj.ObjectID);
					return;
				}

				if (!remoteObjects.ContainsValue (obj))
					return;

				var id = remoteObjects.First (e => e.Value == obj).Key;
				remoteObjects.Remove (id);
			}
		}

		#endregion
	}
}

