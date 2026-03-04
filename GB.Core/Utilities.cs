using System.Runtime.CompilerServices;

namespace GB.Core
{
    internal static class Utilities
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool GetBit(this int byteValue, int position) => (byteValue & (1 << position)) != 0;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int SetBit(this int byteValue, int position, bool value) => value ? SetBit(byteValue, position) : ClearBit(byteValue, position);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int SetBit(this int byteValue, int position) => (byteValue | (1 << position)) & 0xff;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int ClearBit(this int byteValue, int position) => ~(1 << position) & byteValue & 0xff;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int ToWord(this int[] data) => (data[1] << 8) | data[0];

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int ToSigned(this int byteValue) => (byteValue & (1 << 7)) == 0 ? byteValue : byteValue - 0x100;
    }
}
