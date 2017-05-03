//
// ProcessHelper.cs
//
// Author:
//       Martin Baulig <martin.baulig@xamarin.com>
//
// Copyright (c) 2017 Xamarin, Inc.
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
using System.Text.RegularExpressions;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Xamarin.AsyncTests.Remoting;

namespace Xamarin.AsyncTests.Console {
	class ProcessHelper : ExternalProcess {
		Process process;
		TextWriter output;
		string commandLine;
		CancellationTokenSource cts;
		TaskCompletionSource<object> tcs;

		public override string CommandLine {
			get;
		}

		ProcessHelper (Process process, TextWriter output, CancellationToken cancellationToken)
		{
			this.process = process;
			this.output = output;

			commandLine = GetCommandLine (process.StartInfo);

			tcs = new TaskCompletionSource<object> ();

			cts = CancellationTokenSource.CreateLinkedTokenSource (cancellationToken);
			cts.Token.Register (() => {
				try {
					process.Kill ();
				} catch {
					;
				}
			});

			process.EnableRaisingEvents = true;
			process.Exited += (sender, e) => {
				Program.Debug ("TOOL EXITED: {0}", process.ExitCode);
				OnExited (process.ExitCode);
				if (cts.IsCancellationRequested) {
					tcs.TrySetCanceled ();
				} else if (process.ExitCode != 0) {
					var message = string.Format ("External tool failed with exit code {0}.", process.ExitCode);
					tcs.TrySetException (new ExternalToolException (commandLine, message));
				} else {
					tcs.TrySetResult (null);
				}
			};

			if (output != null) {
				process.OutputDataReceived += (sender, e) => {
					output.WriteLine (e.Data);
				};
				process.ErrorDataReceived += (sender, e) => {
					output.WriteLine (e.Data);
				};

				process.BeginErrorReadLine ();
				process.BeginOutputReadLine ();
			}
		}

		static string GetCommandLine (ProcessStartInfo startInfo)
		{
			var commandLine = startInfo.FileName;
			if (!string.IsNullOrWhiteSpace (startInfo.Arguments))
				commandLine += " " + startInfo.Arguments;
			return commandLine;
		}

		public override void Abort ()
		{
			cts.Cancel ();
		}

		public override Task WaitForExit (CancellationToken cancellationToken)
		{
			using (var linkedCts = CancellationTokenSource.CreateLinkedTokenSource (cancellationToken)) {
				linkedCts.Token.Register (() => Abort ());
				return tcs.Task;
			}
		}

		public static async Task RunCommand (string command, string args, CancellationToken cancellationToken)
		{
			var process = await StartCommand (command, args, cancellationToken).ConfigureAwait (false);
			await process.WaitForExit (cancellationToken);
		}

		public static Task<ExternalProcess> StartCommand (string command, string args, CancellationToken cancellationToken)
		{
			return StartCommand (command, args, null, cancellationToken);
		}

		public static Task<ExternalProcess> StartCommand (string command, string args, TextWriter output, CancellationToken cancellationToken)
		{
			cancellationToken.ThrowIfCancellationRequested ();

			var psi = new ProcessStartInfo (command, args);
			psi.UseShellExecute = false;
			psi.RedirectStandardInput = true;
			if (output != null) {
				psi.RedirectStandardOutput = true;
				psi.RedirectStandardError = true;
			}

			return StartCommand (psi, output, cancellationToken);
		}

		public static Task<ExternalProcess> StartCommand (ProcessStartInfo psi, CancellationToken cancellationToken)
		{
			return StartCommand (psi, null, cancellationToken);
		}

		public static Task<ExternalProcess> StartCommand (ProcessStartInfo psi, TextWriter output, CancellationToken cancellationToken)
		{
			cancellationToken.ThrowIfCancellationRequested ();

			return Task.Run<ExternalProcess> (() => {
				cancellationToken.ThrowIfCancellationRequested ();

				var commandLine = GetCommandLine (psi);
				Program.Debug ("Running tool: {0}", commandLine);

				var process = Process.Start (psi);
				Program.Debug ("Started tool: {0}", commandLine);
				return new ProcessHelper (process, output, cancellationToken);
			});
		}

		public static Task<string> RunCommandWithOutput (string command, string args, CancellationToken cancellationToken)
		{
			var tcs = new TaskCompletionSource<string> ();
			var cts = CancellationTokenSource.CreateLinkedTokenSource (cancellationToken);

			Task.Run (() => {
				var tool = string.Join (" ", command, args);
				try {
					var psi = new ProcessStartInfo (command, args);
					psi.UseShellExecute = false;
					psi.RedirectStandardInput = true;
					psi.RedirectStandardOutput = true;
					psi.RedirectStandardError = true;

					Program.Debug ("Running tool: {0}", tool);

					var process = Process.Start (psi);

					cts.Token.Register (() => {
						try {
							process.Kill ();
						} catch {
							;
						}
					});

					var stdoutTask = process.StandardOutput.ReadToEndAsync ();
					var stderrTask = process.StandardError.ReadToEndAsync ();

					process.WaitForExit ();

					var stdout = stdoutTask.Result;
					var stderr = stderrTask.Result;

					if (process.ExitCode != 0)
						tcs.TrySetException (new ExternalToolException (tool, stderr));
					else
						tcs.TrySetResult (stdout);
				} catch (Exception ex) {
					tcs.TrySetException (new ExternalToolException (tool, ex));
				} finally {
					cts.Dispose ();
				}
			});

			return tcs.Task;
		}

		protected override void Stop ()
		{
			if (process != null) {
				try {
					process.Kill ();
				} catch {
					;
				}
				process.Dispose ();
				process = null;
			}
			if (cts != null) {
				cts.Dispose ();
				cts = null;
			}
			if (output != null) {
				output.Dispose ();
				output = null;
			}
		}
	}
}
