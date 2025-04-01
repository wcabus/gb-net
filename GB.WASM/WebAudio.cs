using GB.Core;
using GB.Core.Sound;
using System;
using System.Threading;

namespace GB.WASM;

public class WebAudio : ISoundOutput
{
    public const int SampleRate = 22050;

    private byte[] _buffer;
    private readonly byte[] _outputBuffer;
    private CircularByteBuffer _circularBuffer;
    
    private int _i = 0;
    private int _tick;
    private readonly int _divider;
    private readonly int _samplesPerFrame;
    private SynchronizationContext _synchronizationContext;

    public WebAudio()
    {
        _divider = Gameboy.TicksPerSec / SampleRate;
        _samplesPerFrame = SampleRate / 60;
        var bufferLength = _samplesPerFrame * 32;

        _buffer = new byte[bufferLength];
        _circularBuffer = new CircularByteBuffer(SampleRate * 5); // 5 seconds of audio
        
        _outputBuffer = new byte[bufferLength];
        Interop.SetupSoundBuffer(new ArraySegment<byte>(_outputBuffer), _buffer.Length);

        _synchronizationContext = SynchronizationContext.Current ?? new SynchronizationContext();
    }

    public void Play(int left, int right)
    {
        if (_tick++ != 0)
        {
            _tick %= _divider;
            return;
        }

        if (_i >= _buffer.Length)
        {
            return;
        }
        
        left = (int)(left * 0.25);
        right = (int)(right * 0.25);

        left = left < 0 ? 0 : (left > 255 ? 255 : left);
        right = right < 0 ? 0 : (right > 255 ? 255 : right);

        _buffer[_i++] = (byte)left;
        _buffer[_i++] = (byte)right;

        if (_i >= _buffer.Length)
        {
            _circularBuffer.Write(_buffer, 0, _buffer.Length);
            _i = 0;
        }
        
        if (_circularBuffer.Count > 0)
        {
            var outputBuffer = new byte[_buffer.Length];
            var read = _circularBuffer.Read(outputBuffer, 0, outputBuffer.Length);
            while (read < outputBuffer.Length)
            {
                outputBuffer[read++] = 0;
            }
            
            _synchronizationContext.Post(_ =>
            {
                Interop.OutputSoundBuffer(outputBuffer, outputBuffer.Length);
            }, null);
        }
    }

    public void Start()
    {
        
    }

    public void Stop()
    {
    }
}
