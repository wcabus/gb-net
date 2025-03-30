using System.Runtime.CompilerServices;

namespace GB.Core.Graphics
{
    internal sealed class Lcdc : IAddressSpace
    {
        private int _value = 0x91;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsBgAndWindowDisplay() => (_value & 0x01) != 0;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsObjDisplay() => (_value & 0x02) != 0;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int GetSpriteHeight() => (_value & 0x04) == 0 ? 8 : 16;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int GetBgTileMapDisplay() => (_value & 0x08) == 0 ? 0x9800 : 0x9C00;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int GetBgWindowTileData() => (_value & 0x10) == 0 ? 0x9000 : 0x8000;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsBgWindowTileDataSigned() => (_value & 0x10) == 0;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsWindowDisplay() => (_value & 0x20) != 0;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int GetWindowTileMapDisplay() => (_value & 0x40) == 0 ? 0x9800 : 0x9C00;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsLcdEnabled() => (_value & 0x80) != 0;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Accepts(int address) => address == 0xFF40;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetByte(int address, int val) => _value = val;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int GetByte(int address) => _value;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Set(int val) => _value = val;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int Get() => _value;
    }
}
