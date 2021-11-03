﻿namespace GB.Core.Graphics
{
    public class NullDisplay : IDisplay
    {
        public bool Enabled { get; set; }

        public event EventHandler OnFrameProduced = (_, _) => { };

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
