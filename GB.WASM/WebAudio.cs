using GB.Core;
using GB.Core.Sound;
using System;
using System.Threading;

namespace GB.WASM;

public class WebAudio : ISoundOutput
{
    public const int SampleRate = 22050;

    private double[] _buffer;
    private double[] _buffer2;
    private readonly double[] _outputBuffer;

    private int _i = 0;
    private int _tick;
    private readonly int _divider;
    private readonly int _samplesPerFrame;
    private SynchronizationContext _synchronizationContext;

    public WebAudio()
    {
        _divider = Gameboy.TicksPerSec / SampleRate;
        _samplesPerFrame = SampleRate / 60;
        var bufferLength = _samplesPerFrame * 4;

        _buffer = new double[bufferLength];
        _buffer2 = new double[bufferLength];
        
        _outputBuffer = new double[bufferLength];
        Interop.SetupSoundBuffer(new ArraySegment<double>(_outputBuffer), _buffer.Length);

        _synchronizationContext = SynchronizationContext.Current ?? new SynchronizationContext();
    }

    public void Play(int left, int right)
    {
        if (_tick++ != 0)
        {
            _tick %= _divider;
            return;
        }

        left = (int)(left * 0.25);
        right = (int)(right * 0.25);

        left = left < 0 ? 0 : (left > 255 ? 255 : left);
        right = right < 0 ? 0 : (right > 255 ? 255 : right);

        _buffer[_i++] = left / 128f - 1f;
        _buffer[_i++] = right / 128f - 1f;
        if (_i >= _buffer.Length)
        {
            _buffer2 = Interlocked.Exchange(ref _buffer, _buffer2);
            _buffer2.CopyTo(_outputBuffer, 0);

            _synchronizationContext.Post(async _ =>
            {
                await Interop.OutputSound();
            }, null);
            _i = 0;
        }
    }

    public void Start()
    {
        
    }

    public void Stop()
    {
        Array.Clear(_buffer, 0, _buffer.Length);
        Array.Clear(_buffer2, 0, _buffer2.Length);
    }
}
