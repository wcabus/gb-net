using System;
using System.Threading;

namespace GB.WASM;

public sealed class CircularByteBuffer
{
    private readonly byte[] _buffer;
    private int _writePos;
    private int _readPos;
    private int _byteCount;
    private ManualResetEventSlim _writingDone = new(true);
    private ManualResetEventSlim _readingDone = new(true);
    
    public CircularByteBuffer(int size)
    {
        _buffer = new byte[size];
    }

    public int Write(byte[] data, int offset, int count)
    {
        WaitHandle.WaitAll([_writingDone.WaitHandle, _readingDone.WaitHandle]);

        _writingDone.Reset();
        if (count > _buffer.Length - _byteCount)
        {
            count = _buffer.Length - _byteCount;
        }

        var length = Math.Min(_buffer.Length - _writePos, count);
        Array.Copy(data, offset, _buffer, _writePos, length);
        _writePos += length;
        _writePos %= _buffer.Length;
        var bytesWritten = length;
        if (bytesWritten < count)
        {
            Array.Copy(data, offset + bytesWritten, _buffer, _writePos, count - bytesWritten);
            _writePos += count - bytesWritten;
            bytesWritten = count;
        }

        _byteCount += bytesWritten;
        _writingDone.Set();
     
        return bytesWritten;
    }
    
    public int Read(byte[] data, int offset, int count)
    {
        WaitHandle.WaitAll([_writingDone.WaitHandle, _readingDone.WaitHandle]);
        
        _readingDone.Reset();
        if (count > _byteCount)
        {
            count = _byteCount;
        }
        
        var length = Math.Min(_buffer.Length - _readPos, count);
        Array.Copy(_buffer, _readPos, data, offset, length);
        var bytesRead = length;
        _readPos += length;
        _readPos %= _buffer.Length;
        if (bytesRead < count)
        {
            Array.Copy(_buffer, _readPos, data, offset + bytesRead, count - bytesRead);
            _readPos += count - bytesRead;
            bytesRead = count;
        }
        
        _byteCount -= bytesRead;
        _readingDone.Set();
        
        return bytesRead;
    }

    public int Count
    {
        get
        {
            WaitHandle.WaitAll([_writingDone.WaitHandle, _readingDone.WaitHandle]);
            return _byteCount;
        }
    }
}