//
// ConsoleClient.cs
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
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Xamarin.AsyncTests.Client
{
	using Framework;
	using Server;

	public class ConsoleClient : ClientConnection
	{
		readonly TestContext context;

		public override TestContext Context {
			get { return context; }
		}

		public ConsoleClient (Stream stream)
			: base (stream)
		{
			context = new TestContext ();
		}

		void Debug (string message, params object[] args)
		{
			Debug (string.Format (message, args));
		}

		void Debug (string message)
		{
			System.Diagnostics.Debug.WriteLine (message);
		}

		public void Run ()
		{
			MainLoop ();
		}

		public async Task<TestResult> RunTest ()
		{
			var assembly = typeof(Xamarin.AsyncTests.Sample.SimpleTest).Assembly;
			var suite = await TestSuite.LoadAssembly (assembly);
			var result = new TestResult (new TestName (assembly.FullName));
			var success = await suite.Run (Context, result, CancellationToken.None);
			Debug ("DONE RUNNING: {0}", success);
			return result;
		}

		#region implemented abstract members of ClientConnection

		#endregion

		#region implemented abstract members of Connection

		protected override Task OnHello (CancellationToken cancellationToken)
		{
			return Task.Run (() => {
				Debug ("HELLO WORLD!");
			});
		}

		protected override void OnMessage (string message)
		{
			Debug (message);
		}

		protected override void OnDebug (int level, string message)
		{
			Debug (message);
		}

		#endregion
	}
}

