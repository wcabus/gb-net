﻿using GB.Core.Memory;

namespace GB.Core.Graphics.Phase
{
    internal sealed class OamSearch : IGpuPhase
    {
        private enum State
        {
            ReadingY,
            ReadingX
        }

        public sealed record SpritePosition(int X, int Y, int Address);

        private readonly IAddressSpace _oemRam;
        private readonly MemoryRegisters _registers;
        private readonly SpritePosition?[] _sprites;
        private readonly Lcdc _lcdc;
        private int _spritePosIndex;
        private State _state;
        private int _spriteY;
        private int _spriteX;
        private int _i;

        public OamSearch(IAddressSpace oemRam, Lcdc lcdc, MemoryRegisters registers)
        {
            _oemRam = oemRam;
            _registers = registers;
            _lcdc = lcdc;
            _sprites = new SpritePosition[10];
        }

        public OamSearch Start()
        {
            _spritePosIndex = 0;
            _state = State.ReadingY;
            _spriteY = 0;
            _spriteX = 0;
            _i = 0;
            for (var j = 0; j < _sprites.Length; j++)
            {
                _sprites[j] = null;
            }

            return this;
        }

        public bool Tick()
        {
            var spriteAddress = 0xFE00 + 4 * _i;
            switch (_state)
            {
                case State.ReadingY:
                    _spriteY = _oemRam.GetByte(spriteAddress);
                    _state = State.ReadingX;
                    break;

                case State.ReadingX:
                    _spriteX = _oemRam.GetByte(spriteAddress + 1);
                    if (_spritePosIndex < _sprites.Length && Between(_spriteY, _registers.Get(GpuRegister.Ly) + 16,
                            _spriteY + _lcdc.GetSpriteHeight()))
                    {
                        _sprites[_spritePosIndex++] = new SpritePosition(_spriteX, _spriteY, spriteAddress);
                    }

                    _i++;
                    _state = State.ReadingY;
                    break;
            }

            return _i < 40;
        }

        public SpritePosition?[] GetSprites()
        {
            return _sprites;
        }

        private static bool Between(int from, int x, int to)
        {
            return from <= x && x < to;
        }
    }
}
