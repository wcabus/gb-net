using System.Runtime.CompilerServices;

namespace GB.Core.Memory
{
    internal sealed class MemoryRegisters : IAddressSpace
    {
        private readonly int _baseAddress;
        private readonly int _range;
        private readonly int[] _values;
        private readonly RegisterType[] _types; // RegisterType per slot; uses (RegisterType)0xFF as "not a register"
        private const RegisterType InvalidType = (RegisterType)0xFF;

        public MemoryRegisters(params IRegister[] registers)
        {
            if (registers.Length == 0)
            {
                _baseAddress = 0;
                _range = 0;
                _values = Array.Empty<int>();
                _types = Array.Empty<RegisterType>();
                return;
            }

            int min = int.MaxValue, max = int.MinValue;
            foreach (var r in registers)
            {
                if (r.Address < min) min = r.Address;
                if (r.Address > max) max = r.Address;
            }

            _baseAddress = min;
            _range = max - min + 1;
            _values = new int[_range];
            _types = new RegisterType[_range];
            Array.Fill(_types, InvalidType);

            foreach (var r in registers)
            {
                var idx = r.Address - _baseAddress;
                if (_types[idx] != InvalidType)
                {
                    throw new ArgumentException($"Two registers with the same address: {r.Address}");
                }
                _types[idx] = r.Type;
            }
        }

        private MemoryRegisters(MemoryRegisters original)
        {
            _baseAddress = original._baseAddress;
            _range = original._range;
            _values = (int[])original._values.Clone();
            _types = original._types; // types are immutable, share the array
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int Get(IRegister reg)
        {
            var idx = reg.Address - _baseAddress;
            return _values[idx];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Put(IRegister reg, int value)
        {
            var idx = reg.Address - _baseAddress;
            _values[idx] = value;
        }

        public MemoryRegisters Freeze() => new MemoryRegisters(this);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int PreIncrement(IRegister reg)
        {
            var idx = reg.Address - _baseAddress;
            var value = _values[idx] + 1;
            _values[idx] = value;
            return value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Accepts(int address)
        {
            var idx = address - _baseAddress;
            return (uint)idx < (uint)_range && _types[idx] != InvalidType;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetByte(int address, int value)
        {
            var idx = address - _baseAddress;
            var regType = _types[idx];
            if (regType == RegisterType.W || regType == RegisterType.RW)
            {
                _values[idx] = value;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int GetByte(int address)
        {
            var idx = address - _baseAddress;
            var regType = _types[idx];
            return (regType == RegisterType.R || regType == RegisterType.RW) ? _values[idx] : 0xff;
        }
    }
}
