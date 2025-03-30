using System.Runtime.CompilerServices;

namespace GB.Core.Memory
{
    internal sealed class VoidAddressSpace : IAddressSpace
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Accepts(int address) => true;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetByte(int address, int value)
        {
            if (address < 0 || address > 0xFFFF)
            {
                throw new ArgumentException($"Invalid address: 0x{address:X}");
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int GetByte(int address)
        {
            if (address < 0 || address > 0xFFFF)
            {
                throw new ArgumentException($"Invalid address: 0x{address:X}");
            }

            return 0xFF;
        }
    }
}
