using GB.Core.Memory;

namespace GB.Core.Sound
{
    internal sealed class Sound : IAddressSpace
    {
        private static readonly int[] Masks = {
            0x80, 0x3F, 0x00, 0xFF, 0xBF,
            0xFF, 0x3F, 0x00, 0xFF, 0xBF,
            0x7F, 0xFF, 0x9F, 0xFF, 0xBF,
            0xFF, 0xFF, 0x00, 0x00, 0xBF,
            0x00, 0x00, 0x70,
            0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF,
            0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
            0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00
        };

        private readonly SoundModeBase[] _allModes = new SoundModeBase[4];
        private readonly int[] _channels = new int[4];
        private readonly Ram _ram = new(0xFF24, 0x03);
        private readonly ISoundOutput _output;
        private readonly bool[] _overridenEnabled = { true, true, true, true };
        private bool _enabled;

        public Sound(ISoundOutput output, bool gbc)
        {
            _allModes[0] = new SoundMode1(gbc);
            _allModes[1] = new SoundMode2(gbc);
            _allModes[2] = new SoundMode3(gbc);
            _allModes[3] = new SoundMode4(gbc);
            _output = output;
        }

        public void Tick()
        {
            if (!_enabled)
            {
                return;
            }

            for (var i = 0; i < _allModes.Length; i++)
            {
                var abstractSoundMode = _allModes[i];
                var channel = abstractSoundMode.TickChannel();
                _channels[i] = channel;
            }

            var selection = _ram.GetByte(0xFF25);
            var left = 0;
            var right = 0;
            for (var i = 0; i < 4; i++)
            {
                if (!_overridenEnabled[i])
                {
                    continue;
                }

                if ((selection & (1 << i + 4)) != 0)
                {
                    left += _channels[i];
                }

                if ((selection & (1 << i)) != 0)
                {
                    right += _channels[i];
                }
            }

            var volumes = _ram.GetByte(0xFF24);
            left *= (volumes >> 4) & 0b111;
            right *= volumes & 0b111;

            _output.Play(left, right);
        }

        private IAddressSpace? GetAddressSpace(int address)
        {
            foreach (var m in _allModes)
            {
                if (m.Accepts(address))
                {
                    return m;
                }
            }

            if (_ram.Accepts(address))
            {
                return _ram;
            }

            return null;
        }

        public bool Accepts(int address) => GetAddressSpace(address) != null;

        public void SetByte(int address, int value)
        {
            if (address == 0xFF26)
            {
                if ((value & (1 << 7)) == 0)
                {
                    if (_enabled)
                    {
                        _enabled = false;
                        Stop();
                    }
                }
                else
                {
                    if (!_enabled)
                    {
                        _enabled = true;
                        Start();
                    }
                }

                return;
            }

            var s = GetAddressSpace(address);
            if (s == null)
            {
                throw new ArgumentException();
            }

            s.SetByte(address, value);
        }


        public int GetByte(int address)
        {
            int result;
            if (address == 0xFF26)
            {
                result = 0;
                for (var i = 0; i < _allModes.Length; i++)
                {
                    result |= _allModes[i].IsEnabled() ? (1 << i) : 0;
                }

                result |= _enabled ? (1 << 7) : 0;
            }
            else
            {
                result = GetUnmaskedByte(address);
            }

            return result | Masks[address - 0xFF10];
        }

        private int GetUnmaskedByte(int address)
        {
            var s = GetAddressSpace(address);
            if (s == null)
            {
                throw new ArgumentException();
            }

            return s.GetByte(address);
        }

        private void Start()
        {
            for (var i = 0xFF10; i <= 0xFF25; i++)
            {
                var v = 0;
                // lengths should be preserved
                if (i == 0xFF11 || i == 0xFF16 || i == 0xFF20)
                {
                    // channel 1, 2, 4 lengths
                    v = GetUnmaskedByte(i) & 0b00111111;
                }
                else if (i == 0xFF1B)
                {
                    // channel 3 length
                    v = GetUnmaskedByte(i);
                }

                SetByte(i, v);
            }

            foreach (var m in _allModes)
            {
                m.Start();
            }

            _output.Start();
        }

        private void Stop()
        {
            _output.Stop();
            foreach (var s in _allModes)
            {
                s.Stop();
            }
        }

        public void ToggleChannel(int i) => _overridenEnabled[i] = !_overridenEnabled[i];
    }
}
