using System.Runtime.CompilerServices;

namespace GB.Core.Graphics
{
    internal sealed class IntQueue
    {
        private readonly int[] _buffer;
        private int _head;
        private int _tail;
        private int _size;

        public IntQueue(int capacity)
        {
            _buffer = new int[capacity];
            _head = 0;
            _tail = 0;
            _size = 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int Size() => _size;

        public void Enqueue(int value)
        {
            if (_size == _buffer.Length)
            {
                throw new InvalidOperationException("Queue is full");
            }

            _buffer[_tail] = value;
            _tail = (_tail + 1) % _buffer.Length;
            _size++;
        }

        public int Dequeue()
        {
            if (_size == 0)
            {
                throw new InvalidOperationException("Queue is empty");
            }

            var value = _buffer[_head];
            _head = (_head + 1) % _buffer.Length;
            _size--;
            return value;
        }

        public int Get(int index)
        {
            if (index < 0 || index >= _size)
            {
                throw new ArgumentOutOfRangeException(nameof(index));
            }

            return _buffer[(_head + index) % _buffer.Length];
        }

        public void Clear()
        {
            _head = 0;
            _tail = 0;
            _size = 0;
        }

        public void Set(int index, int value)
        {
            if (index < 0 || index >= _size)
            {
                throw new ArgumentOutOfRangeException(nameof(index));
            }

            _buffer[(_head + index) % _buffer.Length] = value;
        }
    }
}
