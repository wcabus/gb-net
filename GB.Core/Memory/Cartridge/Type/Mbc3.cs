﻿using GB.Core.Memory.Cartridge.Battery;
using GB.Core.Memory.Cartridge.RTC;

namespace GB.Core.Memory.Cartridge.Type
{
    internal sealed class Mbc3 : IAddressSpace
    {
        private readonly int[] _cartridge;
        private readonly int[] _ram;
        private readonly RealTimeClock _clock;
        private readonly IBattery _battery;
        private int _selectedRamBank;
        private int _selectedRomBank = 1;
        private bool _ramWriteEnabled;
        private int _latchClockReg = 0xff;
        private bool _clockLatched;

        public Mbc3(int[] cartridge, CartridgeType type, IBattery battery, int romBanks, int ramBanks)
        {
            _cartridge = cartridge;
            _ram = new int[0x2000 * Math.Max(ramBanks, 1)];
            for (var i = 0; i < _ram.Length; i++)
            {
                _ram[i] = 0xff;
            }

            _clock = new RealTimeClock(Clock.SystemClock);
            _battery = battery;

            var clockData = new long[12];
            battery.LoadRamWithClock(_ram, clockData);
            _clock.Deserialize(clockData);
        }

        public void SaveRam()
        {
            _battery.SaveRamWithClock(_ram, _clock.Serialize());
        }

        public bool Accepts(int address)
        {
            return (address >= 0x0000 && address < 0x8000) ||
                   (address >= 0xA000 && address < 0xC000);
        }

        public void SetByte(int address, int value)
        {
            if (address >= 0x0000 && address < 0x2000)
            {
                _ramWriteEnabled = (value & 0b1010) != 0;
                if (!_ramWriteEnabled)
                {
                    SaveRam();
                }
            }
            else if (address >= 0x2000 && address < 0x4000)
            {
                var bank = value & 0b01111111;
                SelectRomBank(bank);
            }
            else if (address >= 0x4000 && address < 0x6000)
            {
                _selectedRamBank = value;
            }
            else if (address >= 0x6000 && address < 0x8000)
            {
                if (value == 0x01 && _latchClockReg == 0x00)
                {
                    if (_clockLatched)
                    {
                        _clock.Unlatch();
                        _clockLatched = false;
                    }
                    else
                    {
                        _clock.Latch();
                        _clockLatched = true;
                    }
                }

                _latchClockReg = value;
            }
            else if (address >= 0xA000 && address < 0xC000 && _ramWriteEnabled && _selectedRamBank < 4)
            {
                var ramAddress = GetRamAddress(address);
                if (ramAddress < _ram.Length)
                {
                    _ram[ramAddress] = value;
                }
            }
            else if (address >= 0xA000 && address < 0xC000 && _ramWriteEnabled && _selectedRamBank >= 4)
            {
                SetTimer(value);
            }
        }

        private void SelectRomBank(int bank)
        {
            if (bank == 0)
            {
                bank = 1;
            }

            _selectedRomBank = bank;
        }


        public int GetByte(int address)
        {
            if (address >= 0x0000 && address < 0x4000)
            {
                return GetRomByte(0, address);
            }
            else if (address >= 0x4000 && address < 0x8000)
            {
                return GetRomByte(_selectedRomBank, address - 0x4000);
            }
            else if (address >= 0xA000 && address < 0xC000 && _selectedRamBank < 4)
            {
                var ramAddress = GetRamAddress(address);
                if (ramAddress < _ram.Length)
                {
                    return _ram[ramAddress];
                }
                else
                {
                    return 0xFF;
                }
            }
            else if (address >= 0xA000 && address < 0xC000 && _selectedRamBank >= 4)
            {
                return GetTimer();
            }
            else
            {
                throw new ArgumentException(address.ToString("X"));
            }
        }

        private int GetRomByte(int bank, int address)
        {
            var cartOffset = bank * 0x4000 + address;
            if (cartOffset < _cartridge.Length)
            {
                return _cartridge[cartOffset];
            }
            else
            {
                return 0xFF;
            }
        }

        private int GetRamAddress(int address)
        {
            return _selectedRamBank * 0x2000 + (address - 0xA000);
        }

        private int GetTimer()
        {
            switch (_selectedRamBank)
            {
                case 0x08:
                    return _clock.GetSeconds();

                case 0x09:
                    return _clock.GetMinutes();

                case 0x0A:
                    return _clock.GetHours();

                case 0x0B:
                    return _clock.GetDayCounter() & 0xFF;

                case 0x0C:
                    var result = ((_clock.GetDayCounter() & 0x100) >> 8);
                    result |= _clock.IsHalt() ? (1 << 6) : 0;
                    result |= _clock.IsCounterOverflow() ? (1 << 7) : 0;
                    return result;
            }

            return 0xFF;
        }

        private void SetTimer(int value)
        {
            var dayCounter = _clock.GetDayCounter();
            switch (_selectedRamBank)
            {
                case 0x08:
                    _clock.SetSeconds(value);
                    break;

                case 0x09:
                    _clock.SetMinutes(value);
                    break;

                case 0x0A:
                    _clock.SetHours(value);
                    break;

                case 0x0B:
                    _clock.SetDayCounter((dayCounter & 0x100) | (value & 0xFF));
                    break;

                case 0x0C:
                    _clock.SetDayCounter((dayCounter & 0xFF) | ((value & 1) << 8));
                    _clock.SetHalt((value & (1 << 6)) != 0);
                    if ((value & (1 << 7)) == 0)
                    {
                        _clock.ClearCounterOverflow();
                    }

                    break;
            }
        }
    }
}
