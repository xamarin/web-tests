//
// Mono.Security.BitConverterLE.cs
//  Like System.BitConverter but always little endian
//
// Author:
//   Bernie Solomon
//

//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
// 
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

using System;

namespace Mono.Security
{
	internal static class BitConverterLE
	{
		internal static ushort ToUInt16 (byte[] value, int startIndex)
		{
			ushort ret;
			unchecked {
				if (BitConverter.IsLittleEndian) {
					var lo = (ushort)value [startIndex];
					var hi = (ushort)(value [startIndex + 1] << 8);
					ret = (ushort)(lo + hi);
				} else {
					var hi = (ushort)value [startIndex];
					var lo = (ushort)(value [startIndex + 1] << 8);
					ret = (ushort)(lo + hi);
				}
			}

			return ret;
		}

		internal static uint ToUInt32 (byte[] value, int startIndex)
		{
			uint ret;
			unchecked {
				if (BitConverter.IsLittleEndian) {
					var lo = ToUInt16 (value, startIndex);
					var hi = ToUInt16 (value, startIndex + 2) << 16;
					ret = (uint)(lo + hi);
				} else {
					var hi = ToUInt16 (value, startIndex);
					var lo = ToUInt16 (value, startIndex + 2) << 16;
					ret = (uint)(lo + hi);
				}
			}

			return ret;
		}
	}
}
