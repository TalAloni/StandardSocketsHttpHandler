
namespace System
{
    internal class BitOperations
    {
        public static int LeadingZeroCount(uint value)
        {
            const int numberOfBits = sizeof(uint) * 8; //compile time constant
            // Do the smearing
            value |= value >> 1;
            value |= value >> 2;
            value |= value >> 4;
            value |= value >> 8;
            value |= value >> 16;
            // Count the ones
            value -= value >> 1 & 0x55555555;
            value = (value >> 2 & 0x33333333) + (value & 0x33333333);
            value = (value >> 4) + value & 0x0f0f0f0f;
            value += value >> 8;
            value += value >> 16;
            return numberOfBits - (int)(value & 0x0000003f); //subtract # of 1s from 32
        }

        public static uint RotateLeft(uint value, int offset)
            => (value << offset) | (value >> (32 - offset));
    }
}
