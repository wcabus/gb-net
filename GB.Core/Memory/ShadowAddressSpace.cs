using System.Runtime.CompilerServices;

namespace GB.Core.Memory
{
    internal sealed class ShadowAddressSpace : IAddressSpace
    {
        private readonly IAddressSpace _addressSpace;
        private readonly int _echoStart;
        private readonly int _targetStart;
        private readonly int _length;

        public ShadowAddressSpace(IAddressSpace addressSpace, int echoStart, int targetStart, int length)
        {
            _addressSpace = addressSpace;
            _echoStart = echoStart;
            _targetStart = targetStart;
            _length = length;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Accepts(int address) => address >= _echoStart && address < _echoStart + _length;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetByte(int address, int value) => _addressSpace.SetByte(Translate(address), value);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int GetByte(int address) => _addressSpace.GetByte(Translate(address));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private int Translate(int address) => GetRelative(address) + _targetStart;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private int GetRelative(int address)
        {
            var i = address - _echoStart;
            if (i < 0 || i >= _length)
            {
                throw new ArgumentException();
            }

            return i;
        }
    }
}
