using GB.Core.Cpu;
using System.Collections.Concurrent;

namespace GB.Core.Controller
{
    internal sealed class Joypad : IAddressSpace
    {
        private readonly ConcurrentDictionary<Button, Button> _buttons = new();
        private int _p1;

        public Joypad(InterruptManager interruptManager, IController controller)
        {
            controller.SetButtonListener(new JoyPadButtonListener(interruptManager, _buttons));
        }

        public bool Accepts(int address)
        {
            return address == 0xFF00;
        }

        public void SetByte(int address, int value)
        {
            _p1 = value & 0b00110000;
        }

        public int GetByte(int address)
        {
            var result = _p1 | 0b11001111;
            foreach (var b in _buttons.Keys)
            {
                if ((b.Line & _p1) == 0)
                {
                    result &= 0xFF & ~b.Mask;
                }
            }

            return result;
        }
    }
}
