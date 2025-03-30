using System.Runtime.CompilerServices;

namespace GB.Core.Memory
{
    internal sealed class DmaAddressSpace : IAddressSpace
    {
        private readonly IAddressSpace _addressSpace;

        public DmaAddressSpace(IAddressSpace addressSpace)
        {
            _addressSpace = addressSpace;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Accepts(int address) => true;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetByte(int address, int value) { }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int GetByte(int address) =>
            address < 0xE000
                ? _addressSpace.GetByte(address)
                : _addressSpace.GetByte(address - 0x2000);
    }
}
