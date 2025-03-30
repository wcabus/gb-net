namespace GB.Core.Serial
{
    public sealed class NullSerialEndpoint : ISerialEndpoint
    {
        public bool ExternalClockPulsed() => false;
        public int Transfer(int outgoing)
        {
            return (outgoing << 1) & 0xFF;
        }
    }
}
