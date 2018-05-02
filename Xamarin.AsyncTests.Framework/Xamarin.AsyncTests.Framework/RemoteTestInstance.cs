//
// RemoteTestInstance.cs
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
using System.Xml.Linq;

namespace Xamarin.AsyncTests.Framework
{
	using Remoting;
	using Reflection;

	abstract class RemoteTestInstance : TestInstance
	{
		protected RemoteTestInstance (RemoteTestHost host, TestNode node, TestInstance parent)
			: base (host, node, parent)
		{
		}

		internal abstract RemoteTestValue CurrentParameter {
			get;
		}

		internal XElement CustomParameter {
			get;
			private set;
		}

		internal long ObjectID {
			get;
			private set;
		}

		internal bool IsForked {
			get;
			private set;
		}

		internal ForkedObjectClient ObjectClient {
			get;
			private set;
		}

		internal ForkedObjectServant ObjectServant {
			get;
			private set;
		}

		internal Connection Connection {
			get;
			private set;
		}

		const string Category = "forked-instance";

		internal sealed override TestParameterValue GetCurrentParameter () => CurrentParameter;

		public override void Initialize (TestContext ctx)
		{
			base.Initialize (ctx);
			
			if (Node.CustomParameter != null) {
				CustomParameter = new XElement (Node.CustomParameter);
				var forkedAttr = Node.CustomParameter.Attribute ("Forked");
				if (forkedAttr != null)
					IsForked = bool.Parse (forkedAttr.Value);
				ObjectID = long.Parse (Node.CustomParameter.Attribute ("ObjectID").Value);
				Connection = GetRemoteConnection (ctx, this);
				if (Connection != null)
					ObjectClient = new ForkedObjectClient ((ServerConnection)Connection, ObjectID);
			} else {
				CustomParameter = TestNode.CreateCustomParameterNode ();
				ObjectID = Connection.GetNextObjectId ();
			}
			CustomParameter.SetAttributeValue ("ObjectID", ObjectID);

			if (Node.CustomParameter != null)
				Deserialize (ctx);
			else
				Serialize (ctx);
		}

		protected virtual void Serialize (TestContext ctx)
		{
			
		}

		protected virtual void Deserialize (TestContext ctx)
		{
			
		}

		internal static void WalkStackAndSerialize (TestContext ctx, TestInstance instance)
		{
			while (instance != null) {
				if (instance is RemoteTestInstance remote) {
					remote.CustomParameter.SetAttributeValue ("Forked", true);
					remote.Serialize (ctx);
				}

				instance = instance.Parent;
			}
		}

		internal void RegisterForkedServant (TestContext ctx, Connection connection)
		{
			ObjectServant = new ForkedObjectServant (ctx, connection, ObjectID, this);
			connection.RegisterObjectServant (ObjectServant, ObjectID);
		}

		internal static ReflectionTestSession GetCurrentSession (TestInstance instance)
		{
			while (instance != null) {
				if (instance is TestBuilderInstance builderInstance &&
				    builderInstance.Builder is TestSuiteBuilder suiteBuilder &&
				    suiteBuilder.Suite.Session is ReflectionTestSession reflectionSession)
					return reflectionSession;
				instance = instance.Parent;
			}

			return null;
		}

		internal static Connection GetRemoteConnection (TestContext ctx, TestInstance instance)
		{
			LogDebug (ctx, instance, 2);
			var session = GetCurrentSession (instance);
			return session?.RemoteConnection;
		}
	}
}
