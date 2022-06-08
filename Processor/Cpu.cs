namespace Processor;

public class Cpu
{
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
    private delegate (byte, Action<byte>) AddressMode();
    private readonly AddressMode[] _addressModes;

    private delegate void Instruction(byte value, Action<byte> write);
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
        var (value, write) = _addressModes[opCode]();
        PC += _instructionBytes[opCode];
        _cycles += _instructionCycles[opCode];
        _instructions[opCode](value, write);
        return _cycles - cycles;
    }
    
    // Addressing modes
    
    //Accumulator
    private (byte, Action<byte>) ACC() =>
        throw new NotImplementedException();

    //Implied
    private (byte, Action<byte>) IMP() =>
        throw new NotImplementedException();

    //Immediate
    private (byte, Action<byte>) IMM() =>
        throw new NotImplementedException();

    //Absolute
    private (byte, Action<byte>) ABS() =>
        throw new NotImplementedException();

    //Zeropage
    private (byte, Action<byte>) ZP() =>
        throw new NotImplementedException();

    //Relative
    private (byte, Action<byte>) REL() =>
        throw new NotImplementedException();
  
    //Indirect
    private (byte, Action<byte>) IND() =>
        throw new NotImplementedException();
    
    //Absolute_x
    private (byte, Action<byte>) ABS_X() =>
        throw new NotImplementedException();
    
    //Absolute_y
    private (byte, Action<byte>) ABS_Y() =>
        throw new NotImplementedException();
    
    //Zeropage_x
    private (byte, Action<byte>) ZP_X() =>
        throw new NotImplementedException();
    
    //Zeropage_y
    private (byte, Action<byte>) ZP_Y() =>
        throw new NotImplementedException();
    
    //Indirect_x
    private (byte, Action<byte>) IND_X() =>
        throw new NotImplementedException();
    
    //Indirect_y
    private (byte, Action<byte>) IND_Y() =>
        throw new NotImplementedException();

    //Invalid opcode
    private (byte, Action<byte>) XXX() =>
        throw new Exception();
    
    // Instructions
    
    void lda(byte value, Action<byte> write) =>
        throw new NotImplementedException();
    
    void ldx(byte value, Action<byte> write) =>
        throw new NotImplementedException();
    
    void ldy(byte value, Action<byte> write) =>
        throw new NotImplementedException();
    
    void sta(byte value, Action<byte> write) =>
        throw new NotImplementedException();
    
    void stx(byte value, Action<byte> write) =>
        throw new NotImplementedException();
    
    void sty(byte value, Action<byte> write) =>
        throw new NotImplementedException();
    
    void adc(byte value, Action<byte> write) =>
        throw new NotImplementedException();
    
    void sbc(byte value, Action<byte> write) =>
        throw new NotImplementedException();
    
    void inc(byte value, Action<byte> write) =>
        throw new NotImplementedException();
    
    void inx(byte value, Action<byte> write) =>
        throw new NotImplementedException();
    
    void iny(byte value, Action<byte> write) =>
        throw new NotImplementedException();
    
    void dec(byte value, Action<byte> write) =>
        throw new NotImplementedException();
    
    void dex(byte value, Action<byte> write) =>
        throw new NotImplementedException();
    
    void dey(byte value, Action<byte> write) =>
        throw new NotImplementedException();
    
    void asl(byte value, Action<byte> write) =>
        throw new NotImplementedException();
    
    void lsr(byte value, Action<byte> write) =>
        throw new NotImplementedException();
    
    void rol(byte value, Action<byte> write) =>
        throw new NotImplementedException();
    
    void ror(byte value, Action<byte> write) =>
        throw new NotImplementedException();
    
    void and(byte value, Action<byte> write) =>
        throw new NotImplementedException();
    
    void ora(byte value, Action<byte> write) =>
        throw new NotImplementedException();
    
    void eor(byte value, Action<byte> write) =>
        throw new NotImplementedException();
    
    void cmp(byte value, Action<byte> write) =>
        throw new NotImplementedException();
    
    void cpx(byte value, Action<byte> write) =>
        throw new NotImplementedException();
    
    void cpy(byte value, Action<byte> write) =>
        throw new NotImplementedException();
    
    void bit(byte value, Action<byte> write) =>
        throw new NotImplementedException();
    
    void bcc(byte value, Action<byte> write) =>
        throw new NotImplementedException();
    
    void bcs(byte value, Action<byte> write) =>
        throw new NotImplementedException();
    
    void bne(byte value, Action<byte> write) =>
        throw new NotImplementedException();
    
    void beq(byte value, Action<byte> write) =>
        throw new NotImplementedException();
    
    void bpl(byte value, Action<byte> write) =>
        throw new NotImplementedException();
    
    void bmi(byte value, Action<byte> write) =>
        throw new NotImplementedException();
    
    void bvc(byte value, Action<byte> write) =>
        throw new NotImplementedException();
    
    void bvs(byte value, Action<byte> write) =>
        throw new NotImplementedException();
    
    void tax(byte value, Action<byte> write) =>
        throw new NotImplementedException();
    
    void txa(byte value, Action<byte> write) =>
        throw new NotImplementedException();
    
    void tay(byte value, Action<byte> write) =>
        throw new NotImplementedException();
    
    void tya(byte value, Action<byte> write) =>
        throw new NotImplementedException();
    
    void tsx(byte value, Action<byte> write) =>
        throw new NotImplementedException();
    
    void txs(byte value, Action<byte> write) =>
        throw new NotImplementedException();
    
    void pha(byte value, Action<byte> write) =>
        throw new NotImplementedException();
    
    void pla(byte value, Action<byte> write) =>
        throw new NotImplementedException();
    
    void php(byte value, Action<byte> write) =>
        throw new NotImplementedException();

    void plp(byte value, Action<byte> write) =>
        throw new NotImplementedException();
    
    void jmp(byte value, Action<byte> write) =>
        throw new NotImplementedException();
    
    void jsr(byte value, Action<byte> write) =>
        throw new NotImplementedException();
    
    void rts(byte value, Action<byte> write) =>
        throw new NotImplementedException();
    
    void rti(byte value, Action<byte> write) =>
        throw new NotImplementedException();
    
    void clc(byte value, Action<byte> write) =>
        throw new NotImplementedException();
    
    void sec(byte value, Action<byte> write) =>
        throw new NotImplementedException();
    
    void cld(byte value, Action<byte> write) =>
        throw new NotImplementedException();
    
    void sed(byte value, Action<byte> write) =>
        throw new NotImplementedException();
    
    void cli(byte value, Action<byte> write) =>
        throw new NotImplementedException();
    
    void sei(byte value, Action<byte> write) =>
        throw new NotImplementedException();
    
    void clv(byte value, Action<byte> write) =>
        throw new NotImplementedException();
    
    void brk(byte value, Action<byte> write) =>
        throw new NotImplementedException();
    
    void nop(byte value, Action<byte> write) =>
        throw new NotImplementedException();

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