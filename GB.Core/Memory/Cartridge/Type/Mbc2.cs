using GB.Core.Memory.Cartridge.Battery;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GB.Core.Memory.Cartridge.Type
{
    internal sealed class Mbc2 : IAddressSpace
    {
        private readonly int[] _cartridge;
        private readonly int[] _ram;
        private readonly IBattery _battery;
        private int _selectedRomBank = 1;
        private bool _ramWriteEnabled;

        public Mbc2(int[] cartridge, CartridgeType type, IBattery battery, int romBanks)
        {
            _cartridge = cartridge;
            _ram = new int[0x0200];
            for (var i = 0; i < _ram.Length; i++)
            {
                _ram[i] = 0xFF;
            }

            _battery = battery;
            battery.LoadRam(_ram);
        }

        public void SaveRam()
        {
            _battery.SaveRam(_ram);
        }

        public bool Accepts(int address) => address >= 0x0000 && address < 0x8000 || address >= 0xA000 && address < 0xC000;

        public void SetByte(int address, int value)
        {
            if (address >= 0x0000 && address < 0x2000)
            {
                if ((address & 0x0100) == 0)
                {
                    _ramWriteEnabled = (value & 0b1010) != 0;
                    if (!_ramWriteEnabled)
                    {
                        SaveRam();
                    }
                }
            }
            else if (address >= 0x2000 && address < 0x4000)
            {
                if ((address & 0x0100) != 0)
                {
                    _selectedRomBank = value & 0b00001111;
                }
            }
            else if (address >= 0xA000 && address < 0xC000 && _ramWriteEnabled)
            {
                var ramAddress = GetRamAddress(address);
                if (ramAddress < _ram.Length)
                {
                    _ram[ramAddress] = value & 0x0F;
                }
            }
        }

        public int GetByte(int address)
        {
            if (address >= 0x0000 && address < 0x4000)
            {
                return GetRomByte(0, address);
            }

            if (address >= 0x4000 && address < 0x8000)
            {
                return GetRomByte(_selectedRomBank, address - 0x4000);
            }

            if (address >= 0xA000 && address < 0xB000)
            {
                var ramAddress = GetRamAddress(address);
                if (ramAddress < _ram.Length)
                {
                    return _ram[ramAddress];
                }

                return 0xFF;
            }

            return 0xFF;
        }

        private int GetRomByte(int bank, int address)
        {
            var cartOffset = bank * 0x4000 + address;
            if (cartOffset < _cartridge.Length)
            {
                return _cartridge[cartOffset];
            }

            return 0xFF;
        }

        private static int GetRamAddress(int address) => address - 0xA000;
    }
}
