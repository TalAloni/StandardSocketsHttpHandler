using System;

namespace System.Collections.Generic
{
    internal static class QueueExtensions
    {
        public static bool TryDequeue<T>(this Queue<T> queue, out T value)
        {
            try
            {
                value = queue.Dequeue();
                return true;
            }
            catch(InvalidOperationException)
            {
                value = default(T);
                return false;
            }
        }
    }
}
