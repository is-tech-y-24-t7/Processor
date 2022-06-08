namespace Processor;

public class Cpu
{
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
    
    public byte SR
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
            // Extract the corresponding bits for each weekday
            // into bool properties of the class
            C = (value & 1<<0 ) != 0 ;
            Z = (value & 1<<1) != 0;
            I = ((value & 1<<2) != 0);
            D = ((value & 1<<3) != 0);
            B = ((value & 1<<4) != 0);
            V = ((value & 1<<6) != 0);
            N = ((value & 1<<7) != 0);
        }
    }
}