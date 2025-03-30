namespace GB.Core.Graphics
{
    public sealed class NullDisplay : IDisplay
    {
        public bool Enabled { get; set; }

        public void PutDmgPixel(int color)
        {
        }

        public void PutColorPixel(int gbcRgb)
        {
        }

        public void RequestRefresh()
        {
        }

        public void WaitForRefresh()
        {
        }

        public void Run(CancellationToken token)
        {
        }
    }
}
