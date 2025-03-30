using System.Runtime.CompilerServices;

namespace GB.Core.Memory
{
    internal sealed class MemoryRegisters : IAddressSpace
    {
        private readonly Dictionary<int, IRegister> _registers;
        private readonly Dictionary<int, int> _values = new Dictionary<int, int>();
        private readonly RegisterType[] _allowsWrite = { RegisterType.W, RegisterType.RW };
        private readonly RegisterType[] _allowsRead = { RegisterType.R, RegisterType.RW };

        public MemoryRegisters(params IRegister[] registers)
        {
            var map = new Dictionary<int, IRegister>();
            foreach (var r in registers)
            {
                if (map.ContainsKey(r.Address))
                {
                    throw new ArgumentException($"Two registers with the same address: {r.Address}");
                }

                map.Add(r.Address, r);
                _values.Add(r.Address, 0);
            }

            _registers = map;
        }

        private MemoryRegisters(MemoryRegisters original)
        {
            _registers = original._registers;
            _values = new Dictionary<int, int>(original._values);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int Get(IRegister reg)
        {
            return _registers.ContainsKey(reg.Address)
                ? _values[reg.Address]
                : throw new ArgumentException("Not a valid register: " + reg);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Put(IRegister reg, int value)
        {
            _values[reg.Address] = _registers.ContainsKey(reg.Address)
                ? value
                : throw new ArgumentException("Not a valid register: " + reg);
        }

        public MemoryRegisters Freeze() => new MemoryRegisters(this);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int PreIncrement(IRegister reg)
        {
            if (!_registers.ContainsKey(reg.Address))
            {
                throw new ArgumentException("Not a valid register: " + reg);
            }

            var value = _values[reg.Address] + 1;
            _values[reg.Address] = value;
            return value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Accepts(int address) => _registers.ContainsKey(address);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetByte(int address, int value)
        {
            var regType = _registers[address].Type;
            if (_allowsWrite.Contains(regType))
            {
                _values[address] = value;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int GetByte(int address)
        {
            var regType = _registers[address].Type;
            return _allowsRead.Contains(regType) ? _values[address] : 0xff;
        }
    }
}
