namespace GB.Core.Memory.Cartridge.RTC
{
    internal sealed class SystemClock : IClock
    {
        public long CurrentTimeMillis() => DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
    }
}
