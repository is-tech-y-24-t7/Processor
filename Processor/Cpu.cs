namespace Processor;

using static Cpu.AddressMode;

public class Cpu
{
    public enum AddressMode
    {
        IMM,    //Immediate
        ABS,    //Absolute
        ZP,     //Zeropage
        ACC,    //Accumulator
        IMP,    //Implied
        ABS_X,  //Absolute_x
        ABS_Y,  //Absolute_y
        ZP_X,   //Zeropage_x
        ZP_Y,   //Zeropage_y
        IND,    //Indirect
        IND_X,  //Indirect_x
        IND_Y,  //Indirect_y
        REL,    //Relative
    }
    
    byte[] InstructionBytes =
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
        
    byte[] MachineCycles =
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
        
    AddressMode[] AddressingModes =
    {                    
    //  0    1      2    3  4     5     6     7  8    9      A    B  C      D      E      F   
        IMP, IND_X, 0,   0, 0,    ZP,   ZP,   0, IMP, IMM,   ACC, 0, 0,     ABS,   ABS,   0, // 0  
        REL, IND_Y, 0,   0, 0,    ZP_X, ZP_X, 0, IMP, ABS_Y, 0,   0, 0,     ABS_X, ABS_X, 0, // 1  
        ABS, IND_X, 0,   0, ZP,   ZP,   ZP,   0, IMP, IMM,   ACC, 0, ABS,   ABS,   ABS,   0, // 2  
        REL, IND_Y, 0,   0, 0,    ZP_X, ZP_X, 0, IMP, ABS_Y, 0,   0, 0,     ABS_X, ABS_X, 0, // 3  
        IMP, IND_X, 0,   0, 0,    ZP,   ZP,   0, IMP, IMM,   ACC, 0, ABS,   ABS,   ABS,   0, // 4  
        REL, IND_Y, 0,   0, 0,    ZP_X, ZP_X, 0, IMP, ABS_Y, 0,   0, 0,     ABS_X, ABS_X, 0, // 5  
        IMP, IND_X, 0,   0, 0,    ZP,   ZP,   0, IMP, IMM,   ACC, 0, IND,   ABS,   ABS,   0, // 6  
        REL, IND_Y, 0,   0, 0,    ZP_X, ZP_X, 0, IMP, ABS_Y, 0,   0, 0,     ABS_X, ABS_X, 0, // 7  
        0,   IND_X, 0,   0, ZP,   ZP,   ZP,   0, IMP, 0,     IMP, 0, ABS,   ABS,   ABS,   0, // 8  
        REL, IND_Y, 0,   0, ZP_X, ZP_X, ZP_Y, 0, IMP, ABS_Y, IMP, 0, 0,     ABS_X, 0,     0, // 9  
        IMM, IND_X, IMM, 0, ZP,   ZP,   ZP,   0, IMP, IMM,   IMP, 0, ABS,   ABS,   ABS,   0, // A  
        REL, IND_Y, 0,   0, ZP_X, ZP_X, ZP_Y, 0, IMP, ABS_Y, IMP, 0, ABS_X, ABS_X, ABS_Y, 0, // B  
        IMM, IND_X, 0,   0, ZP,   ZP,   ZP,   0, IMP, IMM,   IMP, 0, ABS,   ABS,   ABS,   0, // C  
        REL, IND_Y, 0,   0, 0,    ZP_X, ZP_X, 0, IMP, ABS_Y, 0,   0, 0,     ABS_X, ABS_X, 0, // D  
        IMM, IND_X, 0,   0, ZP,   ZP,   ZP,   0, IMP, IMM,   IMP, 0, ABS,   ABS,   ABS,   0, // E  
        REL, IND_Y, 0,   0, 0,    ZP_X, ZP_X, 0, IMP, ABS_Y, 0,   0, 0,     ABS_X, ABS_X, 0, // F
    };

    private readonly ICpuMemory _memory;
    byte A;    // Аккумулятор
    byte X;    //Индекс X 
    byte Y;    //Индекс Y
    ushort PC; // Cчетчик команд, 2 байта
    byte S;    // Указатель вершины стека
    
    // (P) Регистр статуса длиной 1 байт, разбит на 8 битов
    bool C; //Carry flag
    bool Z; // Zero flag
    bool I; // Interrpt Disable
    bool D; // Decimal Flag
    bool B; // Break command
    bool V; // Overflow flag
    bool N; // Negative flag
    
    public byte P
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
            I = ((value & 1<<2) != 0);
            D = ((value & 1<<3) != 0);
            B = ((value & 1<<4) != 0);
            V = ((value & 1<<6) != 0);
            N = ((value & 1<<7) != 0);
        }
    }

    public Cpu(ICpuMemory memory)
    {
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
}