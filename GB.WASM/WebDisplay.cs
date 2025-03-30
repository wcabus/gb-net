using System;
using System.Runtime.CompilerServices;
using System.Threading;
using GB.Core.Graphics;

public sealed class WebDisplay : IDisplay
{
    public static readonly int DisplayWidth = 160;
    public static readonly int DisplayHeight = 144;
    public static readonly float AspectRatio = DisplayWidth / (DisplayHeight * 1f);

    public static readonly int[] Colors = { 0xe6f8da, 0x99c886, 0x437969, 0x051f2a };
    private static readonly byte[] ScreenOffOutput;

    private int[] _rgb = new int[DisplayWidth * DisplayHeight];
    private int[] _rgbPrevious = new int[DisplayWidth * DisplayHeight];
    private readonly byte[] _imageBuffer = new byte[DisplayWidth * DisplayHeight * 4];

    private ManualResetEvent _requestRefresh = new ManualResetEvent(false);
    private ManualResetEvent _refreshDone = new ManualResetEvent(false);
    private int _i;
    private CancellationToken _token = CancellationToken.None;
    private SynchronizationContext _synchronizationContext;

    static WebDisplay()
    {
        var (r, g, b) = ToRgb(Colors[0]);
        var pi = 0;
        var count = DisplayHeight * DisplayWidth;

        ScreenOffOutput = new byte[count * 4];

        while (pi < count)
        {
            ScreenOffOutput[pi++] = (byte)r;
            ScreenOffOutput[pi++] = (byte)g;
            ScreenOffOutput[pi++] = (byte)b;
            ScreenOffOutput[pi++] = 255;
        }
    }

    public void Run(CancellationToken token)
    {
        Interop.SetupBuffer(new ArraySegment<byte>(_imageBuffer), DisplayWidth, DisplayHeight);
        _requestRefresh.Reset();
        _refreshDone.Reset();
        _token = token;
        _synchronizationContext = SynchronizationContext.Current ?? new SynchronizationContext();
        Enabled = true;

        while (true)
        {
            _refreshDone.Reset();
            WaitHandle.WaitAny([token.WaitHandle, _requestRefresh]);

            if (token.IsCancellationRequested)
            {
                break;
            }
            _requestRefresh.Reset();

            FillAndDrawBuffer();
        }
    }

    public bool Enabled { get; set; }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void PutDmgPixel(int color)
    {
        _rgb[_i++] = Colors[color];
        _i %= _rgb.Length;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void PutColorPixel(int gbcRgb)
    {
        if (_i >= _rgb.Length)
        {
            return;
        }
        _rgb[_i++] = TranslateGbcRgb(gbcRgb);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int TranslateGbcRgb(int gbcRgb)
    {
        var r = (gbcRgb >> 0) & 0x1f;
        var g = (gbcRgb >> 5) & 0x1f;
        var b = (gbcRgb >> 10) & 0x1f;
        var result = (r * 8) << 16;
        result |= (g * 8) << 8;
        result |= (b * 8) << 0;
        return result;
    }

    public void RequestRefresh()
    {
        _requestRefresh.Set();
    }

    public void WaitForRefresh()
    {
        if (_token == CancellationToken.None || _token.IsCancellationRequested)
        {
            return;
        }
        WaitHandle.WaitAny([_token.WaitHandle, _refreshDone]);
    }

    private void FillAndDrawBuffer()
    {
        if (Enabled)
        {
            // double buffering, since _rgb is not synchronized
            _rgbPrevious = Interlocked.Exchange(ref _rgb, _rgbPrevious);

            var pi = 0;
            var i = 0;

            while (i < _rgbPrevious.Length)
            {
                var (r, g, b) = ToRgb(_rgbPrevious[i++]);
                _imageBuffer[pi++] = (byte)r;
                _imageBuffer[pi++] = (byte)g;
                _imageBuffer[pi++] = (byte)b;
                _imageBuffer[pi++] = 255;
            }
        }
        else
        {
            ScreenOffOutput.CopyTo(_imageBuffer, 0);
        }

        _synchronizationContext.Post(async _ =>
        {
            await Interop.OutputImage();
            _refreshDone.Set();
        }, null);

        _i = 0;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static (int, int, int) ToRgb(int pixel)
    {
        var b = pixel & 255;
        var g = (pixel >> 8) & 255;
        var r = (pixel >> 16) & 255;
        return (r, g, b);
    }
}
