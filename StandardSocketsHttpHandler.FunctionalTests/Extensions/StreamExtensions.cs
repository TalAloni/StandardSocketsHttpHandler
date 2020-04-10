using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace System
{
    internal static class StreamExtensions
    {
        public static Task<int> ReadAsync(this Stream stream, Memory<byte> memory)
        {
            if (!MemoryMarshal.TryGetArray(memory, out ArraySegment<byte> buffer))
            {
                new NotSupportedException("This Memory does not support exposing the underlying array.");
            }
            return stream.ReadAsync(buffer.Array, buffer.Offset, buffer.Count);
        }
    }
}
