//
// Connection.cs
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
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Xamarin.AsyncTests.Remoting
{
	using Framework;
	using Portable;

	public abstract class Connection
	{
		readonly Stream stream;
		readonly TestApp app;
		CancellationTokenSource cancelCts;
		TaskCompletionSource<object> mainTcs;
		bool shutdownRequested;

		internal Connection (TestApp app, Stream stream)
		{
			this.app = app;
			this.stream = stream;
			cancelCts = new CancellationTokenSource ();

#if CONNECTION_DEBUG
			var portable = DependencyInjector.Get<IPortableSupport> ();
			ME = $"[{DebugHelper.FormatType (this)}:{portable.CurrentProcess}:{portable.CurrentDomain}]";
#endif
		}

		internal string ME {
			get;
		}

		public TestApp App {
			get { return app; }
		}

		protected abstract bool IsServer {
			get;
		}

#region Public Client API

		public async Task Shutdown ()
		{
			if (shutdownRequested) {
				Debug ($"DUPLICATED SHUTDOWN");
				return;
			}
			Debug ($"SHUTDOWN");
			shutdownRequested = true;
			await new ShutdownCommand ().Send (this, CancellationToken.None).ConfigureAwait (false);
			Debug ($"SHUTDOWN DONE");
		}

#endregion

#region Command Handlers

		protected internal virtual void OnShutdown ()
		{
			shutdownRequested = true;
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

#endregion

#region Helper Methods

		[Conditional ("CONNECTION_DEBUG")]
		protected internal void Debug (string message)
		{
			System.Diagnostics.Debug.WriteLine ($"{ME}: {message}");
		}

#endregion

#region Start and Stop

		int mainLoopRunning;

		public async Task Start (CancellationToken cancellationToken)
		{
			lock (this) {
				if (mainTcs != null)
					return;
				mainTcs = new TaskCompletionSource<object> ();
			}

			var cts = CancellationTokenSource.CreateLinkedTokenSource (cancellationToken);
			cts.Token.Register (() => Stop ());

			TaskScheduler scheduler;
			if (SynchronizationContext.Current != null)
				scheduler = TaskScheduler.FromCurrentSynchronizationContext ();
			else
				scheduler = TaskScheduler.Current;

			await Task.Factory.StartNew (async () => {
				try {
					Debug ($"MAIN LOOP START");
					if (Interlocked.CompareExchange (ref mainLoopRunning, 1, 0) != 0) {
						Debug ($"MAIN LOOP ALREADY RUNNING!");
						throw new InternalErrorException ();
					}
					await MainLoop ().ConfigureAwait (false);
					Debug ($"MAIN LOOP COMPLETE");
					mainTcs.SetResult (null);
				} catch (OperationCanceledException) {
					Debug ($"MAIN LOOP CANCELED");
					mainTcs.SetCanceled ();
				} catch (Exception ex) {
					Debug ($"MAIN LOOP ERROR: {ex}");
					mainTcs.SetException (ex);
				}
			}, CancellationToken.None, TaskCreationOptions.None, scheduler);

			await OnStart (cancellationToken);
		}

		internal virtual Task OnStart (CancellationToken cancellationToken)
		{
			return Task.FromResult<object> (null);
		}

		public async Task Run (CancellationToken cancellationToken)
		{
			await Start (cancellationToken);

			try {
				await mainTcs.Task;
			} catch (Exception ex) {
				if (!shutdownRequested) {
					Debug ($"SERVER ERROR: {this} {ex}");
					throw;
				}
			}
		}

		public virtual void Stop ()
		{
			lock (this) {
				if (shutdownRequested)
					return;
				shutdownRequested = true;
				cancelCts.Cancel ();

				foreach (var operation in clientOperations.Values)
					operation.Task.TrySetCanceled ();
			}
		}

#endregion

#region Sending Commands and Main Loop

		internal async Task<bool> SendCommand (Command command, Response response, CancellationToken cancellationToken)
		{
			cancellationToken.ThrowIfCancellationRequested ();

			Debug ($"SEND COMMAND: {command}");

			Task<bool> responseTask = null;
			if (!command.IsOneWay)
				responseTask = RegisterResponse (command, response, cancellationToken);

			try {
				await SendMessage (command).ConfigureAwait (false);

				Debug ($"SEND COMMAND #1: {command} {responseTask != null}");

				if (responseTask == null)
					return false;

				return await responseTask;
			} catch (Exception ex) {
				Debug ($"SEND COMMAND EX: {command} {ex}");
				throw;
			} finally {
				Debug ($"SEND COMMAND DONE: {command}");
			}
		}

		QueuedMessage currentMessage;

		async Task SendMessage (Message message)
		{
			Debug ($"SEND MESSAGE: {message}");

			var queued = new QueuedMessage (message);

			while (!cancelCts.IsCancellationRequested) {
				var old = Interlocked.CompareExchange (ref currentMessage, queued, null);
				Debug ($"SEND MESSAGE QUEUE: {message} {old != null}");

				if (old == null)
					break;

				await old.Task.Task.ConfigureAwait (false);
			}

			cancelCts.Token.ThrowIfCancellationRequested ();

			Debug ($"SEND MESSAGE #1: {message}");

			var doc = message.Write (this);

			var sb = new StringBuilder ();
			var settings = new XmlWriterSettings ();
			settings.OmitXmlDeclaration = true;

			using (var writer = XmlWriter.Create (sb, settings)) {
				doc.WriteTo (writer);
			}

			var bytes = new UTF8Encoding ().GetBytes (sb.ToString ());

			var header = BitConverter.GetBytes (bytes.Length);
			if (bytes.Length == 0)
				throw new ServerErrorException ();

			await stream.WriteAsync (header, 0, 4).ConfigureAwait (false);
			await stream.FlushAsync ();

			await stream.WriteAsync (bytes, 0, bytes.Length);

			await stream.FlushAsync ();

			Debug ($"SEND MESSAGE #2: {message}");

			var old2 = Interlocked.CompareExchange (ref currentMessage, null, queued);
			if (old2 != queued)
				throw new ServerErrorException ();

			queued.Task.SetResult (true);

			Debug ($"SEND MESSAGE #3: {message}");
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
			while (!cancelCts.IsCancellationRequested) {
				Debug ($"MAIN LOOP: {shutdownRequested}");

				var header = await ReadBuffer (4);
				var len = BitConverter.ToInt32 (header, 0);
				Debug ($"MAIN LOOP #0: {shutdownRequested} {len}");
				if (len == 0)
					return;

				var body = await ReadBuffer (len);
				var content = new UTF8Encoding ().GetString (body, 0, body.Length);

				var doc = XDocument.Load (new StringReader (content));
				var element = doc.Root;

				Debug ($"MAIN LOOP: {element}");

				if (element.Name.LocalName.Equals ("Response")) {
					var objectID = element.Attribute ("ObjectID").Value;
					var operation = GetResponse (long.Parse (objectID));
					operation.Response.Read (this, element);
					operation.Task.SetResult (true);
					if (operation.Command is TestSessionClient.SessionShutdownCommand)
						return;
					continue;
				}

				var command = Command.Create (this, element);

				Debug ($"MAIN LOOP #1: {command} {command.IsOneWay}");

				cancelCts.Token.ThrowIfCancellationRequested ();

				if (command.IsOneWay)
					await command.Run (this, cancelCts.Token);
				else
					HandleCommand (command);

				Debug ($"MAIN LOOP #2: {command} {command.IsOneWay}");

				if (command is ShutdownCommand)
					return;
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
				Debug ($"HANDLE COMMAND: {command}");
				response = await command.Run (this, token);
			} catch (Exception ex) {
				response = new Response {
					ObjectID = command.ResponseID, Success = false, Error = ex.ToString ()
				};
			}

			try {
				Debug ($"HANDLE COMMAND #1: {command}");
				if (command.IsOneWay || command.ResponseID == 0 || response == null)
					return;

				await SendMessage (response);
			} catch (Exception ex) {
				Debug ($"ERROR WHILE SENDING RESPONSE: {ex}");
			} finally {
				Debug ($"HANDLE COMMAND DONE: {command}");
				lock (this) {
					if (operation != null) {
						operation.CancelCts.Dispose ();
						operation.CancelCts = null;
						serverOperations.Remove (operation.ObjectID);
					}
				}
				Debug ($"HANDLE COMMAND DONE #1: {command}");
			}
		}

#endregion

#region Serializer

		static long nextId;

		internal static long GetNextObjectId ()
		{
			return Interlocked.Increment (ref nextId);
		}

		Dictionary<long, ClientOperation> clientOperations = new Dictionary<long, ClientOperation> ();
		Dictionary<long, ServerOperation> serverOperations = new Dictionary<long, ServerOperation> ();

		internal Task<bool> RegisterResponse (Command command, Response response, CancellationToken cancellationToken)
		{
			ClientOperation operation;
			lock (this) {
				var objectID = GetNextObjectId ();
				operation = new ClientOperation (objectID, command, response);
				clientOperations.Add (objectID, operation);
				command.ResponseID = objectID;
			}

			var cts = CancellationTokenSource.CreateLinkedTokenSource (cancellationToken);
			cts.Token.Register (async () => {
				try {
					if (shutdownRequested)
						return;
					await new CancelCommand { ObjectID = operation.ObjectID }.Send (this, CancellationToken.None);
				} catch (Exception ex) {
					if (shutdownRequested)
						return;
					Debug ($"CANCEL COMMAND EX: {ex}");
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
			public readonly Command Command;
			public readonly Response Response;
			public TaskCompletionSource<bool> Task;

			public ClientOperation (long objectID, Command command, Response response)
			{
				ObjectID = objectID;
				Command = command;
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

		internal long RegisterObjectServant (ObjectServant servant)
		{
			lock (this) {
				if (remoteObjects.ContainsValue (servant))
					throw new ServerErrorException ();

				var id = GetNextObjectId ();
				remoteObjects.Add (id, servant);
				return id;
			}
		}

		internal void RegisterObjectServant (ObjectServant servant, long objectID)
		{
			lock (this) {
				if (remoteObjects.ContainsValue (servant))
					throw new ServerErrorException ();
				if (remoteObjects.ContainsKey (objectID))
					throw new ServerErrorException ();

				remoteObjects.Add (objectID, servant);
			}
		}

		internal void RegisterObjectClient (ObjectProxy proxy)
		{
			lock (this) {
				if (remoteObjects.ContainsKey (-proxy.ObjectID))
					throw new ServerErrorException ();
				remoteObjects.Add (-proxy.ObjectID, proxy);
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

#endregion
	}
}

