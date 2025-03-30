using GB.Core.Cpu;

namespace GB.Core
{
    internal sealed class Timer : IAddressSpace
    {
        private readonly SpeedMode _speedMode;
        private readonly InterruptManager _interruptManager;
        private static readonly int[] FreqToBit = { 9, 3, 5, 7 };

        private int _div;
        private int _tac;
        private int _tma;
        private int _tima;
        private bool _previousBit;
        private bool _overflow;
        private int _ticksSinceOverflow;

        public Timer(InterruptManager interruptManager, SpeedMode speedMode)
        {
            _interruptManager = interruptManager;
            _speedMode = speedMode;
        }

        public void Tick()
        {
            UpdateDiv((_div + 1) & 0xFFFF);
            if (!_overflow)
            {
                return;
            }

            _ticksSinceOverflow++;
            if (_ticksSinceOverflow == 4)
            {
                _interruptManager.RequestInterrupt(InterruptManager.InterruptType.Timer);
            }

            if (_ticksSinceOverflow == 5)
            {
                _tima = _tma;
            }

            if (_ticksSinceOverflow == 6)
            {
                _tima = _tma;
                _overflow = false;
                _ticksSinceOverflow = 0;
            }
        }

        private void IncTima()
        {
            _tima++;
            _tima %= 0x100;
            if (_tima == 0)
            {
                _overflow = true;
                _ticksSinceOverflow = 0;
            }
        }

        private void UpdateDiv(int newDiv)
        {
            _div = newDiv;
            int bitPos = FreqToBit[_tac & 0b11];
            bitPos <<= _speedMode.GetSpeedMode() - 1;
            bool bit = (_div & (1 << bitPos)) != 0;
            bit &= (_tac & (1 << 2)) != 0;
            if (!bit && _previousBit)
            {
                IncTima();
            }

            _previousBit = bit;
        }

        public bool Accepts(int address) => address >= 0xFF04 && address <= 0xFF07;

        public void SetByte(int address, int value)
        {
            switch (address)
            {
                case 0xFF04: // DIV
                    UpdateDiv(0);
                    break;

                case 0xFF05: // TIMA
                    if (_ticksSinceOverflow < 5)
                    {
                        _tima = value;
                        _overflow = false;
                        _ticksSinceOverflow = 0;
                    }

                    break;

                case 0xFF06: // TMA
                    _tma = value;
                    break;

                case 0xFF07: // TAC
                    _tac = value;
                    break;
            }
        }

        public int GetByte(int address)
        {
            return address switch
            {
                0xFF04 => _div >> 8,
                0xFF05 => _tima,
                0xFF06 => _tma,
                0xFF07 => _tac | 0b11111000,
                _ => throw new ArgumentException()
            };
        }
    }
}
