//
// RemotingHelper.cs
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
using System.Threading;
using System.Threading.Tasks;

namespace Xamarin.AsyncTests.Remoting
{
	using Framework;

	public static class RemotingHelper
	{
		public static async Task<string> HandleMessage (TestServer server, string message, CancellationToken cancellationToken)
		{
			var element = TestSerializer.Deserialize (message);
			var command = Command.Create (server.Connection, element);
			server.Connection.Debug ($"HANDLE MESSAGE: {message} {command}");

			Response response;
			try {
				response = await command.Run (server.Connection, cancellationToken).ConfigureAwait (false);
			} catch (Exception ex) {
				response = new Response {
					ObjectID = command.ResponseID, Success = false, Error = ex.ToString ()
				};
			}

			server.Connection.Debug ($"HANDLE MESSAGE DONE: {message} {response}");

			if (command.IsOneWay || command.ResponseID == 0 || response == null)
				return null;

			var responseElement = response.Write (server.Connection);
			var serialized = TestSerializer.Serialize (responseElement);
			return serialized;
		}
	}
}
