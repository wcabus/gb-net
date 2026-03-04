using System.Diagnostics.CodeAnalysis;

namespace GB.Core.Cpu.InstructionSet;

internal struct OpCode : IEquatable<OpCode>
{
    private static readonly OpCode[] _opCodes;
    private static readonly OpCode[] _extendedOpCodes;

    public static readonly OpCode NotSet = new OpCode();

    static OpCode()
    {
        var opCodes = new InstructionBuilder[0x100];
        var extendedOpCodes = new InstructionBuilder[0x100];

        RegisterCommand(opCodes, 0x00, "NOP");

        foreach (var (opCode, target) in OpCodesForValues(0x01, 0x10, "BC", "DE", "HL", "SP"))
        {
            RegisterLoadCommand(opCodes, opCode, "d16", target);
        }

        foreach (var (opCode, target) in OpCodesForValues(0x02, 0x10, "(BC)", "(DE)"))
        {
            RegisterLoadCommand(opCodes, opCode, "A", target);
        }

        foreach (var kvp in OpCodesForValues(0x03, 0x10, "BC", "DE", "HL", "SP"))
        {
            RegisterCommand(opCodes, kvp, "INC {}").Load(kvp.Value).Alu("INC").Store(kvp.Value);
        }

        foreach (var kvp in OpCodesForValues(0x04, 0x08, "B", "C", "D", "E", "H", "L", "(HL)", "A"))
        {
            RegisterCommand(opCodes, kvp, "INC {}").Load(kvp.Value).Alu("INC").Store(kvp.Value);
        }

        foreach (var kvp in OpCodesForValues(0x05, 0x08, "B", "C", "D", "E", "H", "L", "(HL)", "A"))
        {
            RegisterCommand(opCodes, kvp, "DEC {}").Load(kvp.Value).Alu("DEC").Store(kvp.Value);
        }

        foreach (var (opCode, target) in OpCodesForValues(0x06, 0x08, "B", "C", "D", "E", "H", "L", "(HL)", "A"))
        {
            RegisterLoadCommand(opCodes, opCode, "d8", target);
        }

        foreach (var kvp in OpCodesForValues(0x07, 0x08, "RLC", "RRC", "RL", "RR"))
        {
            RegisterCommand(opCodes, kvp, kvp.Value + "A").Load("A").Alu(kvp.Value).ClearZFlag().Store("A");
        }

        RegisterLoadCommand(opCodes, 0x08, "SP", "(a16)");

        foreach (var kvp in OpCodesForValues(0x09, 0x10, "BC", "DE", "HL", "SP"))
        {
            RegisterCommand(opCodes, kvp, "ADD HL,{}").Load("HL").Alu("ADD", kvp.Value).Store("HL");
        }

        foreach (var (opCode, source) in OpCodesForValues(0x0a, 0x10, "(BC)", "(DE)"))
        {
            RegisterLoadCommand(opCodes, opCode, source, "A");
        }

        foreach (var kvp in OpCodesForValues(0x0b, 0x10, "BC", "DE", "HL", "SP"))
        {
            RegisterCommand(opCodes, kvp, "DEC {}").Load(kvp.Value).Alu("DEC").Store(kvp.Value);
        }

        RegisterCommand(opCodes, 0x10, "STOP");

        RegisterCommand(opCodes, 0x18, "JR r8").Load("PC").Alu("ADD", "r8").Store("PC");

        foreach (var kvp in OpCodesForValues(0x20, 0x08, "NZ", "Z", "NC", "C"))
        {
            RegisterCommand(opCodes, kvp, "JR {},r8").Load("PC").ProceedIf(kvp.Value).Alu("ADD", "r8").Store("PC");
        }

        RegisterCommand(opCodes, 0x22, "LD (HL+),A").CopyByte("A", "(HL)").AluHL("INC");
        RegisterCommand(opCodes, 0x2a, "LD A,(HL+)").CopyByte("(HL)", "A").AluHL("INC");

        RegisterCommand(opCodes, 0x27, "DAA").Load("A").Alu("DAA").Store("A");
        RegisterCommand(opCodes, 0x2f, "CPL").Load("A").Alu("CPL").Store("A");

        RegisterCommand(opCodes, 0x32, "LD (HL-),A").CopyByte("A", "(HL)").AluHL("DEC");
        RegisterCommand(opCodes, 0x3a, "LD A,(HL-)").CopyByte("(HL)", "A").AluHL("DEC");

        RegisterCommand(opCodes, 0x37, "SCF").Load("A").Alu("SCF").Store("A");
        RegisterCommand(opCodes, 0x3f, "CCF").Load("A").Alu("CCF").Store("A");

        foreach (var (key, target) in OpCodesForValues(0x40, 0x08, "B", "C", "D", "E", "H", "L", "(HL)", "A"))
        {
            foreach (var source in OpCodesForValues(key, 0x01, "B", "C", "D", "E", "H", "L", "(HL)", "A"))
            {
                if (source.Key == 0x76)
                {
                    continue;
                }

                RegisterLoadCommand(opCodes, source.Key, source.Value, target);
            }
        }

        RegisterCommand(opCodes, 0x76, "HALT");

        foreach (var (key, mnemonic) in OpCodesForValues(0x80, 0x08, "ADD", "ADC", "SUB", "SBC", "AND", "XOR", "OR", "CP"))
        {
            foreach (var kvp in OpCodesForValues(key, 0x01, "B", "C", "D", "E", "H", "L", "(HL)", "A"))
            {
                RegisterCommand(opCodes, kvp, mnemonic + " {}").Load("A").Alu(mnemonic, kvp.Value).Store("A");
            }
        }

        foreach (var kvp in OpCodesForValues(0xc0, 0x08, "NZ", "Z", "NC", "C"))
        {
            RegisterCommand(opCodes, kvp, "RET {}").AddExtraCycle().ProceedIf(kvp.Value).Pop().ForceFinishCycle().Store("PC");
        }

        foreach (var kvp in OpCodesForValues(0xc1, 0x10, "BC", "DE", "HL", "AF"))
        {
            RegisterCommand(opCodes, kvp, "POP {}").Pop().Store(kvp.Value);
        }

        foreach (var kvp in OpCodesForValues(0xc2, 0x08, "NZ", "Z", "NC", "C"))
        {
            RegisterCommand(opCodes, kvp, "JP {},a16").Load("a16").ProceedIf(kvp.Value).Store("PC").AddExtraCycle();
        }

        RegisterCommand(opCodes, 0xc3, "JP a16").Load("a16").Store("PC").AddExtraCycle();

        foreach (var kvp in OpCodesForValues(0xc4, 0x08, "NZ", "Z", "NC", "C"))
        {
            RegisterCommand(opCodes, kvp, "CALL {},a16").ProceedIf(kvp.Value).AddExtraCycle().Load("PC").Push().Load("a16").Store("PC");
        }

        foreach (var kvp in OpCodesForValues(0xc5, 0x10, "BC", "DE", "HL", "AF"))
        {
            RegisterCommand(opCodes, kvp, "PUSH {}").AddExtraCycle().Load(kvp.Value).Push();
        }

        foreach (var kvp in OpCodesForValues(0xc6, 0x08, "ADD", "ADC", "SUB", "SBC", "AND", "XOR", "OR", "CP"))
        {
            RegisterCommand(opCodes, kvp, kvp.Value + " d8").Load("A").Alu(kvp.Value, "d8").Store("A");
        }

        for (int i = 0xc7, j = 0x00; i <= 0xf7; i += 0x10, j += 0x10)
        {
            RegisterCommand(opCodes, i, $"RST {j:X2}H").Load("PC").Push().ForceFinishCycle().LoadWord(j).Store("PC");
        }

        RegisterCommand(opCodes, 0xc9, "RET").Pop().ForceFinishCycle().Store("PC");

        RegisterCommand(opCodes, 0xcd, "CALL a16").Load("PC").AddExtraCycle().Push().Load("a16").Store("PC");

        for (int i = 0xcf, j = 0x08; i <= 0xff; i += 0x10, j += 0x10)
        {
            RegisterCommand(opCodes, i, $"RST {j:X2}H").Load("PC").Push().ForceFinishCycle().LoadWord(j).Store("PC");
        }

        RegisterCommand(opCodes, 0xd9, "RETI").Pop().ForceFinishCycle().Store("PC").SwitchInterrupts(true, false);

        RegisterLoadCommand(opCodes, 0xe2, "A", "(C)");
        RegisterLoadCommand(opCodes, 0xf2, "(C)", "A");

        RegisterCommand(opCodes, 0xe9, "JP (HL)").Load("HL").Store("PC");

        RegisterCommand(opCodes, 0xe0, "LDH (a8),A").CopyByte("A", "(a8)");
        RegisterCommand(opCodes, 0xf0, "LDH A,(a8)").CopyByte("(a8)", "A");

        RegisterCommand(opCodes, 0xe8, "ADD SP,r8").Load("SP").Alu("ADD_SP", "r8").AddExtraCycle().Store("SP");
        RegisterCommand(opCodes, 0xf8, "LD HL,SP+r8").Load("SP").Alu("ADD_SP", "r8").Store("HL");

        RegisterLoadCommand(opCodes, 0xea, "A", "(a16)");
        RegisterLoadCommand(opCodes, 0xfa, "(a16)", "A");

        RegisterCommand(opCodes, 0xf3, "DI").SwitchInterrupts(false, true);
        RegisterCommand(opCodes, 0xfb, "EI").SwitchInterrupts(true, true);

        RegisterLoadCommand(opCodes, 0xf9, "HL", "SP").AddExtraCycle();

        foreach (var (key, mnemonic) in OpCodesForValues(0x00, 0x08, "RLC", "RRC", "RL", "RR", "SLA", "SRA", "SWAP", "SRL"))
        {
            foreach (var kvp in OpCodesForValues(key, 0x01, "B", "C", "D", "E", "H", "L", "(HL)", "A"))
            {
                RegisterCommand(extendedOpCodes, kvp, mnemonic + " {}").Load(kvp.Value).Alu(mnemonic).Store(kvp.Value);
            }
        }

        foreach (var (key, mnemonic) in OpCodesForValues(0x40, 0x40, "BIT", "RES", "SET"))
        {
            for (var b = 0; b < 0x08; b++)
            {
                foreach (var kvp in OpCodesForValues(key + b * 0x08, 0x01, "B", "C", "D", "E", "H", "L", "(HL)", "A"))
                {
                    if ("BIT".Equals(mnemonic) && "(HL)".Equals(kvp.Value))
                    {
                        RegisterCommand(extendedOpCodes, kvp, $"BIT {b},(HL)").BitHL(b);
                    }
                    else
                    {
                        RegisterCommand(extendedOpCodes, kvp, $"{mnemonic} {b},{kvp.Value}").Load(kvp.Value).Alu(mnemonic, b).Store(kvp.Value);
                    }
                }
            }
        }

        _opCodes = opCodes.Select(b => b?.Build() ?? NotSet).ToArray();
        _extendedOpCodes = extendedOpCodes.Select(b => b?.Build() ?? NotSet).ToArray();
    }

    public static OpCode[] OpCodes => _opCodes;
    public static OpCode[] ExtendedOpCodes => _extendedOpCodes;

    private static InstructionBuilder RegisterCommand(IList<InstructionBuilder> commands, KeyValuePair<int, string> opCodeAndOperand, string name)
    {
        return RegisterCommand(commands, opCodeAndOperand.Key, name.Replace("{}", opCodeAndOperand.Value));
    }

    private static InstructionBuilder RegisterCommand(IList<InstructionBuilder> commands, int opCode, string name)
    {
        var builder = new InstructionBuilder(opCode, name);
        commands[opCode] = builder;
        return builder;
    }

    private static InstructionBuilder RegisterLoadCommand(IList<InstructionBuilder> commands, int opCode, string source, string target)
    {
        return RegisterCommand(commands, opCode, $"LD {target},{source}").CopyByte(source, target);
    }

    private static Dictionary<int, string> OpCodesForValues(int start, int step, params string[] values)
    {
        var instructionMap = new Dictionary<int, string>();
        var opCode = start;
        foreach (var value in values)
        {
            instructionMap.Add(opCode, value);
            opCode += step;
        }

        return instructionMap;
    }

    public OpCode()
    {
        Value = -1;
        Name = "Invalid OpCode";
        Operations = Array.Empty<Operation>();
        Length = 0;
    }

    public OpCode(InstructionBuilder builder)
    {
        Value = builder.GetOpCode();
        Name = builder.GetName();
        Operations = builder.GetOperations().ToArray();
        Length = Operations.Length <= 0 ? 0 : Operations.Max(x => x.OperandLength);
    }

    public int Value { get; }
    public string Name { get; }
    public Operation[] Operations { get; }
    public int Length { get; }

    public override string ToString()
    {
        return $"{Value:X2} {Name}";
    }

    public override bool Equals([NotNullWhen(true)] object? obj)
    {
        if (obj is null)
        {
            return false;
        }

        if (obj is OpCode other)
        {
            return Equals(other);
        }

        return false;
    }

    public override int GetHashCode()
    {
        return Value.GetHashCode();
    }

    public bool Equals(OpCode other)
    {
        return Value == other.Value;
    }

    public static bool operator ==(OpCode first, OpCode second) 
    {
        return first.Equals(second);
    }

    public static bool operator !=(OpCode first, OpCode second)
    {
        return !(first == second);
    }
}