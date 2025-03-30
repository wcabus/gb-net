using System.Text;

namespace GB.Core.Cpu
{
    internal sealed class Flags
    {
        public const int ZeroPosition = 7;
        public const int NegativePosition = 6;
        public const int HalfCarryPosition = 5;
        public const int CarryPosition = 4;

        public int Byte { get; private set; }

        public bool IsZ() => Byte.GetBit(ZeroPosition);
        public bool IsN() => Byte.GetBit(NegativePosition);
        public bool IsH() => Byte.GetBit(HalfCarryPosition);
        public bool IsC() => Byte.GetBit(CarryPosition);
        public void SetZ(bool z) => Byte = Byte.SetBit(ZeroPosition, z);
        public void SetN(bool n) => Byte = Byte.SetBit(NegativePosition, n);
        public void SetH(bool h) => Byte = Byte.SetBit(HalfCarryPosition, h);
        public void SetC(bool c) => Byte = Byte.SetBit(CarryPosition, c);
        public void SetFlagsByte(int flags) => Byte = flags & 0xf0;

        public override string ToString()
        {
            var result = new StringBuilder();
            result.Append(IsZ() ? 'Z' : '-');
            result.Append(IsN() ? 'N' : '-');
            result.Append(IsH() ? 'H' : '-');
            result.Append(IsC() ? 'C' : '-');
            result.Append("----");
            return result.ToString();
        }
    }
}
