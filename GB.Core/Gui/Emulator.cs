using GB.Core.Controller;
using GB.Core.Graphics;
using GB.Core.Memory.Cartridge;
using GB.Core.Serial;
using GB.Core.Sound;

namespace GB.Core.Gui
{
    public sealed class Emulator : IRunnable
    {
        private Cartridge? _cartridge;

        public bool EnableBootRom { get; set; } = true;
        public GameBoyMode GameBoyMode { get; set; } = GameBoyMode.AutoDetect;

        public string? RomPath { get; set; }
        public Stream? RomStream { get; set; }

        public Gameboy? Gameboy { get; set; }
        
        public IDisplay Display { get; set; } = new NullDisplay();
        public ISoundOutput SoundOutput { get; set; } = new NullSoundOutput();
        public IController Controller { get; set; } = new NullController();
        //public SerialEndpoint SerialEndpoint { get; set; } = new NullSerialEndpoint();
        
        //public GameboyOptions Options { get; set; }
        public bool Active { get; set; }

        public Emulator(/*GameboyOptions options*/)
        {
            // Options = options;
        }

        public void Run(CancellationToken token)
        {
            //if (!Options.RomSpecified || !Options.RomFile.Exists)
            //{
            //    throw new ArgumentException("The ROM path doesn't exist: " + Options.RomFile);
            //}
            if (string.IsNullOrEmpty(RomPath) && RomStream is null)
            {
                throw new ArgumentException("Please choose a ROM.");
            }

            if (!string.IsNullOrEmpty(RomPath))
            {
                _cartridge = Cartridge.FromFile(RomPath);
            }
            else if (RomStream != null)
            {
                _cartridge = Cartridge.FromStream(RomStream);
            }

            if (_cartridge is null)
            {
                throw new ArgumentException("The ROM path doesn't exist or points to an invalid ROM file: " + RomPath);
            }

            Gameboy = CreateGameboy(_cartridge);
            new Thread(() => Display.Run(token)).Start();
            new Thread(() => Gameboy.Run(token)).Start();

            Active = true;
        }

        public void Stop(CancellationTokenSource source)
        {
            if (!Active)
            {
                return;
            }

            source.Cancel();
            Active = false;
            
            _cartridge?.SaveRam();
            _cartridge?.Dispose();

            RomPath = null;

            RomStream?.Dispose();
            RomStream = null;
        }

        public void ToggleSoundChannel(int channel)
        {
            Gameboy?.ToggleSoundChannel(channel);
        }

        public void TogglePause()
        {
            if (Gameboy != null)
            {
                Gameboy.Paused = !Gameboy.Paused;
            }
        }

        private Gameboy CreateGameboy(Cartridge rom)
        {
            return new Gameboy(rom, Display, Controller, SoundOutput, new NullSerialEndpoint(), EnableBootRom, GameBoyMode);
        }
    }
}
