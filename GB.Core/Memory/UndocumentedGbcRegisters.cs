namespace GB.Core.Memory
{
    internal sealed class UndocumentedGbcRegisters : IAddressSpace
    {
        private readonly Ram _ram = new(0xFF72, 6);
        private int _xFF6C;

        public UndocumentedGbcRegisters()
        {
            _xFF6C = 0xFE;
            _ram.SetByte(0xFF74, 0xFF);
            _ram.SetByte(0xFF75, 0x8F);
        }

        public bool Accepts(int address) => address == 0xFF6C || _ram.Accepts(address);

        public void SetByte(int address, int value)
        {
            switch (address)
            {
                case 0xFF6C:
                    _xFF6C = 0xFE | (value & 1);
                    break;

                case 0xFF72:
                case 0xFF73:
                case 0xFF74:
                    _ram.SetByte(address, value);
                    break;

                case 0xFF75:
                    _ram.SetByte(address, 0x8F | (value & 0b01110000));
                    break;
            }
        }

        public int GetByte(int address)
        {
            if (address == 0xFF6C)
            {
                return _xFF6C;
            }

            if (!_ram.Accepts(address))
            {
                throw new ArgumentException();
            }

            return _ram.GetByte(address);
        }
    }
}
