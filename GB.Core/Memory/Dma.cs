using GB.Core.Cpu;
using System.Runtime.CompilerServices;

namespace GB.Core.Memory
{
    internal sealed class Dma : IAddressSpace
    {
        private readonly IAddressSpace _addressSpace;
        private readonly IAddressSpace _oam;
        private readonly SpeedMode _speedMode;

        private bool _transferInProgress;
        private bool _restarted;
        private int _from;
        private int _ticks;
        private int _regValue = 0xFF;

        public Dma(IAddressSpace addressSpace, IAddressSpace oam, SpeedMode speedMode)
        {
            _addressSpace = new DmaAddressSpace(addressSpace);
            _speedMode = speedMode;
            _oam = oam;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Accepts(int address)
        {
            return address == 0xFF46;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Tick()
        {
            if (!_transferInProgress) return;
            if (++_ticks < 648 / _speedMode.GetSpeedMode()) return;

            _transferInProgress = false;
            _restarted = false;
            _ticks = 0;

            for (var i = 0; i < 0xA0; i++)
            {
                _oam.SetByte(0xFE00 + i, _addressSpace.GetByte(_from + i));
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetByte(int address, int value)
        {
            _from = value * 0x100;
            _restarted = IsOamBlocked();
            _ticks = 0;
            _transferInProgress = true;
            _regValue = value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int GetByte(int address) => _regValue;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsOamBlocked() => _restarted || _transferInProgress && _ticks >= 5;
    }
}
