using GB.Core.Controller;
using GB.Core.Cpu;
using GB.Core.Graphics;
using System.Runtime.CompilerServices;

namespace GB.Core.Memory
{
    internal sealed class Mmu : IAddressSpace
    {
        private static readonly IAddressSpace Void = new VoidAddressSpace();

        private IAddressSpace? _cartridge;
        private IAddressSpace? _gpu;
        private IAddressSpace? _ramBank0;
        private IAddressSpace? _ramBank1;
        private IAddressSpace? _joypad;
        private IAddressSpace? _interruptManager;
        private IAddressSpace? _serialPort;
        private IAddressSpace? _timer;
        private IAddressSpace? _dma;
        private IAddressSpace? _sound;
        private IAddressSpace? _speedMode;
        private IAddressSpace? _hdma;
        private IAddressSpace? _gbcRegisters;
        private IAddressSpace? _highRam;
        private IAddressSpace? _shadowRam;

        public void AddCartridge(Cartridge.Cartridge cartridge)
        {
            _cartridge = cartridge;
        }

        public void AddGpu(Gpu gpu)
        {
            _gpu = gpu;
        }

        public void AddJoypad(Joypad joypad)
        {
            _joypad = joypad;
        }

        public void AddInterruptManager(InterruptManager interruptManager)
        {
            _interruptManager = interruptManager;
        }

        public void AddSerialPort(Serial.SerialPort serialPort)
        {
            _serialPort = serialPort;
        }

        public void AddTimer(Timer timer)
        {
            _timer = timer;
        }

        public void AddDma(Dma dma)
        {
            _dma = dma;
        }

        public void AddSound(Sound.Sound sound)
        {
            _sound = sound;
        }

        public void AddFirstRamBank(Ram ram)
        {
            _ramBank0 = ram;
        }

        public void AddSecondRamBank(Ram ram)
        {
            _ramBank1 = ram;
        }

        public void AddSecondRamBank(GameboyColorRam ram)
        {
            _ramBank1 = ram;
        }

        public void AddSpeedMode(SpeedMode speedMode)
        {
            _speedMode = speedMode;
        }

        public void AddHdma(Hdma hdma)
        {
            _hdma = hdma;
        }

        public void AddGbcRegisters(UndocumentedGbcRegisters gbcRegisters)
        {
            _gbcRegisters = gbcRegisters;
        }

        public void AddHighRam(Ram highRam)
        {
            _highRam = highRam;
        }

        public void AddShadowRam(ShadowAddressSpace shadowRam)
        {
            _shadowRam = shadowRam;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Accepts(int address) => true;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetByte(int address, int value) => GetSpace(address).SetByte(address, value);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int GetByte(int address) => GetSpace(address).GetByte(address);

        private IAddressSpace GetSpace(int address)
        {
            switch (address)
            {
                case int c when (0x000 <= c && c <= 0x7FFF) || (0xA000 <= c && c <= 0xBFFF) || c == 0xFF50:
                    return _cartridge!;
                case int v when 0x8000 <= v && v <= 0x9FFF:
                    return _gpu!;
                case int r when 0xC000 <= r && r <= 0xCFFF:
                    return _ramBank0!;
                case int r when 0xD000 <= r && r <= 0xDFFF:
                    return _ramBank1!;
                case int s when 0xE000 <= s && s <= 0xFDFF:
                    return _shadowRam!;
                case int v when 0xFE00 <= v && v <= 0xFE9F:
                    return _gpu!; // OAM RAM
                case 0xFF00:
                    return _joypad!;
                case 0xFF01:
                case 0xFF02:
                    return _serialPort!;
                case 0xFF04:
                case 0xFF05:
                case 0xFF06:
                case 0xFF07:
                    return _timer!;
                case 0xFF0F: // IF = interrupt flag
                    return _interruptManager!;
                case int s1 when 0xFF10 <= s1 && s1 <= 0xFF14:
                case int s2 when 0xFF16 <= s2 && s2 <= 0xFF19:
                case int s3 when 0xFF1A <= s3 && s3 <= 0xFF1E:
                case int s4 when 0xFF20 <= s4 && s4 <= 0xFF26:
                case int s5 when 0xFF30 <= s5 && s5 <= 0xFF3F:
                    return _sound!;
                case 0xFF40: // LCD Control
                case 0xFF41: // LCD Status
                case 0xFF42: // Scroll Y
                case 0xFF43: // Scroll X
                case 0xFF44: // LCD Y coord
                case 0xFF45: // LCD Y compare
                    return _gpu!;
                case 0xFF46:
                    return _dma!;
                case 0xFF47: // BG Palette
                case 0xFF48: // OBJ Palette 0
                case 0xFF49: // OBJ Palette 1
                case 0xFF4A: // Window Y pos
                case 0xFF4B: // Window X pos
                    return _gpu!;
                case 0xFF4D:
                    return _speedMode ?? Void;
                case int h when 0xFF51 <= h && h <= 0xFF55:
                    return _hdma ?? Void;
                case 0xFF68: // Background color palette spec
                case 0xFF69: // Background color palette data
                case 0xFF6A: // Object color palette spec
                case 0xFF6B: // Object color palette data
                    return _gpu!;
                case 0xFF6C:
                case 0xFF72:
                case 0xFF73:
                case 0xFF74:
                case 0xFF75:
                case 0xFF76:
                case 0xFF77:
                    return _gbcRegisters ?? Void;
                case int h when 0xFF80 <= h && h <= 0xFFFE:
                    return _highRam!;
                case 0xFFFF: // IE = interrupt enable
                    return _interruptManager!;
            }
            
            return Void;
        }
    }
}
