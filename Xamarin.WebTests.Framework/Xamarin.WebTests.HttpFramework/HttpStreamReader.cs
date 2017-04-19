//
// System.IO.StreamReader.cs
//
// Authors:
//   Dietmar Maurer (dietmar@ximian.com)
//   Miguel de Icaza (miguel@ximian.com) 
//   Marek Safar (marek.safar@gmail.com)
//
// (C) Ximian, Inc.  http://www.ximian.com
// Copyright (C) 2004 Novell (http://www.novell.com)
// Copyright 2011, 2013 Xamarin Inc.
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
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Xamarin.WebTests.HttpFramework
{
	public class HttpStreamReader : IDisposable
	{
		const int BufferSize = 4096;

		//
		// The input buffer
		//
		byte[] input_buffer;
		int input_length;
		int input_pos;

		StringBuilder line_builder;

		Stream base_stream;
		readonly bool leave_open;

		private HttpStreamReader() {}

		public HttpStreamReader (Stream stream)
			: this (stream, true)
		{
		}

		public HttpStreamReader (Stream stream, bool leaveOpen)
		{
			this.base_stream = stream;
			this.leave_open = leaveOpen;

			input_buffer = new byte [BufferSize];
			input_length = 0;
			input_pos = 0;
		}

		public async Task<bool> IsEndOfStream (CancellationToken cancellationToken)
		{
			return await Peek (cancellationToken).ConfigureAwait (false) < 0;
		}

		void Dispose (bool disposing)
		{
			if (disposing && base_stream != null && !leave_open)
				base_stream.Dispose ();
			
			input_buffer = null;
			base_stream = null;
		}

		public async Task<int> Peek (CancellationToken cancellationToken)
		{
			if (input_pos >= input_length && await ReadBufferAsync (cancellationToken).ConfigureAwait (false) == 0)
				return -1;

			return input_buffer [input_pos];
		}

		internal bool DataAvailable ()
		{
			return input_pos < input_length;
		}
		
		bool foundCR;
		int FindNextEOL ()
		{
			char c = '\0';
			for (; input_pos < input_length; input_pos++) {
				c = (char)input_buffer [input_pos];
				if (c == '\n') {
					input_pos++;
					int res = (foundCR) ? (input_pos - 2) : (input_pos - 1);
					if (res < 0)
						res = 0; // if a new buffer starts with a \n and there was a \r at
							// the end of the previous one, we get here.
					foundCR = false;
					return res;
				} else if (foundCR) {
					foundCR = false;
					if (input_pos == 0)
						return -2; // Need to flush the current buffered line.
							   // This is a \r at the end of the previous decoded buffer that
							   // is not followed by a \n in the current decoded buffer.
					return input_pos - 1;
				}

				foundCR = (c == '\r');
			}

			return -1;
		}

		String GetString (int index, int count)
		{
			var chars = new char[count];
			for (int i = 0; i < count; i++)
				chars[i] = (char)input_buffer[index + i];
			return new string (chars);
		}

		public async Task<int> ReadAsync (byte[] buffer, int index, int count, CancellationToken cancellationToken)
		{
			if (buffer == null)
				throw new ArgumentNullException (nameof (buffer));
			if (index < 0)
				throw new ArgumentOutOfRangeException (nameof (index), "< 0");
			if (count < 0)
				throw new ArgumentOutOfRangeException (nameof (count), "< 0");
			if (index > buffer.Length - count)
				throw new ArgumentException ("index + count > buffer.Length");

			int chars_read = 0;
			bool done = false;

			while (count > 0 && !done) {
				cancellationToken.ThrowIfCancellationRequested ();
				if (input_pos >= input_length) {
					var len = await ReadBufferAsync (cancellationToken).ConfigureAwait (false);
					if (len <= 0)
						return chars_read;
					if (len < BufferSize)
						done = true;
				}

				int cch = Math.Min (input_length - input_pos, count);
				Array.Copy (input_buffer, input_pos, buffer, index, cch);
				input_pos += cch;
				index += cch;
				count -= cch;
				chars_read += cch;
			}

			return chars_read;
		}

		public async Task<int> ReadAsync (char[] buffer, int index, int count, CancellationToken cancellationToken)
		{
			if (buffer == null)
				throw new ArgumentNullException (nameof (buffer));
			if (index < 0)
				throw new ArgumentOutOfRangeException (nameof (index), "< 0");
			if (count < 0)
				throw new ArgumentOutOfRangeException (nameof (count), "< 0");
			if (index > buffer.Length - count)
				throw new ArgumentException ("index + count > buffer.Length");

			var byteBuffer = new byte[count];
			var ret = await ReadAsync (byteBuffer, 0, byteBuffer.Length, cancellationToken).ConfigureAwait (false);
			if (ret <= 0)
				return ret;

			Array.Copy (byteBuffer, 0, buffer, index, ret);
			return ret;
		}

		public async Task<string> ReadLineAsync (CancellationToken cancellationToken)
		{
			cancellationToken.ThrowIfCancellationRequested ();
			if (input_pos >= input_length && await ReadBufferAsync (cancellationToken).ConfigureAwait (false) == 0)
				return null;

			int begin = input_pos;
			int end = FindNextEOL ();
			if (end < input_length && end >= begin)
				return GetString (begin, end - begin);
			if (end == -2)
				return line_builder.ToString (0, line_builder.Length);

			if (line_builder == null)
				line_builder = new StringBuilder ();
			else
				line_builder.Length = 0;

			while (true) {
				cancellationToken.ThrowIfCancellationRequested ();
				if (foundCR) // don't include the trailing CR if present
					input_length--;

				line_builder.Append (GetString (begin, input_length - begin));
				if (await ReadBufferAsync (cancellationToken).ConfigureAwait (false) == 0)
					return line_builder.ToString (0, line_builder.Length);

				begin = input_pos;
				end = FindNextEOL ();
				if (end < input_length && end >= begin) {
					line_builder.Append (GetString (begin, end - begin));
					return line_builder.ToString (0, line_builder.Length);
				}

				if (end == -2)
					return line_builder.ToString (0, line_builder.Length);
			}
		}

		public async Task<string> ReadToEndAsync (CancellationToken cancellationToken)
		{
			StringBuilder text = new StringBuilder ();

			do {
				text.Append (GetString (input_pos, input_length - input_pos));
			} while (await ReadBufferAsync (cancellationToken).ConfigureAwait (false) != 0);

			return text.ToString ();
		}

		async Task<int> ReadBufferAsync (CancellationToken cancellationToken)
		{
			cancellationToken.ThrowIfCancellationRequested ();

			input_pos = 0;
			input_length = await base_stream.ReadAsync (input_buffer, 0, BufferSize, cancellationToken).ConfigureAwait (false);
			return input_length;
		}

		public void Dispose ()
		{
			Dispose (true);
			GC.SuppressFinalize (this);
		}
	}
}
