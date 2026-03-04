using GB.Core.Graphics;
using System.Runtime.CompilerServices;

namespace GB.Core.Cpu.InstructionSet
{
    [Flags]
    internal enum OperationFlags : byte
    {
        None = 0,
        ReadsMemory = 1,
        WritesMemory = 2,
        AccessesMemory = ReadsMemory | WritesMemory,
        ForceFinishCycle = 4,
        HasOamBug = 8,
        HasShouldProceed = 16,
        HasSwitchInterrupts = 32,
    }

    internal abstract class Operation
    {
        /// <summary>
        /// Pre-computed flags set at construction time to avoid virtual dispatch in the hot path.
        /// Subclasses that override ReadsMemory, WritesMemory, ForceFinishCycle, CausesOamBug,
        /// ShouldProceed, or SwitchInterrupts must set the appropriate flags in their constructor.
        /// </summary>
        public OperationFlags Flags;
        public int OperandLength;

        public virtual int Execute(CpuRegisters registers, IAddressSpace addressSpace, int[] args, int context) => context;
        public virtual bool ShouldProceed(CpuRegisters registers) => true;
        public virtual void SwitchInterrupts(InterruptManager interruptManager) { }
        public virtual CorruptionType? CausesOamBug(CpuRegisters registers, int context) => null;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool InOamArea(int address) => address is >= 0xFE00 and <= 0xFEFF;
    }
}
