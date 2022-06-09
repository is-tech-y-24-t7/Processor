namespace Processor;

public class Cpu
{
    private struct InstructionContext
    {
        public InstructionContext(byte value, ushort address, Action<byte> write)
        {
            _value = value;
            Address = address;
            _write = write;
        }

        public byte Value
        {
            get => _value;
            set
            {
                _value = value;
                _write.Invoke(value);
            }
        }

        public ushort Address { get; }
        private Action<byte> _write;
        private byte _value;
}
    
    private readonly ICpuMemory _memory;
    private byte A;    // Аккумулятор
    private byte X;    //Индекс X 
    private byte Y;    //Индекс Y
    private ushort PC; // Cчетчик команд, 2 байта
    private byte S;    // Указатель вершины стека
    
    // (P) Регистр статуса длиной 1 байт, разбит на 8 битов
    private bool C; //Carry flag
    private bool Z; // Zero flag
    private bool I; // Interrpt Disable
    private bool D; // Decimal Flag
    private bool B; // Break command
    private bool V; // Overflow flag
    private bool N; // Negative flag
    private delegate InstructionContext AddressMode();
    private readonly AddressMode[] _addressModes;

    private delegate void Instruction(InstructionContext ctx);
    private readonly Instruction[] _instructions;

    private int _cycles;

    private byte P
    {
        get
        {
            byte value = 0;
            if (C) value |= 1 << 0;
            if (Z) value |= 1 << 1;
            if (I) value |= 1 << 2;
            if (D) value |= 1 << 3;
            if (B) value |= 1 << 4;
            value |= 1 << 5;
            if (V) value |= 1 << 6;
            if (N) value |= 1 << 7;
            return value;
        }
        set
        {
            C = (value & 1<<0 ) != 0 ;
            Z = (value & 1<<1) != 0;
            I = (value & 1<<2) != 0;
            D = (value & 1<<3) != 0;
            B = (value & 1<<4) != 0;
            V = (value & 1<<6) != 0;
            N = (value & 1<<7) != 0;
        }
    }

    public Cpu(ICpuMemory memory)
    {
        _addressModes = InitAddressModes();
        _instructions = InitInstructions();
        _memory = memory;
        Reset();
    }

    public void Reset()
    {
        A = 0;
        X = 0;
        Y = 0;
        S = 0xFD;
        P = 0x34;
        PC = _memory.Read16(0xFFFC);
    }

    public int Step()
    {
        var cycles = _cycles;
        var opCode = _memory.Read(PC);
        var ctx = _addressModes[opCode]();
        PC += _instructionBytes[opCode];
        _cycles += _instructionCycles[opCode];
        _instructions[opCode](ctx);
        return _cycles - cycles;
    }

    private void CheckPageCross(ushort frm, ushort to)
        => throw new NotImplementedException();
    
    // Addressing modes
    
    //Accumulator
    private InstructionContext ACC() =>
        new (A, 0, value => A = value);

    //Implied
    private static InstructionContext IMP() =>
        new (0, 0, _ => { });
    
    private InstructionContext Addressed(ushort address) =>
        new(_memory.Read(address), address, value => _memory.Write(address, value));
    
    //Immediate
    private InstructionContext IMM() =>
        Addressed((ushort) (PC + 1));

    //Absolute
    private InstructionContext ABS() =>
        Addressed(_memory.Read16((ushort) (PC + 1)));

    //Zeropage
    private InstructionContext ZP() =>
        Addressed(_memory.Read((ushort) (PC + 1)));

    //Relative
    private InstructionContext REL() =>
        new (_memory.Read((ushort) (PC + 1)), 0, _ => { });
  
    //Indirect
    private InstructionContext IND() =>
        Addressed(_memory.Read16Wrap(_memory.Read16((ushort) (PC + 1))));

    private InstructionContext AbsoluteIndexed(byte offset)
    {
        var address = _memory.Read16((ushort) (PC + 1));
        var newAddress = (ushort) (address + offset);
        CheckPageCross(address, newAddress);
        return Addressed(newAddress);
    }
    //Absolute_x
    private InstructionContext ABS_X() =>
        AbsoluteIndexed(X);

    //Absolute_y
    private InstructionContext ABS_Y() =>
        AbsoluteIndexed(Y);
    
    private InstructionContext ZeroPageIndexed(byte offset) =>
        Addressed((ushort) ((_memory.Read((ushort) (PC + 1)) + offset) & 0xFF));
    
    //Zeropage_x
    private InstructionContext ZP_X() =>
        ZeroPageIndexed(X);
    
    //Zeropage_y
    private InstructionContext ZP_Y() =>
        ZeroPageIndexed(Y);
    
    //Indirect_x
    private InstructionContext IND_X() =>
        Addressed(_memory.Read16Wrap((ushort) ((_memory.Read((ushort) (PC + 1)) + X) & 0xFF)));
    
    //Indirect_y
    private InstructionContext IND_Y()
    {
        var address = _memory.Read16Wrap(_memory.Read((ushort) (PC + 1)));
        var newAddress = (ushort) (address + Y);
        CheckPageCross(address, newAddress);
        return Addressed(newAddress);
    }

    //Invalid opcode
    private InstructionContext XXX() =>
        throw new Exception();
    
    // Instructions
    void SetZN(byte value)
    {
        Z = value == 0;
        N = ((value >> 7) & 1) == 1;
    }
    
    // Load

    void lda(InstructionContext ctx)
        => SetZN(A = ctx.Value);

    void ldx(InstructionContext ctx) =>
        SetZN(X = ctx.Value);

    void ldy(InstructionContext ctx) =>
        SetZN(Y = ctx.Value);
    // Store

    void sta(InstructionContext ctx) =>
        ctx.Value = A;

    void stx(InstructionContext ctx) =>
        ctx.Value = X;

    void sty(InstructionContext ctx) =>
        ctx.Value = Y;
    
    // Arithmetic
    
    void adc(InstructionContext ctx)
    {
        var sum = A + ctx.Value + (C ? 1 : 0);
        C = sum > 0xFF;
        var result = (byte) sum;
        V = (~(A ^ ctx.Value) & (A ^ result) & 0x80) != 0;
        SetZN(A = result);
    }

    void sbc(InstructionContext ctx)
    {
        var diff = A - ctx.Value - (C ? 0 : 1);
        C = diff >= 0;
        var result = (byte) diff;
        V = ((A ^ ctx.Value) & (A ^ result) & 0x80) != 0;
        SetZN(A = result);
    }
    
    // Increment and Decrement

    void inc(InstructionContext ctx) =>
        SetZN(++ctx.Value);

    void inx(InstructionContext ctx) =>
        SetZN(++X);

    void iny(InstructionContext ctx) =>
        SetZN(++Y);

    void dec(InstructionContext ctx) =>
        SetZN(--ctx.Value);

    void dex(InstructionContext ctx) =>
        SetZN(--X);

    void dey(InstructionContext ctx) =>
        SetZN(--Y);
    
    // Shift and Rotate

    void asl(InstructionContext ctx)
    {
        C = ((ctx.Value >> 7) & 1) == 1;
        ctx.Value <<= 1;
        SetZN(ctx.Value);
    }

    void lsr(InstructionContext ctx)
    {
        C = (ctx.Value & 1) == 1;
        ctx.Value >>= 1;
        SetZN(ctx.Value);
    }

    void rol(InstructionContext ctx)
    {
        var oldC = C;
        C = ((ctx.Value >> 7) & 1) == 1;
        ctx.Value = (byte) ((ctx.Value << 1) | (oldC ? 1 : 0));
        SetZN(ctx.Value);
    }

    void ror(InstructionContext ctx)
    {
        var oldC = C;
        C = (ctx.Value & 1) == 1;
        ctx.Value = (byte) ((ctx.Value >> 1) | ((oldC ? 1 : 0) << 7));
        SetZN(ctx.Value);
    }
    
    // Logic

    void and(InstructionContext ctx) =>
        SetZN(A &= ctx.Value);

    void ora(InstructionContext ctx) =>
        SetZN(A |= ctx.Value);

    void eor(InstructionContext ctx) =>
        SetZN(A ^= ctx.Value);
    // Compare and Test Bit

    void Compare(int diff)
    {
        N = diff < 0;
        Z = diff == 0;
        C = diff >= 0;
    }

    void cmp(InstructionContext ctx) =>
        Compare(A - ctx.Value);

    void cpx(InstructionContext ctx) =>
        Compare(X - ctx.Value);

    void cpy(InstructionContext ctx) =>
        Compare(Y - ctx.Value);

    void bit(InstructionContext ctx)
    {
        N = ((ctx.Value >> 7) & 1) == 1;
        V = ((ctx.Value >> 6) & 1) == 1;
        Z = (A & ctx.Value) == 0;
    }
    // Branch

    bool IsPageCross(ushort from, ushort to) =>
        throw new NotImplementedException();

    void Branch(bool cond, byte offset)
    {
        if (!cond) return;
        var newPC = (ushort) (PC + (sbyte) offset);
        _cycles += 1 + (IsPageCross(PC, newPC) ? 1 : 0);
        PC = newPC;
    }

    void bcc(InstructionContext ctx) =>
        Branch(!C, ctx.Value);

    void bcs(InstructionContext ctx) =>
        Branch(C, ctx.Value);

    void bne(InstructionContext ctx) =>
        Branch(!Z, ctx.Value);

    void beq(InstructionContext ctx) =>
        Branch(Z, ctx.Value);

    void bpl(InstructionContext ctx) =>
        Branch(!N, ctx.Value);

    void bmi(InstructionContext ctx) =>
        Branch(N, ctx.Value);

    void bvc(InstructionContext ctx) =>
        Branch(!V, ctx.Value);

    void bvs(InstructionContext ctx) =>
        Branch(V, ctx.Value);
    
    // Transfer

    void tax(InstructionContext ctx) =>
        SetZN(X = A);

    void txa(InstructionContext ctx) =>
        SetZN(A = X);

    void tay(InstructionContext ctx) =>
        SetZN(Y = A);

    void tya(InstructionContext ctx) =>
        SetZN(A = Y);

    void tsx(InstructionContext ctx) =>
        SetZN(X = S);

    void txs(InstructionContext ctx) =>
        SetZN(S = X);
    
    // Stack

    void pha(InstructionContext ctx) =>
        S = A;

    void pla(InstructionContext ctx) =>
        A = S;

    void php(InstructionContext ctx) =>
        S = P;

    void plp(InstructionContext ctx) =>
        P = S;
    
    // Subroutines and Jump
    
    void jmp(InstructionContext ctx) =>
        throw new NotImplementedException();
    
    void jsr(InstructionContext ctx) =>
        throw new NotImplementedException();
    
    void rts(InstructionContext ctx) =>
        throw new NotImplementedException();
    
    void rti(InstructionContext ctx) =>
        throw new NotImplementedException();
    
    // Set and Clear

    void clc(InstructionContext ctx) =>
        C = false;

    void sec(InstructionContext ctx) =>
        C = true;

    void cld(InstructionContext ctx) =>
        D = false;

    void sed(InstructionContext ctx) =>
        D = true;

    void cli(InstructionContext ctx) =>
        I = false;

    void sei(InstructionContext ctx) =>
        I = true;

    void clv(InstructionContext ctx) =>
        V = false;
    
    // Misc

    void brk(InstructionContext ctx)
    {
        B = true;
        I = true;
    }
    
    void nop(InstructionContext ctx) { }

    void xxx(InstructionContext ctx) =>
        throw new Exception();

    // Constants
    
    private readonly byte[] _instructionBytes =
    {
    //  0  1  2  3  4  5  6  7  8  9  A  B  C  D  E  F
        1, 2, 0, 0, 0, 2, 2, 0, 1, 2, 1, 0, 0, 3, 3, 0, // 0
        2, 2, 0, 0, 0, 2, 2, 0, 1, 3, 0, 0, 0, 3, 3, 0, // 1
        3, 2, 0, 0, 2, 2, 2, 0, 1, 2, 1, 0, 3, 3, 3, 0, // 2
        2, 2, 0, 0, 0, 2, 2, 0, 1, 3, 0, 0, 0, 3, 3, 0, // 3
        1, 2, 0, 0, 0, 2, 2, 0, 1, 2, 1, 0, 3, 3, 3, 0, // 4
        2, 2, 0, 0, 0, 2, 2, 0, 1, 3, 0, 0, 0, 3, 3, 0, // 5
        1, 2, 0, 0, 0, 2, 2, 0, 1, 2, 1, 0, 3, 3, 3, 0, // 6
        2, 2, 0, 0, 0, 2, 2, 0, 1, 3, 0, 0, 0, 3, 3, 0, // 7
        0, 2, 0, 0, 2, 2, 2, 0, 1, 0, 1, 0, 3, 3, 3, 0, // 8
        2, 2, 0, 0, 2, 2, 2, 0, 1, 3, 1, 0, 0, 3, 0, 0, // 9
        2, 2, 2, 0, 2, 2, 2, 0, 1, 2, 1, 0, 3, 3, 3, 0, // A
        2, 2, 0, 0, 2, 2, 2, 0, 1, 3, 1, 0, 3, 3, 3, 0, // B
        2, 2, 0, 0, 2, 2, 2, 0, 1, 2, 1, 0, 3, 3, 3, 0, // C
        2, 2, 0, 0, 0, 2, 2, 0, 1, 3, 0, 0, 0, 3, 3, 0, // D
        2, 2, 0, 0, 2, 2, 2, 0, 1, 2, 1, 0, 3, 3, 3, 0, // E
        2, 2, 0, 0, 0, 2, 2, 0, 1, 3, 0, 0, 0, 3, 3, 0, // F
    };
        
    private readonly byte[] _instructionCycles =
    { 
    //  0  1  2  3  4  5  6  7  8  9  A  B  C  D  E  F    
        7, 6, 0, 0, 0, 3, 5, 0, 3, 2, 2, 0, 0, 4, 6, 0, // 0
        2, 5, 0, 0, 0, 4, 6, 0, 2, 4, 0, 0, 0, 4, 7, 0, // 1
        6, 6, 0, 0, 3, 3, 5, 0, 4, 2, 2, 0, 4, 4, 6, 0, // 2
        2, 5, 0, 0, 0, 4, 6, 0, 2, 4, 0, 0, 0, 4, 7, 0, // 3
        6, 6, 0, 0, 0, 3, 5, 0, 3, 2, 2, 0, 3, 4, 6, 0, // 4
        2, 5, 0, 0, 0, 4, 6, 0, 2, 4, 0, 0, 0, 4, 7, 0, // 5
        6, 6, 0, 0, 0, 3, 5, 0, 4, 2, 2, 0, 5, 4, 6, 0, // 6
        2, 5, 0, 0, 0, 4, 6, 0, 2, 4, 0, 0, 0, 4, 7, 0, // 7
        0, 6, 0, 0, 3, 3, 3, 0, 2, 0, 2, 0, 4, 4, 4, 0, // 8
        2, 6, 0, 0, 4, 4, 4, 0, 2, 5, 2, 0, 0, 5, 0, 0, // 9
        2, 6, 2, 0, 3, 3, 3, 0, 2, 2, 2, 0, 4, 4, 4, 0, // A
        2, 5, 0, 0, 4, 4, 4, 0, 2, 4, 2, 0, 4, 4, 4, 0, // B
        2, 6, 0, 0, 3, 3, 5, 0, 2, 2, 2, 0, 4, 4, 6, 0, // C
        2, 5, 0, 0, 0, 4, 6, 0, 2, 4, 0, 0, 0, 4, 7, 0, // D
        2, 6, 0, 0, 3, 3, 5, 0, 2, 2, 2, 0, 4, 4, 6, 0, // E
        2, 5, 0, 0, 0, 4, 6, 0, 2, 4, 0, 0, 0, 4, 7, 0, // F
    };

    private AddressMode[] InitAddressModes() =>
        new AddressMode[] { 
        //  0    1      2    3    4     5     6     7    8    9      A    B    C      D      E      F   
            IMP, IND_X, XXX, XXX, XXX,  ZP,   ZP,   XXX, IMP, IMM,   ACC, XXX, XXX,   ABS,   ABS,   XXX, // 0  
            REL, IND_Y, XXX, XXX, XXX,  ZP_X, ZP_X, XXX, IMP, ABS_Y, XXX, XXX, XXX,   ABS_X, ABS_X, XXX, // 1  
            ABS, IND_X, XXX, XXX, ZP,   ZP,   ZP,   XXX, IMP, IMM,   ACC, XXX, ABS,   ABS,   ABS,   XXX, // 2  
            REL, IND_Y, XXX, XXX, XXX,  ZP_X, ZP_X, XXX, IMP, ABS_Y, XXX, XXX, XXX,   ABS_X, ABS_X, XXX, // 3  
            IMP, IND_X, XXX, XXX, XXX,  ZP,   ZP,   XXX, IMP, IMM,   ACC, XXX, ABS,   ABS,   ABS,   XXX, // 4  
            REL, IND_Y, XXX, XXX, XXX,  ZP_X, ZP_X, XXX, IMP, ABS_Y, XXX, XXX, XXX,   ABS_X, ABS_X, XXX, // 5  
            IMP, IND_X, XXX, XXX, XXX,  ZP,   ZP,   XXX, IMP, IMM,   ACC, XXX, IND,   ABS,   ABS,   XXX, // 6  
            REL, IND_Y, XXX, XXX, XXX,  ZP_X, ZP_X, XXX, IMP, ABS_Y, XXX, XXX, XXX,   ABS_X, ABS_X, XXX, // 7  
            XXX, IND_X, XXX, XXX, ZP,   ZP,   ZP,   XXX, IMP, XXX,   IMP, XXX, ABS,   ABS,   ABS,   XXX, // 8  
            REL, IND_Y, XXX, XXX, ZP_X, ZP_X, ZP_Y, XXX, IMP, ABS_Y, IMP, XXX, XXX,   ABS_X, XXX,   XXX, // 9  
            IMM, IND_X, IMM, XXX, ZP,   ZP,   ZP,   XXX, IMP, IMM,   IMP, XXX, ABS,   ABS,   ABS,   XXX, // A  
            REL, IND_Y, XXX, XXX, ZP_X, ZP_X, ZP_Y, XXX, IMP, ABS_Y, IMP, XXX, ABS_X, ABS_X, ABS_Y, XXX, // B  
            IMM, IND_X, XXX, XXX, ZP,   ZP,   ZP,   XXX, IMP, IMM,   IMP, XXX, ABS,   ABS,   ABS,   XXX, // C  
            REL, IND_Y, XXX, XXX, XXX,  ZP_X, ZP_X, XXX, IMP, ABS_Y, XXX, XXX, XXX,   ABS_X, ABS_X, XXX, // D  
            IMM, IND_X, XXX, XXX, ZP,   ZP,   ZP,   XXX, IMP, IMM,   IMP, XXX, ABS,   ABS,   ABS,   XXX, // E  
            REL, IND_Y, XXX, XXX, XXX,  ZP_X, ZP_X, XXX, IMP, ABS_Y, XXX, XXX, XXX,   ABS_X, ABS_X, XXX, // F
        };

    private Instruction[] InitInstructions() =>
        throw new NotImplementedException();
}