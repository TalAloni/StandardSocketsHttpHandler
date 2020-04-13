using System;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace System.IO
{
    internal static class StreamExtensions
    {
        public static int Read(this Stream stream, Span<byte> buffer)
        {
            byte[] temp = new byte[buffer.Length];
            int count = stream.Read(temp, 0, buffer.Length);
            for (int index = 0; index < count; index++)
            {
                buffer[index] = temp[index];
            }
            return count;
        }

        public static Task<int> ReadAsync(this Stream stream, Memory<byte> memory)
        {
            return stream.ReadAsync(memory, CancellationToken.None);
        }

        public static Task<int> ReadAsync(this Stream stream, Memory<byte> memory, CancellationToken cancellationToken)
        {
            if (!MemoryMarshal.TryGetArray(memory, out ArraySegment<byte> buffer))
            {
                new NotSupportedException("This Memory does not support exposing the underlying array.");
            }
            return stream.ReadAsync(buffer.Array, buffer.Offset, buffer.Count);
        }

        public static void Write(this Stream stream, ReadOnlySpan<byte> buffer)
        {
            byte[] temp = buffer.ToArray();
            stream.Write(temp, 0, temp.Length);
        }

        public static Task WriteAsync(this Stream stream, ReadOnlyMemory<byte> memory)
        {
            return stream.WriteAsync(memory, CancellationToken.None);
        }

        public static Task WriteAsync(this Stream stream, ReadOnlyMemory<byte> memory, CancellationToken cancellationToken)
        {
            if (!MemoryMarshal.TryGetArray(memory, out ArraySegment<byte> buffer))
            {
                new NotSupportedException("This Memory does not support exposing the underlying array.");
            }
            return stream.WriteAsync(buffer.Array, buffer.Offset, buffer.Count, cancellationToken);
        }
    }
}
