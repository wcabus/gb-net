using GB.Core.Cpu.InstructionSet;
using GB.Core.Graphics;

namespace GB.Core.Cpu
{
    internal sealed class Processor
    {
        private readonly CpuRegisters _registers;

        private readonly IAddressSpace _addressSpace;
        private readonly InterruptManager _interruptManager;
        private readonly Gpu _gpu;
        private readonly IDisplay _display;
        private readonly SpeedMode _speedMode;

        internal CpuState State = CpuState.OpCode;
        private int _cycles;
        private bool _haltBugMode;

        private int _opcode1;
        private int _opcode2;
        private OpCode _currentOpCode = OpCode.NotSet;

        private readonly int[] _operand = new int[2];
        private Operation[] _ops = Array.Empty<Operation>();
        private int _operandIndex;
        private int _opIndex;

        private int _opContext;
        private int _interruptFlag;
        private int _interruptEnabled;

        private InterruptManager.InterruptType _requestedIrq = InterruptManager.InterruptType.None;

        public Processor(IAddressSpace addressSpace, InterruptManager interruptManager, Gpu gpu, IDisplay display, SpeedMode speedMode)
        {
            _registers = new();

            _addressSpace = addressSpace;
            _interruptManager = interruptManager;
            _gpu = gpu;
            _display = display;
            _speedMode = speedMode;
        }

        public void InitializeRegisters(bool gbc)
        {
            _registers.AF = 0x01b0;
            if (gbc)
            {
                _registers.A = 0x11;
            }

            _registers.BC = 0x0013;
            _registers.DE = 0x00d8;
            _registers.HL = 0x014d;
            _registers.SP = 0xfffe;
            _registers.PC = 0x0100;
        }

        public void Tick()
        {
            _cycles++;
            
            if (_cycles >= (4 / _speedMode.GetSpeedMode()))
            {
                _cycles = 0;
            }
            else
            {
                return;
            }

            if (State is CpuState.OpCode or CpuState.Halted or CpuState.Stopped)
            {
                if (_interruptManager.IsIme() && _interruptManager.IsInterruptRequested())
                {
                    if (State == CpuState.Stopped)
                    {
                        _display.Enabled = true;
                    }

                    State = CpuState.IRQ_ReadInterruptFlag;
                }
            }

            switch (State)
            {
                case CpuState.IRQ_ReadInterruptFlag:
                case CpuState.IRQ_ReadInterruptEnabled:
                case CpuState.IRQ_PushMSB:
                case CpuState.IRQ_PushLSB:
                case CpuState.IRQ_Jump:
                    HandleInterrupt();
                    return;
                case CpuState.Halted when _interruptManager.IsInterruptRequested():
                    State = CpuState.OpCode;
                    break;
            }

            if (State is CpuState.Halted or CpuState.Stopped)
            {
                return;
            }

            var accessedMemory = false;
            for (;;)
            // while (true)
            {
                var pc = _registers.PC;
                switch (State)
                {
                    // Read next opcode
                    case CpuState.OpCode:
                        ClearState();

                        _opcode1 = _addressSpace.GetByte(pc);
                        accessedMemory = true;

                        if (_opcode1 == 0xCB)
                        {
                            State = CpuState.ExtendedOpcode;
                        }
                        else if (_opcode1 == 0x10) // STOP has an additional byte
                        {
                            _currentOpCode = OpCode.OpCodes[_opcode1];
                            State = CpuState.ExtendedOpcode;
                        }
                        else 
                        {
                            State = CpuState.Operands;
                            _currentOpCode = OpCode.OpCodes[_opcode1];
                            if (_currentOpCode == OpCode.NotSet)
                            {
                                throw new InvalidOperationException($"Invalid opcode detected: 0x{_opcode1:X2}");
                            }
                        }
                        
                        // HALT bug: if set, skip incrementing the PC
                        if (!_haltBugMode)
                        {
                            _registers.IncrementPC();
                        }
                        else
                        {
                            _haltBugMode = false;
                        }

                        break;

                    // Some opcodes are two bytes long
                    case CpuState.ExtendedOpcode:
                        if (accessedMemory)
                        {
                            return;
                        }
                        accessedMemory = true;
                        _opcode2 = _addressSpace.GetByte(pc);
                        _currentOpCode = _currentOpCode != OpCode.NotSet ? _currentOpCode : OpCode.ExtendedOpCodes[_opcode2];
                        if (_currentOpCode == OpCode.NotSet)
                        {
                            throw new InvalidOperationException($"Invalid opcode detected: 0xCB{_opcode2:X2}");
                        }


                        State = CpuState.Operands;
                        _registers.IncrementPC();

                        break;

                    // Read operands for the current opcode
                    case CpuState.Operands:
                        while (_operandIndex < _currentOpCode.Length)
                        {
                            if (accessedMemory)
                            {
                                return;
                            }

                            accessedMemory = true;
                            _operand[_operandIndex++] = _addressSpace.GetByte(pc);
                            _registers.IncrementPC();
                        }

                        _ops = _currentOpCode.Operations;
                        State = CpuState.Running;
                        break;

                    // Execute the opcode
                    case CpuState.Running:
                        if (_opcode1 == 0x10) // STOP
                        {
                            if (_speedMode.OnCpuStopped())
                            {
                                State = CpuState.OpCode;
                            }
                            else
                            {
                                State = CpuState.Stopped;
                                _display.Enabled = false;
                            }

                            return;
                        }
                        else if (_opcode1 == 0x76) // HALT
                        {
                            if (_interruptManager.IsHaltBug())
                            {
                                State = CpuState.OpCode;
                                _haltBugMode = true;
                                return;
                            }

                            State = CpuState.Halted;
                            return;
                        }

                        if (_opIndex < _ops.Length)
                        {
                            var op = _ops[_opIndex];
                            var opAccessesMemory = op.ReadsMemory() || op.WritesMemory();
                            if (accessedMemory && opAccessesMemory)
                            {
                                return;
                            }

                            _opIndex++;

                            var corruptionType = op.CausesOamBug(_registers, _opContext);
                            if (corruptionType != null)
                            {
                                HandleSpriteBug(corruptionType.Value);
                            }

                            _opContext = op.Execute(_registers, _addressSpace, _operand, _opContext);
                            op.SwitchInterrupts(_interruptManager);

                            if (!op.ShouldProceed(_registers))
                            {
                                _opIndex = _ops.Length;
                                break;
                            }

                            if (op.ForceFinishCycle())
                            {
                                return;
                            }

                            if (opAccessesMemory)
                            {
                                accessedMemory = true;
                            }
                        }

                        if (_opIndex >= _ops.Length)
                        {
                            State = CpuState.OpCode;
                            _operandIndex = 0;
                            _interruptManager.OnInstructionFinished();
                            return;
                        }

                        break;
                    
                    case CpuState.Halted:
                    case CpuState.Stopped:
                        return;
                }
            }
        }

        private void HandleInterrupt()
        {
            switch (State)
            {
                case CpuState.IRQ_ReadInterruptFlag:
                    _interruptFlag = _addressSpace.GetByte(0xFF0F);
                    State = CpuState.IRQ_ReadInterruptEnabled;
                    break;

                case CpuState.IRQ_ReadInterruptEnabled:
                    _interruptEnabled = _addressSpace.GetByte(0xFFFF);
                    _requestedIrq = InterruptManager.InterruptType.None;
                    
                    foreach (var irq in InterruptManager.InterruptType.Values)
                    {
                        if ((_interruptFlag & _interruptEnabled & (1 << irq.Ordinal)) != 0)
                        {
                            _requestedIrq = irq;
                            break;
                        }
                    }

                    if (_requestedIrq == InterruptManager.InterruptType.None)
                    {
                        // no IRQ requested, continue executing opcodes
                        State = CpuState.OpCode;
                    }
                    else
                    {
                        // IRQ requested, switch the CPU state to start pushing the PC to the stack.
                        State = CpuState.IRQ_PushMSB;
                        _interruptManager.ClearInterrupt(_requestedIrq);
                        _interruptManager.DisableInterrupts(false);
                    }

                    break;

                case CpuState.IRQ_PushMSB:
                    _registers.DecrementSP();
                    _addressSpace.SetByte(_registers.SP, (_registers.PC & 0xFF00) >> 8);
                    State = CpuState.IRQ_PushLSB; // push the LSB of the PC register
                    break;

                case CpuState.IRQ_PushLSB:
                    _registers.DecrementSP();
                    _addressSpace.SetByte(_registers.SP, _registers.PC & 0x00FF);
                    State = CpuState.IRQ_Jump; // jump to the interrupt handler
                    break;

                case CpuState.IRQ_Jump:
                    _registers.PC = _requestedIrq!.Handler;
                    _requestedIrq = InterruptManager.InterruptType.None;
                    State = CpuState.OpCode; // execute the instruction at the interrupt handler
                    break;
            }
        }

        private void HandleSpriteBug(CorruptionType type)
        {
            if (!_gpu.GetLcdc().IsLcdEnabled())
            {
                return;
            }

            var stat = _addressSpace.GetByte(GpuRegister.Stat.Address);
            if ((stat & 0b11) == (int)Gpu.Mode.OamSearch && _gpu.GetTicksInLine() < 79)
            {
                SpriteBug.CorruptOam(_addressSpace, type, _gpu.GetTicksInLine());
            }
        }

        private void ClearState()
        {
            _opcode1 = 0;
            _opcode2 = 0;
            _currentOpCode = OpCode.NotSet;
            _ops = Array.Empty<Operation>();

            _operand[0] = 0x00;
            _operand[1] = 0x00;
            _operandIndex = 0;

            _opIndex = 0;
            _opContext = 0;

            _interruptFlag = 0;
            _interruptEnabled = 0;
            _requestedIrq = InterruptManager.InterruptType.None;
        }
    }
}