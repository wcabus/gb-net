using System.Runtime.CompilerServices;

namespace GB.Core.Cpu
{
    internal sealed class SpeedMode : IAddressSpace
    {
        private bool _currentSpeed = false;
        private int _speed = 1;
        private bool _prepareTransition;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Accepts(int address) => address == 0xFF4D;
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetByte(int address, int value) => _prepareTransition = (value & 0x01) != 0;
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int GetByte(int address) => (_currentSpeed ? (1 << 7) : 0) | (_prepareTransition ? (1 << 0) : 0) | 0b01111110;

        public bool OnCpuStopped()
        {
            if (!_prepareTransition)
            {
                return false;
            }

            // Toggle the speed mode
            _currentSpeed = !_currentSpeed;
            _speed = _currentSpeed ? 2 : 1;
            _prepareTransition = false;
            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int GetSpeedMode() => _speed;
    }
}
