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
		Stream stream;
		TestContext context;
		CancellationTokenSource cancelCts;
		TaskCompletionSource<bool> commandTcs;
		Queue<QueuedMessage> messageQueue;
		bool shutdownRequested;

		public Connection (TestContext context, Stream stream)
		{
			this.context = context;
			this.stream = stream;
			cancelCts = new CancellationTokenSource ();
			messageQueue = new Queue<QueuedMessage> ();
		}

		public int DebugLevel {
			get; set;
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

		public async Task SetDebugLevel (int level)
		{
			var command = new SetDebugLevelCommand { Argument = level.ToString () };
			await command.Send (this);
		}

		public Task<SettingsBag> GetSettings (CancellationToken cancellationToken)
		{
			return new GetSettingsCommand ().Send (this, cancellationToken);
		}

		public async Task Shutdown ()
		{
			await new ShutdownCommand ().Send (this);
		}

		public Task<TestSuite> LoadTestSuite (CancellationToken cancellationToken)
		{
			return new LoadTestSuiteCommand ().Send (this, cancellationToken);
		}

		protected string DumpSettings (SettingsBag settings)
		{
			var node = Serializer.Settings.Write (this, settings);
			var wxs = new XmlWriterSettings ();
			wxs.Indent = true;
			using (var writer = new StringWriter ()) {
				var xml = XmlWriter.Create (writer, wxs);
				node.WriteTo (xml);
				xml.Flush ();
				return writer.ToString ();
			}
		}

		public static SettingsBag LoadSettings (XElement node)
		{
			return Serializer.ReadSettings (node);
		}

		public static XElement WriteSettings (SettingsBag settings)
		{
			return Serializer.WriteSettings (settings);
		}

		#endregion

		public virtual void Stop ()
		{
			cancelCts.Cancel ();

			lock (this) {
				Context.Settings.PropertyChanged -= OnSettingsChanged;

				foreach (var queued in messageQueue)
					queued.Task.TrySetCanceled ();
				foreach (var operation in operations.Values)
					operation.Task.TrySetCanceled ();
			}
		}

		public ITestLogger GetLogger ()
		{
			return new ServerLogger (this);
		}

		internal async Task<bool> SendCommand (Command command, Response response, CancellationToken cancellationToken)
		{
			Task<bool> responseTask = null;
			if (!command.IsOneWay)
				responseTask = RegisterResponse (command, response, cancellationToken);

			await SendMessage (command);

			if (responseTask == null)
				return false;

			return await responseTask;
		}

		internal async Task SendMessage (Message message)
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

		protected internal static void Debug (string message)
		{
			System.Diagnostics.Debug.WriteLine (message);
		}

		protected internal static void Debug (string message, params object[] args)
		{
			System.Diagnostics.Debug.WriteLine (message, args);
		}

		async Task<byte[]> ReadBuffer (int length)
		{
			var buffer = new byte [length];
			int pos = 0;
			while (pos < length) {
				var ret = await stream.ReadAsync (buffer, pos, length-pos, cancelCts.Token);
				if (ret <= 0)
					throw new InvalidOperationException ();
				pos += ret;
			}
			return buffer;
		}

		protected async Task MainLoop ()
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

			Response response;
			try {
				response = await command.Run (this, cancelCts.Token);
			} catch (Exception ex) {
				response = new Response {
					ObjectID = command.ResponseID, Success = false, Error = ex.ToString ()
				};
			}

			if (command.IsOneWay || command.ResponseID == 0 || response == null)
				return;

			try {
				await SendMessage (response);
			} catch (Exception ex) {
				Debug ("ERROR WHILE SENDING RESPONSE: {0}", ex);
			}
		}

		protected internal virtual void OnShutdown ()
		{
			shutdownRequested = true;
		}

		protected internal abstract void OnLogMessage (string message);

		protected abstract void OnDebug (int level, string message);

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

		static long next_id;
		protected long GetNextObjectId ()
		{
			return ++next_id;
		}

		Dictionary<long,Operation> operations = new Dictionary<long, Operation> ();

		internal Task<bool> RegisterResponse (Command command, Response response, CancellationToken cancellationToken)
		{
			Operation operation;
			lock (this) {
				var objectID = GetNextObjectId ();
				operation = new Operation (objectID, response);
				operations.Add (objectID, operation);
				command.ResponseID = objectID;
			}

			var cts = CancellationTokenSource.CreateLinkedTokenSource (cancellationToken);
			cts.Token.Register (async () => {
				await new CancelCommand { ObjectID = operation.ObjectID }.Send (this);
			});

			return operation.Task.Task;
		}

		Operation GetResponse (long objectID)
		{
			lock (this) {
				var operation = operations [objectID];
				operations.Remove (operation.ObjectID);
				return operation;
			}
		}

		internal void OnCancel (long objectID)
		{
			lock (this) {
				Operation operation;
				if (!operations.TryGetValue (objectID, out operation))
					return;
				operation.Task.TrySetCanceled ();
			}
		}

		internal class Operation
		{
			public readonly long ObjectID;
			public readonly Response Response;
			public TaskCompletionSource<bool> Task;

			public Operation (long objectID, Response response)
			{
				ObjectID = objectID;
				Response = response;
				Task = new TaskCompletionSource<bool> ();
			}
		}

		internal class QueuedMessage
		{
			public readonly Message Message;
			public readonly TaskCompletionSource<bool> Task;

			public QueuedMessage (Message message)
			{
				Message = message;
				Task = new TaskCompletionSource<bool> ();
			}
		}

		protected internal abstract Task<TestSuite> OnLoadTestSuite (CancellationToken cancellationToken);

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

		internal async Task<bool> RunTest (TestCase test, TestResult result, CancellationToken cancellationToken)
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

		internal async Task<TestResult> OnRun (TestCase test, CancellationToken cancellationToken)
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

		protected async Task Hello (bool useServerSettings, CancellationToken cancellationToken)
		{
			Debug ("HELLO: {0}", useServerSettings);
			var hello = new HelloCommand ();
			if (!useServerSettings)
				hello.Argument = Context.Settings;
			var retval = await hello.Send (this, cancellationToken);
			if (useServerSettings) {
				if (retval == null)
					throw new InvalidOperationException ();
				Context.Settings.Merge (retval);
				Context.Settings.PropertyChanged += OnSettingsChanged;
			}
			Debug ("Handshake complete.");
		}

		internal SettingsBag OnHello (SettingsBag argument)
		{
			Debug ("ON HELLO: {0}", argument != null);

			lock (this) {
				if (argument == null) {
					Context.Settings.PropertyChanged += OnSettingsChanged;
					return Context.Settings;
				} else {
					Context.Settings.Merge (argument);
					return null;
				}
			}
		}
	}
}

