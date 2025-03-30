using GB.Core.Graphics;
using System.Runtime.CompilerServices;

namespace GB.Core.Cpu.InstructionSet
{
    internal sealed class InstructionBuilder
    {
        private DataType _lastUsedDataType = DataType.None;

        private readonly int _opCode;
        private readonly string _name;
        private readonly List<Operation> _operations = new();

        private static readonly List<Func<Flags, int, int>> OamBug;

        static InstructionBuilder()
        {
            OamBug = new List<Func<Flags, int, int>>
            {
                AluFunctions.GetFunction("INC", DataType.d16),
                AluFunctions.GetFunction("DEC", DataType.d16)
            };
        }

        public InstructionBuilder(int opCode, string name)
        {
            _opCode = opCode;
            _name = name;
        }

        public static bool CausesOamBug(Func<Flags, int, int> func, int context)
        {
            return OamBug.Contains(func) && Operation.InOamArea(context);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int GetOpCode() => _opCode;
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public string GetName() => _name;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public List<Operation> GetOperations() => _operations;

        public InstructionBuilder CopyByte(string source, string target)
        {
            return Load(source).Store(target);
        }

        public InstructionBuilder Load(string source)
        {
            var operand = Operand.Parse(source);
            _lastUsedDataType = operand.DataType;
            _operations.Add(new LoadOperation(operand));
            return this;
        }

        public InstructionBuilder LoadWord(int value)
        {
            _lastUsedDataType = DataType.d16;
            _operations.Add(new LoadWordOperation(value));
            return this;
        }

        public InstructionBuilder Store(string target)
        {
            var operand = Operand.Parse(target);
            if (_lastUsedDataType == DataType.d16 && operand.Name == "(a16)")
            {
                // 16-bit data to be stored in a 16-bit memory address location
                _operations.Add(new StoreLSBOperation(operand));
                _operations.Add(new StoreMSBOperation(operand));
            }
            else if (_lastUsedDataType == operand.DataType)
            {
                _operations.Add(new StoreOperation(operand));
            }
            else
            {
                throw new InvalidOperationException($"Can't store {_lastUsedDataType} in target {target}");
            }

            return this;
        }

        public InstructionBuilder Alu(string operation)
        {
            var func = AluFunctions.GetFunction(operation, _lastUsedDataType);
            _operations.Add(new AluOperationWithSingleOperand(func, operation, _lastUsedDataType));

            if (_lastUsedDataType == DataType.d16)
            {
                AddExtraCycle();
            }

            return this;
        }

        public InstructionBuilder Alu(string operation, int byteValue)
        {
            var func = AluFunctions.GetFunction(operation, _lastUsedDataType, DataType.d8);
            _operations.Add(new AluOperationWithImmediateOperand(func, operation, byteValue));

            if (_lastUsedDataType == DataType.d16)
            {
                AddExtraCycle();
            }

            return this;
        }


        public InstructionBuilder Alu(string operation, string operandName)
        {
            var operand = Operand.Parse(operandName);
            var aluFunction = AluFunctions.GetFunction(operation, _lastUsedDataType, operand.DataType);
            _operations.Add(new AluOperationWithSecondOperand(aluFunction, operand, operation, _lastUsedDataType));

            if (_lastUsedDataType == DataType.d16)
            {
                AddExtraCycle();
            }

            return this;
        }

        public InstructionBuilder AluHL(string operation)
        {
            Load("HL");
            _operations.Add(new AluOperationWithHL(AluFunctions.GetFunction(operation, DataType.d16)));
            return Store("HL");
        }

        public InstructionBuilder Push()
        {
            var dec = AluFunctions.GetFunction("DEC", DataType.d16);
            _operations.Add(new PushMSBOperation(dec));
            _operations.Add(new PushLSBOperation(dec));
            return this;
        }

        public InstructionBuilder Pop()
        {
            var inc = AluFunctions.GetFunction("INC", DataType.d16);
            _lastUsedDataType = DataType.d16;
            _operations.Add(new PopLSBOperation(inc));
            _operations.Add(new PopMSBOperation(inc));
            return this;
        }

        public InstructionBuilder ProceedIf(string condition)
        {
            _operations.Add(new ProceedIfOperation(condition));
            return this;
        }

        public InstructionBuilder BitHL(int bit)
        {
            _operations.Add(new BitHLOperation(bit));
            return this;
        }

        public InstructionBuilder ClearZFlag()
        {
            _operations.Add(new ClearZFlagOperation());
            return this;
        }

        public InstructionBuilder SwitchInterrupts(bool enable, bool withDelay)
        {
            _operations.Add(new SwitchInterruptsOperation(enable, withDelay));
            return this;
        }

        public InstructionBuilder AddExtraCycle()
        {
            _operations.Add(new ExtraCycleOperation());
            return this;
        }

        public InstructionBuilder ForceFinishCycle()
        {
            _operations.Add(new ForceFinishCycleOperation());
            return this;
        }

        public OpCode Build() => new OpCode(this);

        public override string ToString() => _name;

        /// <summary>
        /// Part of an instruction/opcode. Loads immediate, register or referenced (memory) data and returns it.
        /// </summary>
        private sealed class LoadOperation : Operation
        {
            private Operand _operand;

            public LoadOperation(Operand operand)
            {
                _operand = operand;
            }

            public override bool ReadsMemory() => _operand.AccessesMemory;
            public override int Length() => _operand.Bytes;

            public override int Execute(CpuRegisters registers, IAddressSpace addressSpace, int[] args, int context) => _operand.Read(registers, addressSpace, args);

            public override string ToString()
            {
                return string.Format(_operand.DataType == DataType.d16 ? "{0} -> [__]" : "{0} -> [_]", _operand.Name);
            }
        }

        private sealed class LoadWordOperation : Operation
        {
            private readonly int _value;

            public LoadWordOperation(int value)
            {
                _value = value;
            }

            public override int Execute(CpuRegisters registers, IAddressSpace addressSpace, int[] args, int context) => _value;
            public override string ToString() => $"0x{_value:X2} -> [__]";
        }

        /// <summary>
        /// Part of an instruction/opcode. Stores data in a register or memory address.
        /// </summary>
        private sealed class StoreOperation : Operation
        {
            private Operand _operand;

            public StoreOperation(Operand operand)
            {
                _operand = operand;
            }

            public override bool WritesMemory() => _operand.AccessesMemory;
            public override int Length() => _operand.Bytes;

            public override int Execute(CpuRegisters registers, IAddressSpace addressSpace, int[] args, int context)
            {
                _operand.Write(registers, addressSpace, args, context);
                return context;
            }

            public override string ToString()
            {
                return string.Format(_operand.DataType == DataType.d16 ? "[__] -> {0}" : "[_] -> {0}", _operand.Name);
            }
        }

        /// <summary>
        /// Part of an instruction/opcode. Stores the least significant byte of 16-bit data in a memory address.
        /// </summary>
        private sealed class StoreLSBOperation : Operation
        {
            private Operand _operand;

            public StoreLSBOperation(Operand operand)
            {
                _operand = operand;
            }

            public override bool WritesMemory() => _operand.AccessesMemory;
            public override int Length() => _operand.Bytes;

            public override int Execute(CpuRegisters registers, IAddressSpace addressSpace, int[] args, int context)
            {
                addressSpace.SetByte(args.ToWord(), context & 0x00FF);
                return context;
            }

            public override string ToString()
            {
                return $"[ _] -> {_operand.Name}";
            }
        }

        /// <summary>
        /// Part of an instruction/opcode. Stores the most significant byte of 16-bit data in a memory address.
        /// </summary>
        private sealed class StoreMSBOperation : Operation
        {
            private Operand _operand;

            public StoreMSBOperation(Operand operand)
            {
                _operand = operand;
            }

            public override bool WritesMemory() => _operand.AccessesMemory;
            public override int Length() => _operand.Bytes;

            public override int Execute(CpuRegisters registers, IAddressSpace addressSpace, int[] args, int context)
            {
                addressSpace.SetByte((args.ToWord() + 1) & 0xFFFF, (context & 0xFF00) >> 8);
                return context;
            }

            public override string ToString()
            {
                return $"[_ ] -> {_operand.Name}";
            }
        }

        /// <summary>
        /// Part of an instruction/opcode. Performs an arithmetic or logical function and sets the appropriate CPU flags.
        /// </summary>
        private sealed class AluOperationWithSingleOperand : Operation
        {
            private readonly Func<Flags, int, int> _func;
            private readonly string _operation;
            private readonly DataType _lastUsedDataType;

            public AluOperationWithSingleOperand(Func<Flags, int, int> func, string operation, DataType lastUsedDataType)
            {
                _func = func;
                _operation = operation;
                _lastUsedDataType = lastUsedDataType;
            }

            public override int Execute(CpuRegisters registers, IAddressSpace addressSpace, int[] args, int context) => _func(registers.Flags, context);

            public override CorruptionType? CausesOamBug(CpuRegisters registers, int context) =>
                InstructionBuilder.CausesOamBug(_func, context)
                    ? CorruptionType.INC_DEC
                    : null;

            public override string ToString() => _lastUsedDataType == DataType.d16 ? $"{_operation}([__]) → [__]" : $"{_operation}([_]) → [_]";
        }

        private sealed class AluOperationWithImmediateOperand : Operation
        {
            private readonly Func<Flags, int, int, int> _func;
            private readonly string _operation;
            private readonly int _byteValue;

            public AluOperationWithImmediateOperand(Func<Flags, int, int, int> func, string operation, int byteValue)
            {
                _func = func;
                _operation = operation;
                _byteValue = byteValue;
            }

            public override int Execute(CpuRegisters registers, IAddressSpace addressSpace, int[] args, int context)
            {
                return _func(registers.Flags, context, _byteValue);
            }

            public override string ToString()
            {
                return $"{_operation}({_byteValue:D},[_]) -> [_]";
            }
        }

        /// <summary>
        /// Part of an instruction/opcode. Performs an arithmetic or logical function and sets the appropriate CPU flags.
        /// </summary>
        private sealed class AluOperationWithSecondOperand : Operation
        {
            private readonly Func<Flags, int, int, int> _func;
            private readonly Operand _operand;
            private readonly string _operation;
            private readonly DataType _lastUsedDataType;

            public AluOperationWithSecondOperand(Func<Flags, int, int, int> func, Operand operand, string operation, DataType lastUsedDataType)
            {
                _func = func;
                _operand = operand;
                _operation = operation;
                _lastUsedDataType = lastUsedDataType;
            }

            public override bool ReadsMemory() => _operand.AccessesMemory;
            public override int Length() => _operand.Bytes;

            public override int Execute(CpuRegisters registers, IAddressSpace addressSpace, int[] args, int context)
            {
                var operandValue = _operand.Read(registers, addressSpace, args);
                return _func(registers.Flags, context, operandValue);
            }

            public override string ToString()
            {
                return _lastUsedDataType == DataType.d16
                    ? $"{_operation}([__],{_operand}) -> [__]"
                    : $"{_operation}([_],{_operand}) -> [_]";
            }
        }

        /// <summary>
        /// Part of an instruction/opcode. Performs an arithmetic or logical function on/with the HL register and sets the appropriate CPU flags.
        /// </summary>
        private sealed class AluOperationWithHL : Operation
        {
            private readonly Func<Flags, int, int> _func;

            public AluOperationWithHL(Func<Flags, int, int> func)
            {
                _func = func;
            }

            public override int Execute(CpuRegisters registers, IAddressSpace addressSpace, int[] args, int context)
            {
                return _func(registers.Flags, context);
            }

            public override CorruptionType? CausesOamBug(CpuRegisters registers, int context)
            {
                return InstructionBuilder.CausesOamBug(_func, context) ? CorruptionType.LD_HL : null;
            }

            public override string ToString() => "%s(HL) -> [__]";
        }

        /// <summary>
        /// Pops the least significant byte from the stack
        /// </summary>
        private sealed class PopLSBOperation : Operation
        {
            private readonly Func<Flags, int, int> _func;

            public PopLSBOperation(Func<Flags, int, int> func)
            {
                _func = func;
            }

            public override bool ReadsMemory() => true;

            public override int Execute(CpuRegisters registers, IAddressSpace addressSpace, int[] args, int context)
            {
                var lsb = addressSpace.GetByte(registers.SP);
                registers.SP = _func(registers.Flags, registers.SP);
                return lsb;
            }

            public override CorruptionType? CausesOamBug(CpuRegisters registers, int context)
            {
                return InOamArea(registers.SP) ? CorruptionType.POP_1 : null;
            }

            public override string ToString() => "(SP++) -> [ _]";
        }

        /// <summary>
        /// Pops the most significant byte from the stack
        /// </summary>
        private sealed class PopMSBOperation : Operation
        {
            private readonly Func<Flags, int, int> _func;

            public PopMSBOperation(Func<Flags, int, int> func)
            {
                _func = func;
            }

            public override bool ReadsMemory() => true;

            public override int Execute(CpuRegisters registers, IAddressSpace addressSpace, int[] args, int context)
            {
                var msb = addressSpace.GetByte(registers.SP);
                registers.SP = _func(registers.Flags, registers.SP);
                return context | (msb << 8);
            }

            public override CorruptionType? CausesOamBug(CpuRegisters registers, int context)
            {
                return InOamArea(registers.SP) ? CorruptionType.POP_2 : null;
            }

            public override string ToString() => "(SP++) -> [_ ]";
        }

        /// <summary>
        /// Pushes the most significant byte to the stack.
        /// </summary>
        private sealed class PushMSBOperation : Operation
        {
            private readonly Func<Flags, int, int> _func;

            public PushMSBOperation(Func<Flags, int, int> func)
            {
                _func = func;
            }

            public override bool WritesMemory() => true;

            public override int Execute(CpuRegisters registers, IAddressSpace addressSpace, int[] args, int context)
            {
                registers.SP = _func(registers.Flags, registers.SP);
                addressSpace.SetByte(registers.SP, (context & 0xff00) >> 8);
                return context;
            }

            public override CorruptionType? CausesOamBug(CpuRegisters registers, int context)
            {
                return InOamArea(registers.SP) ? CorruptionType.PUSH_1 : null;
            }

            public override string ToString() => "[_ ] → (SP--)";
        }

        /// <summary>
        /// Pushes the least significant byte to the stack.
        /// </summary>
        private sealed class PushLSBOperation : Operation
        {
            private readonly Func<Flags, int, int> _func;

            public PushLSBOperation(Func<Flags, int, int> func)
            {
                _func = func;
            }

            public override bool WritesMemory() => true;

            public override int Execute(CpuRegisters registers, IAddressSpace addressSpace, int[] args, int context)
            {
                registers.SP = _func(registers.Flags, registers.SP);
                addressSpace.SetByte(registers.SP, context & 0x00ff);
                return context;
            }

            public override CorruptionType? CausesOamBug(CpuRegisters registers, int context)
            {
                return InOamArea(registers.SP) ? CorruptionType.PUSH_2 : null;
            }

            public override string ToString() => "[ _] → (SP--)";
        }

        /// <summary>
        /// Proceeds only if the condition is met (for example, NZ => Zero flag is 0)
        /// </summary>
        private sealed class ProceedIfOperation : Operation
        {
            private string _condition;

            public ProceedIfOperation(string condition)
            {
                _condition = condition;
            }

            public override bool ShouldProceed(CpuRegisters registers)
            {
                return _condition switch 
                {
                    "NZ" => !registers.Flags.IsZ(),
                    "Z" => registers.Flags.IsZ(),
                    "NC" => !registers.Flags.IsC(),
                    "C" => registers.Flags.IsC(),
                    _ => false
                };
            }

            public override string ToString() => $"? {_condition}:";
        }

        /// <summary>
        /// Performs the BIT operation on the memory address specified by the HL register.
        /// </summary>
        private sealed class BitHLOperation : Operation
        {
            private readonly int _bit;

            public BitHLOperation(int bit)
            {
                _bit = bit;
            }

            public override bool ReadsMemory() => true;

            public override int Execute(CpuRegisters registers, IAddressSpace addressSpace, int[] args, int context)
            {
                var value = addressSpace.GetByte(registers.HL);
                var flags = registers.Flags;
                flags.SetN(false);
                flags.SetH(true);
                if (_bit < 8)
                {
                    flags.SetZ(!value.GetBit(_bit));
                }

                return context;
            }

            public override string ToString() => $"BIT({_bit:D},HL)";
        }

        /// <summary>
        /// Clears the Zero flag in the flags register.
        /// </summary>
        private sealed class ClearZFlagOperation : Operation
        {
            public override int Execute(CpuRegisters registers, IAddressSpace addressSpace, int[] args, int context)
            {
                registers.Flags.SetZ(false);
                return context;
            }

            public override string ToString() => "0 -> Z";
        }

        private sealed class SwitchInterruptsOperation : Operation
        {
            private readonly bool _enable;
            private readonly bool _withDelay;

            public SwitchInterruptsOperation(bool enable, bool withDelay)
            {
                _enable = enable;
                _withDelay = withDelay;
            }

            public override void SwitchInterrupts(InterruptManager interruptManager)
            {
                if (_enable) 
                {
                    interruptManager.EnableInterrupts(_withDelay);
                }
                else
                {
                    interruptManager.DisableInterrupts(_withDelay);
                }
            }

            public override string ToString()
            {
                return (_enable ? "enable" : "disable") + " interrupts";
            }
        }

        /// <summary>
        /// Indicates that this operation takes an additional CPU cycle to complete.
        /// </summary>
        private sealed class ExtraCycleOperation : Operation
        {
            public override bool ReadsMemory() => true;
            public override string ToString() => "wait cycle";
        }

        /// <summary>
        /// Indicates that this operation casuses the CPU to finish a complete cycle.
        /// </summary>
        private sealed class ForceFinishCycleOperation : Operation
        {
            public override bool ForceFinishCycle() => true;
            public override string ToString() => "finish cycle";
        }
    }
}
