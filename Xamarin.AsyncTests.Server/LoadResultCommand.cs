//
// LoadResultCommand.cs
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
using System.Threading;
using System.Threading.Tasks;

namespace Xamarin.AsyncTests.Server
{
	class LoadResultCommand : Command, IServerCommand
	{
		public TestResult Result {
			get; set;
		}

		public long ObjectID {
			get; set;
		}

		public override void ReadXml (Serializer serializer, XmlReader reader)
		{
			base.ReadXml (serializer, reader);
			ObjectID = long.Parse (reader.GetAttribute ("ObjectID"));
			var subReader = reader.ReadSubtree ();
			Result = serializer.ReadResult (subReader);
		}

		public override void WriteXml (Serializer serializer, XmlWriter writer)
		{
			writer.WriteAttributeString ("ObjectID", ObjectID.ToString ());
			base.WriteXml (serializer, writer);
			serializer.Write (writer, Result);
		}

		public Task Run (ServerConnection connection, CancellationToken cancellationToken)
		{
			return connection.Run (this, cancellationToken);
		}
	}
}

