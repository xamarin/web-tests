//
// ReflectionMethodInvoker.cs
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
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Reflection;

namespace Xamarin.AsyncTests.Framework.Reflection
{
	static class ReflectionMethodInvoker
	{
		internal static int GetTimeout (TestContext ctx, TestBuilder builder)
		{
			if (ctx.Settings.DisableTimeouts)
				return -1;
			if (builder is ReflectionTestCaseBuilder caseBuilder) {
				if (caseBuilder.Attribute.Timeout != 0)
					return caseBuilder.Attribute.Timeout;
				builder = caseBuilder.Fixture;
			}
			if (builder is ReflectionTestFixtureBuilder fixtureBuilder) {
				if (fixtureBuilder.Attribute.Timeout != 0)
					return fixtureBuilder.Attribute.Timeout;
			}
			return 30000;
		}

		internal static object InvokeMethod (
			TestContext ctx, TestBuilder builder, TestInstance instance,
			MethodInfo method, bool expectException,
			CancellationToken cancellationToken)
		{
			return Invoke (
				ctx, builder, instance, method, expectException, cancellationToken);
		}

		internal static object InvokeConstructor (
			TestContext ctx, ReflectionFixtureHost host,
			TestInstance instance)
		{
			return Invoke (
				ctx, host.Builder, instance, host.Constructor,
				false, CancellationToken.None);
		}

		static object Invoke (
			TestContext ctx, TestBuilder builder, TestInstance instance,
			MethodBase method, bool expectException,
			CancellationToken cancellationToken)
		{
			var args = new LinkedList<object> ();

			CancellationTokenSource timeoutCts = null;
			CancellationTokenSource methodCts = null;
			CancellationToken methodToken;

			var disposableContext = ctx.CreateDisposable ();

			var timeout = GetTimeout (ctx, builder);
			if (method is ConstructorInfo) {
				methodToken = CancellationToken.None;
			} else if (timeout > 0) {
				timeoutCts = new CancellationTokenSource (timeout);
				methodCts = CancellationTokenSource.CreateLinkedTokenSource (cancellationToken, timeoutCts.Token);
				methodCts.CancelAfter (timeout);
				methodToken = methodCts.Token;
			} else {
				methodToken = cancellationToken;
			}

			var parameters = method.GetParameters ();

			string builderName;
			if (builder is ReflectionTestCaseBuilder caseBuilder)
				builderName = $"{caseBuilder.Fixture.Name}.{builder.Name}";
			else
				builderName = builder.Name;

			var me = $"{DebugHelper.FormatType (builder)}({builderName}) INVOKE";

			ctx.LogDebug (10, $"{me}: {DebugHelper.FormatMethod (method)} {timeout}");
			TestInstance.LogDebug (ctx, instance, 10);

			var index = parameters.Length - 1;
			while (index >= 0) {
				ctx.LogDebug (10, $"{me} #1: {index} {instance}");
				if (instance is TestBuilderInstance) {
					instance = instance.Parent;
					continue;
				}
				if (instance is ReflectionPropertyInstance) {
					instance = instance.Parent;
					continue;
				}

				var param = parameters[index];
				var paramType = param.ParameterType;
				var paramTypeInfo = paramType.GetTypeInfo ();
				index--;

				if (paramType.Equals (typeof (CancellationToken))) {
					if (method is ConstructorInfo)
						throw new InternalErrorException ();
					args.AddFirst (methodToken);
					continue;
				}
				if (paramType.Equals (typeof (TestContext))) {
					args.AddFirst (disposableContext);
					continue;
				}

				if (instance is ReflectionFixtureInstance fixtureInstance) {
					if (!paramTypeInfo.IsAssignableFrom (fixtureInstance.Host.Builder.Type))
						throw new InternalErrorException ();
					if (fixtureInstance.Instance == null)
						throw new InternalErrorException ();
					args.AddFirst (fixtureInstance.Instance);
					fixtureInstance.Host.Unwind (ref instance);
					continue;
				}

				if (instance is ForkedTestInstance) {
					var forkedInstance = ForkedTestHost.GetInstance (ref instance);
					args.AddFirst (forkedInstance);
					continue;
				}

				if (instance is HeavyTestInstance heavyInstance) {
					args.AddFirst (heavyInstance.Instance);
					instance = instance.Parent;
					continue;
				}

				var parameterizedInstance = instance as ParameterizedTestInstance;
				if (parameterizedInstance == null)
					throw new InternalErrorException ();

				if (!paramTypeInfo.IsAssignableFrom (parameterizedInstance.Host.ParameterTypeInfo))
					throw new InternalErrorException ();

				var current = parameterizedInstance.Current;
				args.AddFirst (current.Value);

				instance = instance.Parent;
			}

			ctx.LogDebug (10, $"{me} #2");
			TestInstance.LogDebug (ctx, instance, 10);

			object thisInstance = null;
			bool seenFixtureInstance = false;

			if (method.IsConstructor)
				instance = null;

			while (instance != null) {
				if (instance is ReflectionFixtureInstance fixtureInstance) {
					fixtureInstance.Host.Unwind (ref instance);
					if (!method.IsStatic)
						thisInstance = fixtureInstance.Instance ?? throw new InternalErrorException ();
					seenFixtureInstance = true;
					continue;
				}

				if (instance is TestBuilderInstance) {
					instance = instance.Parent;
					continue;
				}

				var host = instance.Host;
				if (ReflectionHelper.IsFixedTestHost (host)) {
					instance = instance.Parent;
					continue;
				}

				if (host is RepeatedTestHost) {
					instance = instance.Parent;
					continue;
				}

				if (host is ForkedTestHost) {
					instance = instance.Parent;
					continue;
				}

				if (host is FixturePropertyHost propertyHost) {
					if (seenFixtureInstance && !propertyHost.IsStatic)
						throw new InternalErrorException ($"Unexpected non-static fixture property on stack after seeing fixture instance: `{instance}'.");
					instance = instance.Parent;
					continue;
				}

				if (seenFixtureInstance)
					throw new InternalErrorException ($"Unexpected instance on stack after seeing fixture instance: `{instance}'.");

				if (host is ReflectionPropertyHost) {
					instance = instance.Parent;
					continue;
				}

				throw new InternalErrorException ($"Unexpected instance on stack: `{instance}'.");
			}

			try {
				return InvokeInner (
					ctx, method, thisInstance,
					args.ToArray (), expectException);
			} finally {
				if (timeoutCts != null)
					timeoutCts.Dispose ();
				disposableContext.ClearAutoDisposeBag ();
			}
		}

		static object InvokeInner (
			TestContext ctx, MethodBase member, object instance,
			object[] args, bool expectException)
		{
			if (member is MethodInfo method)
				return InvokeMethod (
					ctx, method, instance, args, expectException);
			if (member is ConstructorInfo constructor)
				return CreateInstance (
					ctx, constructor, args, expectException);
			throw new InternalErrorException ();
		}

		[StackTraceEntryPoint]
		static object InvokeMethod (
			TestContext ctx, MethodInfo method, object instance,
			object[] args, bool expectException)
		{
			try {
				return method.Invoke (instance, args);
			} catch (TargetInvocationException ex) {
				if (expectException)
					throw;
				ctx.OnError (ex.InnerException);
				return null;
			}
		}

		[StackTraceEntryPoint]
		static object CreateInstance (
			TestContext ctx, ConstructorInfo ctor,
			object[] args, bool expectException)
		{
			try {
				return Activator.CreateInstance (ctor.DeclaringType, args);
			} catch (TargetInvocationException ex) {
				if (expectException)
					throw;
				ctx.OnError (ex.InnerException);
				return null;
			}
		}
	}
}
