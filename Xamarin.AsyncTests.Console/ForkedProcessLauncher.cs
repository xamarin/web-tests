//
// ForkedProcessLauncher.cs
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
using System.Reflection;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace Xamarin.AsyncTests.Console
{
	using Remoting;

	public class ForkedProcessLauncher : IForkedProcessLauncher
	{
		public Task<ExternalProcess> LaunchApplication (string options, CancellationToken cancellationToken)
		{
			var assembly = Assembly.GetEntryAssembly ();
			var psi = new ProcessStartInfo (assembly.Location, options) {
				UseShellExecute = false
			};
			if (!psi.EnvironmentVariables.ContainsKey ("MONO_ENV_OPTIONS"))
				psi.EnvironmentVariables.Add ("MONO_ENV_OPTIONS", "--debug");
			return ProcessHelper.StartCommand (psi, cancellationToken);
		}

		public Task<ExternalProcess> LaunchApplication (string application, string arguments, CancellationToken cancellationToken)
		{
			var psi = new ProcessStartInfo (application, arguments) {
				UseShellExecute = false
			};

			foreach (var reserved in ReservedNames) {
				psi.EnvironmentVariables.Remove (reserved);
			}

			return ProcessHelper.StartCommand (psi, cancellationToken);
		}

		static readonly string[] ReservedNames = { "MONO_RUNTIME", "MONO_PATH", "MONO_GAC_PREFIX" };
	}
}
