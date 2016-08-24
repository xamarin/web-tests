//
// btls-cert-info.cs
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
using System;
using System.IO;
using System.Text;
using Mono.Btls.Interface;

namespace Mono.Btls
{
	static class BtlsCertInfo	
	{
		static void Main (string[] args)
		{
			BtlsX509 x509;
			if (args.Length == 0) {
				x509 = ReadFromStandardInput ();
			} else if (args.Length == 1) {
				x509 = ReadFromFile (args [0]);
			} else {
				Console.Error.WriteLine ("Certificate filename expected.");
				Environment.Exit (255);
				return;
			}

			var subject = x509.GetSubjectName ();
			Console.WriteLine ("Found certificate:\n  {0}\n", subject.GetString ());
			Console.WriteLine ("Hash: {0:x8}\nOld Hash: {1:x8}\n", subject.GetHash (), subject.GetHashOld ());
		}

		static BtlsX509 ReadFromStandardInput ()
		{
			var text = Console.In.ReadToEnd ();
			var data = Encoding.UTF8.GetBytes (text);
			return BtlsProvider.CreateNative (data, BtlsX509Format.PEM);
		}

		static BtlsX509 ReadFromFile (string filename)
		{
			var data = File.ReadAllBytes (filename);
			return BtlsProvider.CreateNative (data, BtlsX509Format.PEM);
		}
	}
}
