namespace GB.Core.Memory
{
    internal sealed class GameboyColorRam : IAddressSpace
    {
        private readonly int[] _ram = new int[7 * 0x1000];
        private int _svbk;

        public bool Accepts(int address) => address is 0xFF70 or >= 0xD000 and < 0xE000;

        public void SetByte(int address, int value)
        {
            if (address == 0xFF70)
            {
                _svbk = value;
            }
            else
            {
                _ram[Translate(address)] = value;
            }
        }

        public int GetByte(int address) => address == 0xFF70 ? _svbk : _ram[Translate(address)];

        private int Translate(int address)
        {
            var ramBank = _svbk & 0x7;
            if (ramBank == 0)
            {
                ramBank = 1;
            }

            var result = address - 0xD000 + (ramBank - 1) * 0x1000;
            if (result < 0 || result >= _ram.Length)
            {
                throw new ArgumentException();
            }

            return result;
        }
    }
}
