namespace Processor;

public class Cpu
{
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