using GB.Core.Memory;

namespace GB.Core.Sound
{
    internal sealed class SoundMode3 : SoundModeBase
    {
        private static readonly int[] DmgWave =
        {
            0x84, 0x40, 0x43, 0xAA, 0x2D, 0x78, 0x92, 0x3C,
            0x60, 0x59, 0x59, 0xB0, 0x34, 0xB8, 0x2E, 0xDA
        };

        private static readonly int[] CgbWave =
        {
            0x00, 0xFF, 0x00, 0xFF, 0x00, 0xFF, 0x00, 0xFF,
            0x00, 0xFF, 0x00, 0xFF, 0x00, 0xFF, 0x00, 0xFF
        };

        private readonly Ram _waveRam = new Ram(0xFF30, 0x10);
        private int _freqDivider;
        private int _lastOutput;
        private int _i;
        private int _ticksSinceRead = 65536;
        private int _lastReadAddress;
        private int _buffer;
        private bool _triggered;

        public SoundMode3(bool gbc) : base(0xFF1A, 256, gbc)
        {
            foreach (var v in gbc ? CgbWave : DmgWave)
            {
                _waveRam.SetByte(0xFF30, v);
            }
        }

        public override bool Accepts(int address) => _waveRam.Accepts(address) || base.Accepts(address);

        public override int GetByte(int address)
        {
            if (!_waveRam.Accepts(address))
            {
                return base.GetByte(address);
            }

            if (!IsEnabled())
            {
                return _waveRam.GetByte(address);
            }

            if (_waveRam.Accepts(_lastReadAddress) && (Gbc || _ticksSinceRead < 2))
            {
                return _waveRam.GetByte(_lastReadAddress);
            }

            return 0xff;
        }


        public override void SetByte(int address, int value)
        {
            if (!_waveRam.Accepts(address))
            {
                base.SetByte(address, value);
                return;
            }

            if (!IsEnabled())
            {
                _waveRam.SetByte(address, value);
            }
            else if (_waveRam.Accepts(_lastReadAddress) && (Gbc || _ticksSinceRead < 2))
            {
                _waveRam.SetByte(_lastReadAddress, value);
            }
        }

        protected override void SetNr0(int value)
        {
            base.SetNr0(value);
            DacEnabled = (value & (1 << 7)) != 0;
            ChannelEnabled &= DacEnabled;
        }

        protected override void SetNr1(int value)
        {
            base.SetNr1(value);
            Length.SetLength(256 - value);
        }

        protected override void SetNr4(int value)
        {
            if (!Gbc && (value & (1 << 7)) != 0)
            {
                if (IsEnabled() && _freqDivider == 2)
                {
                    var pos = _i / 2;
                    if (pos < 4)
                    {
                        _waveRam.SetByte(0xFF30, _waveRam.GetByte(0xFF30 + pos));
                    }
                    else
                    {
                        pos &= ~3;
                        for (var j = 0; j < 4; j++)
                        {
                            _waveRam.SetByte(0xFF30 + j, _waveRam.GetByte(0xFF30 + ((pos + j) % 0x10)));
                        }
                    }
                }
            }

            base.SetNr4(value);
        }

        public override void Start()
        {
            _i = 0;
            _buffer = 0;
            if (Gbc)
            {
                Length.Reset();
            }

            Length.Start();
        }

        protected override void Trigger()
        {
            _i = 0;
            _freqDivider = 6;
            _triggered = !Gbc;
            if (Gbc)
            {
                GetWaveEntry();
            }
        }

        public override int TickChannel()
        {
            _ticksSinceRead++;
            if (!UpdateLength())
            {
                return 0;
            }

            if (!DacEnabled)
            {
                return 0;
            }

            if ((GetNr0() & (1 << 7)) == 0)
            {
                return 0;
            }

            _freqDivider--;

            if (_freqDivider == 0)
            {
                ResetFreqDivider();
                if (_triggered)
                {
                    _lastOutput = (_buffer >> 4) & 0x0F;
                    _triggered = false;
                }
                else
                {
                    _lastOutput = GetWaveEntry();
                }

                _i = (_i + 1) % 32;
            }

            return _lastOutput;
        }

        private int GetVolume() => (GetNr2() >> 5) & 0b11;

        private int GetWaveEntry()
        {
            _ticksSinceRead = 0;
            _lastReadAddress = 0xFF30 + _i / 2;
            _buffer = _waveRam.GetByte(_lastReadAddress);

            var b = _buffer;
            if (_i % 2 == 0)
            {
                b = (b >> 4) & 0x0F;
            }
            else
            {
                b &= 0x0F;
            }

            return GetVolume() switch
            {
                0 => 0,
                1 => b,
                2 => b >> 1,
                3 => b >> 2,
                _ => throw new InvalidOperationException("Illegal state")
            };
        }

        private void ResetFreqDivider() => _freqDivider = GetFrequency() * 2;
    }
}
