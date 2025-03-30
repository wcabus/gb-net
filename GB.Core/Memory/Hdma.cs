using GB.Core.Graphics;
using System.Runtime.CompilerServices;

namespace GB.Core.Memory
{
    internal sealed class Hdma : IAddressSpace
    {
        private const int Hdma1 = 0xFF51;
        private const int Hdma2 = 0xFF52;
        private const int Hdma3 = 0xFF53;
        private const int Hdma4 = 0xFF54;
        private const int Hdma5 = 0xFF55;

        private readonly IAddressSpace _addressSpace;
        private readonly Ram _hdma1234 = new Ram(Hdma1, 4);
        private Gpu.Mode? _gpuMode;

        private bool _transferInProgress;
        private bool _hblankTransfer;
        private bool _lcdEnabled;

        private int _length;
        private int _src;
        private int _dst;
        private int _tick;

        public Hdma(IAddressSpace addressSpace)
        {
            _addressSpace = addressSpace;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Accepts(int address) => address is >= Hdma1 and <= Hdma5;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Tick()
        {
            if (!IsTransferInProgress())
            {
                return;
            }

            if (++_tick < 0x20)
            {
                return;
            }

            for (var j = 0; j < 0x10; j++)
            {
                _addressSpace.SetByte(_dst + j, _addressSpace.GetByte(_src + j));
            }

            _src += 0x10;
            _dst += 0x10;
            if (_length-- == 0)
            {
                _transferInProgress = false;
                _length = 0x7F;
            }
            else if (_hblankTransfer)
            {
                _gpuMode = null; // wait until next HBlank
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetByte(int address, int value)
        {
            if (_hdma1234.Accepts(address))
            {
                _hdma1234.SetByte(address, value);
            }
            else if (address == Hdma5)
            {
                if (_transferInProgress)
                {
                    StopTransfer();
                }
                else
                {
                    StartTransfer(value);
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int GetByte(int address)
        {
            if (_hdma1234.Accepts(address))
            {
                return 0xff;
            }

            if (address == Hdma5)
            {
                return (_transferInProgress ? 0 : (1 << 7)) | _length;
            }

            throw new ArgumentException();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void OnGpuUpdate(Gpu.Mode newGpuMode) => _gpuMode = newGpuMode;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void OnLcdSwitch(bool lcdEnabled) => _lcdEnabled = lcdEnabled;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsTransferInProgress()
        {
            if (!_transferInProgress)
            {
                return false;
            }

            if (_hblankTransfer && (_gpuMode == Gpu.Mode.HBlank || !_lcdEnabled))
            {
                return true;
            }

            return !_hblankTransfer;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void StartTransfer(int reg)
        {
            _hblankTransfer = (reg & (1 << 7)) != 0;
            _length = reg & 0x7F;

            _src = (_hdma1234.GetByte(Hdma1) << 8) | (_hdma1234.GetByte(Hdma2) & 0xF0);
            _dst = ((_hdma1234.GetByte(Hdma3) & 0x1F) << 8) | (_hdma1234.GetByte(Hdma4) & 0xF0);
            _src &= 0xFFF0;
            _dst = (_dst & 0x1FFF) | 0x8000;

            _transferInProgress = true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void StopTransfer() => _transferInProgress = false;
    }
}
