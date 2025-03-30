using GB.Core.Controller;
using GB.Core.Cpu;
using GB.Core.Graphics;
using GB.Core.Gui;
using GB.Core.Memory;
using GB.Core.Memory.Cartridge;
using GB.Core.Serial;
using GB.Core.Sound;

namespace GB.Core
{
    public sealed class Gameboy : IRunnable
    {
        public const int TicksPerSec = 4_194_304;

        private readonly Processor _cpu;

        private readonly IDisplay _display;
        private readonly Gpu _gpu;
        private readonly Timer _timer;
        private readonly Dma _dma;
        private readonly Hdma _hdma;
        private readonly Sound.Sound _sound;
        private readonly SerialPort _serialPort;

        public bool Paused { get; set; }

        public Gameboy(Cartridge cartridge, IDisplay display, IController controller, ISoundOutput soundOutput, ISerialEndpoint serialEndpoint, bool enableBootRom = true, GameBoyMode gameBoyMode = GameBoyMode.AutoDetect)
        {
            _display = display;
            var gbc = cartridge.IsGameboyColor;

            switch (gameBoyMode)
            {
                case GameBoyMode.Color:
                    gbc = true;
                    break;
                case GameBoyMode.DMG:
                    // Force into Color mode for cartridges that don't support the DMG, use DMG mode for universal cartridges.
                    gbc = cartridge.GameboyType == GameboyType.GameboyColor;
                    break;
            }

            var speedMode = new SpeedMode();

            var interruptManager = new InterruptManager(gbc);

            _timer = new Timer(interruptManager, speedMode);
            var mmu = new Mmu();

            var oamRam = new Ram(0xFE00, 0x00A0);
            
            _dma = new Dma(mmu, oamRam, speedMode);
            _gpu = new Gpu(_display, interruptManager, _dma, oamRam, gbc);
            _hdma = new Hdma(mmu);
            _sound = new Sound.Sound(soundOutput, gbc);
            _serialPort = new SerialPort(interruptManager, serialEndpoint, speedMode, gbc);

            mmu.AddCartridge(cartridge);
            mmu.AddGpu(_gpu);
            mmu.AddJoypad(new Joypad(interruptManager, controller));
            mmu.AddInterruptManager(interruptManager);
            mmu.AddSerialPort(_serialPort);
            mmu.AddTimer(_timer);
            mmu.AddDma(_dma);
            mmu.AddSound(_sound);

            mmu.AddFirstRamBank(new Ram(0xC000, 0x1000));
            if (gbc)
            {
                mmu.AddSpeedMode(speedMode);
                mmu.AddHdma(_hdma);
                mmu.AddSecondRamBank(new GameboyColorRam());
                mmu.AddGbcRegisters(new UndocumentedGbcRegisters());
            }
            else
            {
                mmu.AddSecondRamBank(new Ram(0xD000, 0x1000));
            }

            mmu.AddHighRam(new Ram(0xFF80, 0x7F));
            mmu.AddShadowRam(new ShadowAddressSpace(mmu, 0xE000, 0xC000, 0x1E00));

            _cpu = new Processor(mmu, interruptManager, _gpu, _display, speedMode);

            interruptManager.DisableInterrupts(false);

            if (enableBootRom)
            {
                return;
            }

            _cpu.InitializeRegisters(gbc);
            cartridge.SetByte(0xFF50, 0xFF);
        }

        public void ToggleSoundChannel(int channel)
        {
            _sound.ToggleChannel(channel - 1);
        }

        public void Run(CancellationToken cancellationToken)
        {
            var requestedScreenRefresh = false;
            var lcdDisabled = false;

            while (!cancellationToken.IsCancellationRequested)
            {
                if (Paused)
                {
                    Thread.Sleep(1000);
                    continue;
                }

                var newMode = Tick();
                if (newMode.HasValue)
                {
                    _hdma.OnGpuUpdate(newMode.Value);
                }

                if (!lcdDisabled && !_gpu.IsLcdEnabled())
                {
                    lcdDisabled = true;
                    _display.RequestRefresh();
                    _hdma.OnLcdSwitch(false);
                }
                else if (newMode == Gpu.Mode.VBlank)
                {
                    requestedScreenRefresh = true;
                    _display.RequestRefresh();
                }

                if (lcdDisabled && _gpu.IsLcdEnabled())
                {
                    lcdDisabled = false;
                    _display.WaitForRefresh();
                    _hdma.OnLcdSwitch(true);
                }
                else if (requestedScreenRefresh && newMode == Gpu.Mode.OamSearch)
                {
                    requestedScreenRefresh = false;
                    _display.WaitForRefresh();
                }
            }
        }

        private Gpu.Mode? Tick()
        {
            if (_hdma.IsTransferInProgress())
            {
                _hdma.Tick();
            }
            else
            {
                _cpu.Tick();
            }

            _timer.Tick();
            _dma.Tick();
            _sound.Tick();
            _serialPort.Tick();
            return _gpu.Tick();
        }
    }
}
