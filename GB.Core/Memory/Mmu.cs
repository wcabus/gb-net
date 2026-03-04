using GB.Core.Controller;
using GB.Core.Cpu;
using GB.Core.Graphics;
using System.Runtime.CompilerServices;

namespace GB.Core.Memory
{
    internal sealed class Mmu : IAddressSpace
    {
        private static readonly IAddressSpace Void = new VoidAddressSpace();

        // Page table: 256 entries, one per 256-byte page (address >> 8)
        private readonly IAddressSpace[] _pageTable = new IAddressSpace[256];
        // Fine-grained table for IO registers 0xFF00-0xFF7F (128 entries)
        private readonly IAddressSpace[] _ioTable = new IAddressSpace[128];

        private IAddressSpace? _cartridge;
        private IAddressSpace? _gpu;
        private IAddressSpace? _interruptManager;
        private IAddressSpace? _highRam;

        public Mmu()
        {
            // Initialize all pages to Void
            Array.Fill(_pageTable, Void);
            Array.Fill(_ioTable, Void);
        }

        public void AddCartridge(Cartridge.Cartridge cartridge)
        {
            _cartridge = cartridge;
            // ROM: 0x0000-0x7FFF (pages 0x00-0x7F)
            for (int i = 0x00; i <= 0x7F; i++) _pageTable[i] = cartridge;
            // External RAM: 0xA000-0xBFFF (pages 0xA0-0xBF)
            for (int i = 0xA0; i <= 0xBF; i++) _pageTable[i] = cartridge;
            // 0xFF50 boot ROM register - handled in IO table
            _ioTable[0x50] = cartridge;
        }

        public void AddGpu(Gpu gpu)
        {
            _gpu = gpu;
            // VRAM: 0x8000-0x9FFF (pages 0x80-0x9F)
            for (int i = 0x80; i <= 0x9F; i++) _pageTable[i] = gpu;
            // OAM: 0xFE00-0xFE9F (page 0xFE)
            _pageTable[0xFE] = gpu;
            // GPU registers in IO range
            _ioTable[0x40] = gpu; // LCDC
            _ioTable[0x41] = gpu; // STAT
            _ioTable[0x42] = gpu; // SCY
            _ioTable[0x43] = gpu; // SCX
            _ioTable[0x44] = gpu; // LY
            _ioTable[0x45] = gpu; // LYC
            _ioTable[0x47] = gpu; // BGP
            _ioTable[0x48] = gpu; // OBP0
            _ioTable[0x49] = gpu; // OBP1
            _ioTable[0x4A] = gpu; // WY
            _ioTable[0x4B] = gpu; // WX
            _ioTable[0x4F] = gpu; // VBK (GBC VRAM bank select)
            // GBC color palette registers
            _ioTable[0x68] = gpu;
            _ioTable[0x69] = gpu;
            _ioTable[0x6A] = gpu;
            _ioTable[0x6B] = gpu;
        }

        public void AddJoypad(Joypad joypad)
        {
            _ioTable[0x00] = joypad;
        }

        public void AddInterruptManager(InterruptManager interruptManager)
        {
            _interruptManager = interruptManager;
            _ioTable[0x0F] = interruptManager; // IF
            _pageTable[0xFF] = interruptManager; // IE at 0xFFFF - will be overridden for high RAM below
        }

        public void AddSerialPort(Serial.SerialPort serialPort)
        {
            _ioTable[0x01] = serialPort;
            _ioTable[0x02] = serialPort;
        }

        public void AddTimer(Timer timer)
        {
            _ioTable[0x04] = timer;
            _ioTable[0x05] = timer;
            _ioTable[0x06] = timer;
            _ioTable[0x07] = timer;
        }

        public void AddDma(Dma dma)
        {
            _ioTable[0x46] = dma;
        }

        public void AddSound(Sound.Sound sound)
        {
            for (int i = 0x10; i <= 0x14; i++) _ioTable[i] = sound;
            for (int i = 0x16; i <= 0x19; i++) _ioTable[i] = sound;
            for (int i = 0x1A; i <= 0x1E; i++) _ioTable[i] = sound;
            for (int i = 0x20; i <= 0x26; i++) _ioTable[i] = sound;
            for (int i = 0x30; i <= 0x3F; i++) _ioTable[i] = sound;
        }

        public void AddFirstRamBank(Ram ram)
        {
            // Work RAM bank 0: 0xC000-0xCFFF (pages 0xC0-0xCF)
            for (int i = 0xC0; i <= 0xCF; i++) _pageTable[i] = ram;
        }

        public void AddSecondRamBank(Ram ram)
        {
            // Work RAM bank 1: 0xD000-0xDFFF (pages 0xD0-0xDF)
            for (int i = 0xD0; i <= 0xDF; i++) _pageTable[i] = ram;
        }

        public void AddSecondRamBank(GameboyColorRam ram)
        {
            for (int i = 0xD0; i <= 0xDF; i++) _pageTable[i] = ram;
            _ioTable[0x70] = ram; // SVBK (GBC WRAM bank select)
        }

        public void AddSpeedMode(SpeedMode speedMode)
        {
            _ioTable[0x4D] = speedMode;
        }

        public void AddHdma(Hdma hdma)
        {
            for (int i = 0x51; i <= 0x55; i++) _ioTable[i] = hdma;
        }

        public void AddGbcRegisters(UndocumentedGbcRegisters gbcRegisters)
        {
            _ioTable[0x6C] = gbcRegisters;
            _ioTable[0x72] = gbcRegisters;
            _ioTable[0x73] = gbcRegisters;
            _ioTable[0x74] = gbcRegisters;
            _ioTable[0x75] = gbcRegisters;
            _ioTable[0x76] = gbcRegisters;
            _ioTable[0x77] = gbcRegisters;
        }

        public void AddHighRam(Ram highRam)
        {
            _highRam = highRam;
        }

        public void AddShadowRam(ShadowAddressSpace shadowRam)
        {
            // Echo RAM: 0xE000-0xFDFF (pages 0xE0-0xFD)
            for (int i = 0xE0; i <= 0xFD; i++) _pageTable[i] = shadowRam;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Accepts(int address) => true;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetByte(int address, int value) => GetSpace(address).SetByte(address, value);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int GetByte(int address) => GetSpace(address).GetByte(address);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private IAddressSpace GetSpace(int address)
        {
            var page = (address >> 8) & 0xFF;

            // Fast path: most addresses are not in the 0xFF page
            if (page != 0xFF)
            {
                return _pageTable[page];
            }

            // 0xFF page: IO registers (0xFF00-0xFF7F), High RAM (0xFF80-0xFFFE), IE (0xFFFF)
            var low = address & 0xFF;
            if (low < 0x80)
            {
                return _ioTable[low];
            }
            if (low == 0xFF)
            {
                return _interruptManager ?? Void;
            }
            return _highRam ?? Void;
        }
    }
}
