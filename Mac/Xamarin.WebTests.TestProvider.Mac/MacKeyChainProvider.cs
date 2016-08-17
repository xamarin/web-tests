//
// MacKeyChainProvider.cs
//
// Author:
//       Martin Baulig <martin.baulig@xamarin.com>
//
// Copyright (c) 2016 Xamarin Inc. (http://www.xamarin.com)

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
#if __XAMMAC__
using System;
using System.IO;
using System.Runtime.InteropServices;
using Foundation;

namespace Xamarin.WebTests.TestProvider.Mac
{
	public static class MacKeyChainProvider
	{
		[DllImport ("/System/Library/Frameworks/Security.framework/Security", CharSet = CharSet.Ansi)]
		extern static int SecKeychainCreate (string path, int passwordLen, IntPtr password, bool promptUser, IntPtr initialAccess, out IntPtr keychain);

		[DllImport ("/System/Library/Frameworks/Security.framework/Security", CharSet = CharSet.Ansi)]
		extern static int SecKeychainOpen (string path, out IntPtr keychain);

		[DllImport ("/System/Library/Frameworks/Security.framework/Security", CharSet = CharSet.Ansi)]
		extern static int SecKeychainUnlock (IntPtr keychain, int passwordLength, IntPtr password, bool usePassword);

		[DllImport ("/System/Library/Frameworks/Security.framework/Security")]
		extern static int SecKeychainSetDefault (IntPtr keychain);

		[DllImport ("/System/Library/Frameworks/Security.framework/Security")]
		extern static int SecKeychainGetUserInteractionAllowed ([MarshalAs (UnmanagedType.I1)] out bool state);

		internal static IntPtr CreateKeyChain (string path, string password)
		{
			IntPtr keychain;
			int passwordLen = 0;
			IntPtr passwordPtr = IntPtr.Zero;

			if (password != null) {
				passwordLen = password.Length;
				passwordPtr = Marshal.StringToHGlobalAnsi (password);
			}

			try {
				var ret = SecKeychainCreate (path, passwordLen, passwordPtr, false, IntPtr.Zero, out keychain);
				Console.Error.WriteLine ("CREATE KEYCHAIN: {0} - {1:x}", ret, keychain.ToInt64 ());
				if (ret != 0)
					throw new NotSupportedException ();
				return keychain;
			} finally {
				if (passwordPtr != IntPtr.Zero)
					Marshal.FreeHGlobal (passwordPtr);
			}
		}

		internal static IntPtr OpenKeyChain (string path)
		{
			IntPtr keychain;
			var ret = SecKeychainOpen (path, out keychain);
			if (ret != 0)
				throw new NotSupportedException ();
			return keychain;
		}

		internal static void UnlockKeyChain (IntPtr keychain, string password)
		{
			int passwordLen = 0;
			IntPtr passwordPtr = IntPtr.Zero;

			if (password != null) {
				passwordLen = password.Length;
				passwordPtr = Marshal.StringToHGlobalAnsi (password);
			}

			try {
				var ret = SecKeychainUnlock (keychain, passwordLen, passwordPtr, true);
				Console.Error.WriteLine ("UNLOCK KEYCHAIN: {0} - {1:x}", ret, keychain.ToInt64 ());
				if (ret != 0)
					throw new NotSupportedException ();
			} finally {
				if (passwordPtr != IntPtr.Zero)
					Marshal.FreeHGlobal (passwordPtr);
			}
		}

		internal static void SetDefaultKeyChain (IntPtr keychain)
		{
			var ret = SecKeychainSetDefault (keychain);
			if (ret != 0)
				throw new NotSupportedException ();
		}

#if WRENCH
		internal static void CreateAndSelectCustomKeyChain ()
		{
			bool status;
			int ret = SecKeychainGetUserInteractionAllowed (out status);
			Console.Error.WriteLine ("USER INTERACTION ALLOWED: {0} {1}", ret, status);
			var home = Environment.GetFolderPath (Environment.SpecialFolder.UserProfile);
			var path = Path.Combine (home, "Library", "Keychains", "web-tests.keychain");
			IntPtr keychain;
			if (!File.Exists (path))
				keychain = CreateKeyChain (path, "monkey");
			else {
				keychain = OpenKeyChain (path);
				UnlockKeyChain (keychain, "monkey");
			}
			SetDefaultKeyChain (keychain);
		}
#endif
	}
}
#endif

