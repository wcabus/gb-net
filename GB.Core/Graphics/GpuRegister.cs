using GB.Core.Memory;

namespace GB.Core.Graphics
{
    internal sealed class GpuRegister : IRegister
    {
        public static GpuRegister Stat = new GpuRegister(0xFF41, RegisterType.RW);
        public static GpuRegister Scy = new GpuRegister(0xFF42, RegisterType.RW);
        public static GpuRegister Scx = new GpuRegister(0xFF43, RegisterType.RW);
        public static GpuRegister Ly = new GpuRegister(0xFF44, RegisterType.R);
        public static GpuRegister Lyc = new GpuRegister(0xFF45, RegisterType.RW);
        public static GpuRegister Bgp = new GpuRegister(0xFF47, RegisterType.RW);
        public static GpuRegister Obp0 = new GpuRegister(0xFF48, RegisterType.RW);
        public static GpuRegister Obp1 = new GpuRegister(0xFF49, RegisterType.RW);
        public static GpuRegister Wy = new GpuRegister(0xFF4A, RegisterType.RW);
        public static GpuRegister Wx = new GpuRegister(0xFF4B, RegisterType.RW);
        public static GpuRegister Vbk = new GpuRegister(0xFF4F, RegisterType.W);

        public int Address { get; }
        public RegisterType Type { get; }

        public GpuRegister(int address, RegisterType type)
        {
            Address = address;
            Type = type;
        }

        public static IEnumerable<IRegister> Values()
        {
            yield return Stat;
            yield return Scy;
            yield return Scx;
            yield return Ly;
            yield return Lyc;
            yield return Bgp;
            yield return Obp0;
            yield return Obp1;
            yield return Wy;
            yield return Wx;
            yield return Vbk;
        }
    }
}
