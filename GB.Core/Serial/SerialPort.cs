using GB.Core.Cpu;
using System.Diagnostics;

namespace GB.Core.Serial
{
    internal sealed class SerialPort : IAddressSpace
    {
        private readonly ISerialEndpoint _serialEndpoint;
        private readonly InterruptManager _interruptManager;
        private readonly SpeedMode _speedMode;
        private readonly bool _gbc;
        private int _sb;
        private int _sc;
        private int _divider;
        private int _shiftClock;

        public SerialPort(InterruptManager interruptManager, ISerialEndpoint serialEndpoint, SpeedMode speedMode, bool gbc)
        {
            _interruptManager = interruptManager;
            _serialEndpoint = serialEndpoint;
            _speedMode = speedMode;
            _gbc = gbc;
        }

        public void Tick()
        {
            if (!TransferInProgress)
            {
                return;
            }

            if (++_divider >= Gameboy.TicksPerSec / 8192 / (FastMode ? 4 : 1) / _speedMode.GetSpeedMode())
            {
                var clockPulsed = false;
                if (InternalClockEnabled || _serialEndpoint.ExternalClockPulsed())
                {
                    _shiftClock++;
                    clockPulsed = true;
                }

                if (_shiftClock >= 8)
                {
                    TransferInProgress = false;
                    _interruptManager.RequestInterrupt(InterruptManager.InterruptType.Serial);
                    return;
                }

                if (clockPulsed)
                {
                    try
                    {
                        _sb = _serialEndpoint.Transfer(_sb);
                    }
                    catch (IOException e)
                    {
                        Debug.WriteLine($"Can't transfer byte {e}");
                        _sb = 0;
                    }
                }

                _divider = 0;
            }
        }

        public bool Accepts(int address)
        {
            return address is 0xFF01 or 0xFF02;
        }

        public void SetByte(int address, int value)
        {
            if (address == 0xFF01 && !TransferInProgress)
            {
                _sb = value;
            }
            else if (address == 0xFF02)
            {
                TransferInProgress = value.GetBit(7);
                FastMode = value.GetBit(1);
                InternalClockEnabled = value.GetBit(0);
            }
        }

        public int GetByte(int address)
        {
            if (address == 0xFF01)
            {
                return TransferInProgress ? 0x00 : _sb;
            } 
            if (address == 0xFF02)
            {
                return _sc | (_gbc ? 0b01111100 : 0b01111110);
            }
            throw new ArgumentException();
        }

        private bool TransferInProgress
        {
            get => (_sc & (1 << 7)) != 0;
            set
            {
                if (value)
                {
                    _sc = _sc.SetBit(7);
                    _divider = 0;
                    _shiftClock = 0;
                }
                else
                {
                    _sc = _sc.ClearBit(7);
                }
            }
        }

        private bool FastMode
        {
            get => (_sc & 2) != 0;
            set
            {
                if (value)
                {
                    _sc = _sc.SetBit(1);
                }
                else
                {
                    _sc = _sc.ClearBit(1);
                }
            }
        }

        private bool InternalClockEnabled
        {
            get => (_sc & 1) != 0;
            set
            {
                if (value)
                {
                    _sc = _sc.SetBit(0);
                }
                else
                {
                    _sc = _sc.ClearBit(0);
                }
            }
        }
    }
}
