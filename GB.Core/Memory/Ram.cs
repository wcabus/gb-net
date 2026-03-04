using System.Runtime.CompilerServices;

namespace GB.Core.Memory
{
    internal sealed class Ram : IAddressSpace
    {
        private readonly byte[] _space;
        private readonly int _length;
        private readonly int _offset;

        public Ram(int offset, int length)
        {
            _space = new byte[length];
            _length = length;
            _offset = offset;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Accepts(int address) => address >= _offset && address < _offset + _length;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetByte(int address, int value) => _space[address - _offset] = (byte)value;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int GetByte(int address)
        {
            var index = address - _offset;
            if (index < 0 || index >= _space.Length)
            {
                throw new IndexOutOfRangeException("Address: " + address);
            }

            return _space[index];
        }
    }
}
