using System;
using System.Buffers;
using System.Text;

namespace System.Net.Http
{
    internal class Utf8Utils
    {
        public static OperationStatus ToUtf16(ReadOnlySpan<byte> source, Span<char> destination, out int bytesRead, out int charsWritten)
        {
            bytesRead = source.Length;
            charsWritten = 0;
            string input = UTF8Encoding.UTF8.GetString(source);
            // We convert back the string to bytes to verify that we received valid UTF-8 byte sequence
            byte[] utf8Bytes = UTF8Encoding.UTF8.GetBytes(input);
            if (!source.SequenceEqual(new ReadOnlySpan<byte>(utf8Bytes)))
            {
                return OperationStatus.InvalidData;
            }

            charsWritten = input.Length;
            for(int index = 0; index < input.Length; index++)
            {
                destination[index] = input[index];
            }

            return OperationStatus.Done;
        }
    }
}
