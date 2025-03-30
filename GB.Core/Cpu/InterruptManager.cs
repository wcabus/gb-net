using System.Runtime.CompilerServices;

namespace GB.Core.Cpu
{
    internal sealed class InterruptManager : IAddressSpace
    {
        private bool _ime;
        private readonly bool _gbc;
        private int _interruptFlag = 0xE1;
        private int _interruptEnabled;
        private int _pendingEnableInterrupts = -1;
        private int _pendingDisableInterrupts = -1;

        public InterruptManager(bool gameBoyColor)
        {
            _gbc = gameBoyColor;
        }

        public void EnableInterrupts(bool withDelay)
        {
            _pendingDisableInterrupts = -1;
            if (withDelay)
            {
                if (_pendingEnableInterrupts == -1)
                {
                    _pendingEnableInterrupts = 1;
                }
            }
            else
            {
                _pendingEnableInterrupts = -1;
                _ime = true;
            }
        }

        public void DisableInterrupts(bool withDelay)
        {
            _pendingEnableInterrupts = -1;
            if (withDelay && _gbc)
            {
                if (_pendingDisableInterrupts == -1)
                {
                    _pendingDisableInterrupts = 1;
                }
            }
            else
            {
                _pendingDisableInterrupts = -1;
                _ime = false;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void RequestInterrupt(InterruptType type) => _interruptFlag |= 1 << type.Ordinal;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void ClearInterrupt(InterruptType type) => _interruptFlag &= ~(1 << type.Ordinal);

        public void OnInstructionFinished()
        {
            if (_pendingEnableInterrupts != -1)
            {
                if (_pendingEnableInterrupts-- == 0)
                {
                    EnableInterrupts(false);
                }
            }

            if (_pendingDisableInterrupts != -1)
            {
                if (_pendingDisableInterrupts-- == 0)
                {
                    DisableInterrupts(false);
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsIme() => _ime;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsInterruptRequested() => (_interruptFlag & _interruptEnabled) != 0;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsHaltBug() => (_interruptFlag & _interruptEnabled & 0x1F) != 0 && !_ime;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Accepts(int address) => address == 0xFF0F || address == 0xFFFF;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetByte(int address, int value)
        {
            switch (address)
            {
                case 0xFF0F:
                    _interruptFlag = value | 0xE0;
                    break;

                case 0xFFFF:
                    _interruptEnabled = value;
                    break;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int GetByte(int address)
        {
            switch (address)
            {
                case 0xFF0F:
                    return _interruptFlag;

                case 0xFFFF:
                    return _interruptEnabled;

                default:
                    return 0xFF;
            }
        }

        public sealed class InterruptType
        {
            public static InterruptType None = new InterruptType(-1, -1);

            public static InterruptType VBlank = new InterruptType(0x0040, 0);
            public static InterruptType Lcdc = new InterruptType(0x0048, 1);
            public static InterruptType Timer = new InterruptType(0x0050, 2);
            public static InterruptType Serial = new InterruptType(0x0058, 3);
            public static InterruptType P1013 = new InterruptType(0x0060, 4);

            public int Ordinal { get; }

            public int Handler { get; }

            private InterruptType(int handler, int ordinal)
            {
                Handler = handler;
                Ordinal = ordinal;
            }

            public override string ToString()
            {
                return Ordinal switch
                {
                    0 => "VBlank",
                    1 => "LCD STAT",
                    2 => "Timer",
                    3 => "Serial",
                    4 => "Joypad",
                    _ => "Invalid Interrupt!"
                };
            }

            public static IEnumerable<InterruptType> Values
            {
                get
                {
                    yield return VBlank;
                    yield return Lcdc;
                    yield return Timer;
                    yield return Serial;
                    yield return P1013;
                }
            }
        }
    }
}
