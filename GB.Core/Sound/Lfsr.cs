namespace GB.Core.Sound
{
    internal sealed class Lfsr
    {
        public int Value { get; private set; }

        public Lfsr() => Reset();
        public void Start() => Reset();
        public void Reset() => Value = 0x7FFF;

        public int NextBit(bool widthMode7)
        {
            var xor = ((Value & 1) ^ ((Value & 2) >> 1));
            Value >>= 1;
            Value |= xor << 14;

            if (widthMode7)
            {
                Value |= (xor << 6);
                Value &= 0x7F;
            }

            return 1 & ~Value;
        }
    }
}
