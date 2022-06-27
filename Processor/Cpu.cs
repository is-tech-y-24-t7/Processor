using System;
using MemoryService;
using Console = System.Console;

namespace Processor
{
    public class Cpu
    {
        enum AddressMode
        {
            Absolute = 1, // 1
            AbsoluteX, // 2
            AbsoluteY, // 3
            Accumulator, // 4
            Immediate, // 5
            Implied, // 6
            IndexedIndirect, // 7
            Indirect, // 8
            IndirectIndexed, // 9
            Relative, // 10
            ZeroPage, // 11
            ZeroPageX, // 12
            ZeroPageY // 13
        };

        private readonly CpuMemory _memory;
        private byte A;    // Аккумулятор
        private byte X;    // Индекс X 
        private byte Y;    // Индекс Y
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

        public int _cycles { get; private set; }
        int _idle;

        private bool irqInterrupt;
        private bool nmiInterrupt;

        delegate void Instruction(AddressMode mode, ushort address);
        Instruction[] _instructions;
        
        public Cpu(MemoryService.Console console)
        {
            _memory = console.CpuMemory;
            _instructions = InitInstructions();
        }

        public void Reset()
        {
            PC = _memory.Read16(0xFFFC);
            S = 0xFD;
            A = 0;
            X = 0;
            Y = 0;
            SetProcessorFlags((byte)0x24);

            _cycles = 0;
            _idle = 0;

            nmiInterrupt = false;
        }
        
        public void TriggerNmi()
        {
            nmiInterrupt = true;
        }
        
        public void TriggerIrq()
        {
            if (!I) irqInterrupt = true;
        }
        
        public void AddIdleCycles(int idleCycles)
        {
            _idle += idleCycles;
        }
        
        public int Step()
        {
            if (_idle > 0)
            {
                _idle--;
                return 1;
            }

            if (irqInterrupt) Irq();
            irqInterrupt = false;

            if (nmiInterrupt) Nmi();
            nmiInterrupt = false;

            int cyclesOrig = _cycles;
            byte opCode = _memory.Read(PC);

            AddressMode mode = (AddressMode)_addressModes[opCode];
            
            ushort address = 0;
            bool pageCrossed = false;
            switch (mode)
            {
                case AddressMode.Implied:
                    break;
                case AddressMode.Immediate:
                    address = (ushort)(PC + 1);
                    break;
                case AddressMode.Absolute:
                    address = _memory.Read16((ushort)(PC + 1));
                    break;
                case AddressMode.AbsoluteX:
                    address = (ushort)(_memory.Read16((ushort)(PC + 1)) + X);
                    pageCrossed = IsPageCross((ushort)(address - X), (ushort)X);
                    break;
                case AddressMode.AbsoluteY:
                    address = (ushort)(_memory.Read16((ushort)(PC + 1)) + Y);
                    pageCrossed = IsPageCross((ushort)(address - Y), (ushort)Y);
                    break;
                case AddressMode.Accumulator:
                    break;
                case AddressMode.Relative:
                    address = (ushort)(PC + (sbyte)_memory.Read((ushort)(PC + 1)) + 2);
                    break;
                case AddressMode.ZeroPage:
                    address = _memory.Read((ushort)(PC + 1));
                    break;
                case AddressMode.ZeroPageY:
                    address = (ushort)((_memory.Read((ushort)(PC + 1)) + Y) & 0xFF);
                    break;
                case AddressMode.ZeroPageX:
                    address = (ushort)((_memory.Read((ushort)(PC + 1)) + X) & 0xFF);
                    break;
                case AddressMode.Indirect:
                    address = (ushort)_memory.Read16WrapPage((ushort)_memory.Read16((ushort)(PC + 1)));
                    break;
                case AddressMode.IndexedIndirect:
                    ushort lowerNibbleAddress = (ushort)((_memory.Read((ushort)(PC + 1)) + X) & 0xFF);
                    address = (ushort)_memory.Read16WrapPage((ushort)(lowerNibbleAddress));
                    break;
                case AddressMode.IndirectIndexed:
                    ushort valueAddress = (ushort)_memory.Read((ushort)(PC + 1));
                    address = (ushort)(_memory.Read16WrapPage(valueAddress) + Y);
                    pageCrossed = IsPageCross((ushort)(address - Y), address);
                    break;
            }

            PC += (ushort)_instructionSizes[opCode];
            _cycles += _instructionCycles[opCode];

            if (pageCrossed) _cycles += _instructionPageCycles[opCode];
            _instructions[opCode](mode, address);

            return _cycles - cyclesOrig;
        }

        void SetZn(byte value)
        {
            Z = value == 0;
            N = ((value >> 7) & 1) == 1;
        }

        bool IsBitSet(byte value, int index)
        {
            return (value & (1 << index)) != 0;
        }

        byte PullStack()
        {
            S++;
            byte data = _memory.Read((ushort)(0x0100 | S));
            return data;
        }

        void PushStack(byte data)
        {
            _memory.Write((ushort)(0x100 | S), data);
            S--;
        }

        ushort PullStack16()
        {
            byte lo = PullStack();
            byte hi = PullStack();
            return (ushort)((hi << 8) | lo);
        }

        void PushStack16(ushort data)
        {
            byte lo = (byte)(data & 0xFF);
            byte hi = (byte)((data >> 8) & 0xFF);

            PushStack(hi);
            PushStack(lo);
        }

        byte GetStatusFlags()
        {
            byte flags = 0;

            if (C) flags |= (byte)(1 << 0);
            if (Z) flags |= (byte)(1 << 1);
            if (I) flags |= (byte)(1 << 2);
            if (D) flags |= (byte) (1 << 3);
            if (B) flags |= (byte)(1 << 4);
            flags |= (byte)(1 << 5);
            if (V) flags |= (byte)(1 << 6);
            if (N) flags |= (byte)(1 << 7);

            return flags;
        }

        void SetProcessorFlags(byte flags)
        {
            C = IsBitSet(flags, 0);
            Z = IsBitSet(flags, 1);
            I = IsBitSet(flags, 2);
            D = IsBitSet(flags, 3);
            B = IsBitSet(flags, 4);
            V = IsBitSet(flags, 6);
            N = IsBitSet(flags, 7);
        }

        bool IsPageCross(ushort a, ushort b)
        {
            return (a & 0xFF) != (b & 0xFF);
        }

        void HandleBranchCycles(ushort origPc, ushort branchPc)
        {
            _cycles++;
            _cycles += IsPageCross(origPc, branchPc) ? 1 : 0;
        }

        void Nmi()
        {
            PushStack16(PC);
            PushStack(GetStatusFlags());
            PC = _memory.Read16(0xFFFA);
            I = true;
        }

        void Irq()
        {
            PushStack16(PC);
            PushStack(GetStatusFlags());
            PC = _memory.Read16(0xFFFE);
            I = true;
        }
        
        void xxx(AddressMode mode, ushort address)
        {
            throw new Exception("Illegal Opcode");
        }
        
        void brk(AddressMode mode, ushort address)
        {
            PushStack16(PC);
            PushStack(GetStatusFlags());
            B = true;
            PC = _memory.Read16((ushort)0xFFFE);
        }
        
        void ror(AddressMode mode, ushort address)
        {
            bool Corig = C;
            if (mode == AddressMode.Accumulator)
            {
                C = IsBitSet(A, 0);
                A >>= 1;
                A |= (byte)(Corig ? 0x80 : 0);

                SetZn(A);
            }
            else
            {
                byte data = _memory.Read(address);
                C = IsBitSet(data, 0);

                data >>= 1;
                data |= (byte)(Corig ? 0x80 : 0);

                _memory.Write(address, data);

                SetZn(data);
            }
        }
        
        void rti(AddressMode mode, ushort address)
        {
            SetProcessorFlags(PullStack());
            PC = PullStack16();
        }
        
        void txs(AddressMode mode, ushort address)
        {
            S = X;
        }
        
        void tsx(AddressMode mode, ushort address)
        {
            X = S;
            SetZn(X);
        }
        
        void txa(AddressMode mode, ushort address)
        {
            A = X;
            SetZn(A);
        }
        
        void tya(AddressMode mode, ushort address)
        {
            A = Y;
            SetZn(A);
        }
        
        void tay(AddressMode mode, ushort address)
        {
            Y = A;
            SetZn(Y);
        }
        
        void tax(AddressMode mode, ushort address)
        {
            X = A;
            SetZn(X);
        }
        
        void dex(AddressMode mode, ushort address)
        {
            X--;
            SetZn(X);
        }
        
        void dey(AddressMode mode, ushort address)
        {
            Y--;
            SetZn(Y);
        }
        
        void inx(AddressMode mode, ushort address)
        {
            X++;
            SetZn(X);
        }
        
        void iny(AddressMode mode, ushort address)
        {
            Y++;
            SetZn(Y);
        }
        
        void sty(AddressMode mode, ushort address)
        {
            _memory.Write(address, Y);
        }
        
        void cpx(AddressMode mode, ushort address)
        {
            byte data = _memory.Read(address);
            SetZn((byte)(X - data));
            C = X >= data;
        }
        
        void cpy(AddressMode mode, ushort address)
        {
            byte data = _memory.Read(address);
            SetZn((byte)(Y - data));
            C = Y >= data;
        }
        
        void sbc(AddressMode mode, ushort address)
        {
            byte data = _memory.Read(address);
            int notCarry = (!C ? 1 : 0);

            byte result = (byte)(A - data - notCarry);
            SetZn(result);

            // If an overflow occurs (result actually less than 0)
            // the carry flag is cleared
            C = (A - data - notCarry) >= 0 ? true : false;

            V = ((A ^ data) & (A ^ result) & 0x80) != 0;

            A = result;
        }
        
        void adc(AddressMode mode, ushort address)
        {
            byte data = _memory.Read(address);
            int carry = (C ? 1 : 0);

            byte sum = (byte)(A + data + carry);
            SetZn(sum);

            C = (A + data + carry) > 0xFF;
            V = (~(A ^ data) & (A ^ sum) & 0x80) != 0;

            A = sum;
        }
        
        void eor(AddressMode mode, ushort address)
        {
            byte data = _memory.Read(address);
            A ^= data;
            SetZn(A);
        }

        void clv(AddressMode mode, ushort address)
        {
            V = false;
        }
        
        void bmi(AddressMode mode, ushort address)
        {
            PC = N ? address : PC;
        }
        
        void plp(AddressMode mode, ushort address)
        {
            SetProcessorFlags((byte)(PullStack() & ~(0x10)));
        }
        
        void cld(AddressMode mode, ushort address)
        {
            D = false;
        }
        
        void cmp(AddressMode mode, ushort address)
        {
            byte data = _memory.Read(address);
            C = A >= data;
            SetZn((byte)(A - data));
        }
        
        void and(AddressMode mode, ushort address)
        {
            byte data = _memory.Read(address);
            A &= data;
            SetZn(A);
        }
        
        void pla(AddressMode mode, ushort address)
        {
            A = PullStack();
            SetZn(A);
        }
        
        void php(AddressMode mode, ushort address)
        {
            PushStack((byte)(GetStatusFlags() | 0x10));
        }
        
        void sed(AddressMode mode, ushort address)
        {
            D = true;
        }
        
        void cli(AddressMode mode, ushort address)
        {
            I = false;
        }
        
        void sei(AddressMode mode, ushort address)
        {
            I = true;
        }
        
        void dec(AddressMode mode, ushort address)
        {
            byte data = _memory.Read(address);
            data--;
            _memory.Write(address, data);
            SetZn(data);
        }
        
        void inc(AddressMode mode, ushort address)
        {
            byte data = _memory.Read(address);
            data++;
            _memory.Write(address, data);
            SetZn(data);
        }
        
        void rts(AddressMode mode, ushort address)
        {
            PC = (ushort)(PullStack16() + 1);
        }
        
        void jsr(AddressMode mode, ushort address)
        {
            PushStack16((ushort)(PC - 1));
            PC = address;
        }
        
        void bpl(AddressMode mode, ushort address)
        {
            if (!N)
            {
                HandleBranchCycles(PC, address);
                PC = address;
            }
        }
        
        void bvc(AddressMode mode, ushort address)
        {
            if (!V)
            {
                HandleBranchCycles(PC, address);
                PC = address;
            }
        }
        
        void bvs(AddressMode mode, ushort address)
        {
            if (V)
            {
                HandleBranchCycles(PC, address);
                PC = address;
            }
        }
        
        void bit(AddressMode mode, ushort address)
        {
            byte data = _memory.Read(address);
            N = IsBitSet(data, 7);
            V = IsBitSet(data, 6);
            Z = (data & A) == 0;
        }
        
        void bne(AddressMode mode, ushort address)
        {
            if (!Z)
            {
                HandleBranchCycles(PC, address);
                PC = address;
            }
        }
        
        void beq(AddressMode mode, ushort address)
        {
            if (Z)
            {
                HandleBranchCycles(PC, address);
                PC = address;
            }
        }
        
        void clc(AddressMode mode, ushort address)
        {
            C = false;
        }
        
        void bcc(AddressMode mode, ushort address)
        {
            if (!C)
            {
                HandleBranchCycles(PC, address);
                PC = address;
            }
        }
        
        void bcs(AddressMode mode, ushort address)
        {
            if (C)
            {
                HandleBranchCycles(PC, address);
                PC = address;
            }
        }
        
        void sec(AddressMode mode, ushort address)
        {
            C = true;
        }
        
        void nop(AddressMode mode, ushort address)
        {

        }
        
        void stx(AddressMode mode, ushort address)
        {
            _memory.Write(address, X);
        }
        
        void ldy(AddressMode mode, ushort address)
        {
            Y = _memory.Read(address);
            SetZn(Y);
        }
        
        void ldx(AddressMode mode, ushort address)
        {
            X = _memory.Read(address);
            SetZn(X);
        }
        
        void jmp(AddressMode mode, ushort address)
        {
            PC = address;
        }
        
        void sta(AddressMode mode, ushort address)
        {
            _memory.Write(address, A);
        }
        
        void ora(AddressMode mode, ushort address)
        {
            A |= _memory.Read(address);
            SetZn(A);
        }
        
        void lda(AddressMode mode, ushort address)
        {
            A = _memory.Read(address);
            SetZn(A);
        }
        
        void pha(AddressMode mode, ushort address)
        {
            PushStack(A);
        }

        void asl(AddressMode mode, ushort address)
        {
            if (mode == AddressMode.Accumulator)
            {
                C = IsBitSet(A, 7);
                A <<= 1;
                SetZn(A);
            }
            else
            {
                byte data = _memory.Read(address);
                C = IsBitSet(data, 7);
                byte dataUpdated = (byte)(data << 1);
                _memory.Write(address, dataUpdated);
                SetZn(dataUpdated);
            }
        }
        
        void rol(AddressMode mode, ushort address)
        {
            bool Corig = C;
            if (mode == AddressMode.Accumulator)
            {
                C = IsBitSet(A, 7);
                A <<= 1;
                A |= (byte)(Corig ? 1 : 0);

                SetZn(A);
            }
            else
            {
                byte data = _memory.Read(address);
                C = IsBitSet(data, 7);

                data <<= 1;
                data |= (byte)(Corig ? 1 : 0);

                _memory.Write(address, data);

                SetZn(data);
            }
        }
        
        void lsr(AddressMode mode, ushort address)
        {
            if (mode == AddressMode.Accumulator)
            {
                C = (A & 1) == 1;
                A >>= 1;

                SetZn(A);
            }
            else
            {
                byte value = _memory.Read(address);
                C = (value & 1) == 1;

                byte updatedValue = (byte)(value >> 1);

                _memory.Write(address, updatedValue);

                SetZn(updatedValue);
            }
        }

        int[] _addressModes = {
            6, 7, 6, 7, 11, 11, 11, 11, 6, 5, 4, 5, 1, 1, 1, 1,
            10, 9, 6, 9, 12, 12, 12, 12, 6, 3, 6, 3, 2, 2, 2, 2,
            1, 7, 6, 7, 11, 11, 11, 11, 6, 5, 4, 5, 1, 1, 1, 1,
            10, 9, 6, 9, 12, 12, 12, 12, 6, 3, 6, 3, 2, 2, 2, 2,
            6, 7, 6, 7, 11, 11, 11, 11, 6, 5, 4, 5, 1, 1, 1, 1,
            10, 9, 6, 9, 12, 12, 12, 12, 6, 3, 6, 3, 2, 2, 2, 2,
            6, 7, 6, 7, 11, 11, 11, 11, 6, 5, 4, 5, 8, 1, 1, 1,
            10, 9, 6, 9, 12, 12, 12, 12, 6, 3, 6, 3, 2, 2, 2, 2,
            5, 7, 5, 7, 11, 11, 11, 11, 6, 5, 6, 5, 1, 1, 1, 1,
            10, 9, 6, 9, 12, 12, 13, 13, 6, 3, 6, 3, 2, 2, 3, 3,
            5, 7, 5, 7, 11, 11, 11, 11, 6, 5, 6, 5, 1, 1, 1, 1,
            10, 9, 6, 9, 12, 12, 13, 13, 6, 3, 6, 3, 2, 2, 3, 3,
            5, 7, 5, 7, 11, 11, 11, 11, 6, 5, 6, 5, 1, 1, 1, 1,
            10, 9, 6, 9, 12, 12, 12, 12, 6, 3, 6, 3, 2, 2, 2, 2,
            5, 7, 5, 7, 11, 11, 11, 11, 6, 5, 6, 5, 1, 1, 1, 1,
            10, 9, 6, 9, 12, 12, 12, 12, 6, 3, 6, 3, 2, 2, 2, 2,
        };

        int[] _instructionSizes = {
            1, 2, 0, 0, 2, 2, 2, 0, 1, 2, 1, 0, 3, 3, 3, 0,
            2, 2, 0, 0, 2, 2, 2, 0, 1, 3, 1, 0, 3, 3, 3, 0,
            3, 2, 0, 0, 2, 2, 2, 0, 1, 2, 1, 0, 3, 3, 3, 0,
            2, 2, 0, 0, 2, 2, 2, 0, 1, 3, 1, 0, 3, 3, 3, 0,
            1, 2, 0, 0, 2, 2, 2, 0, 1, 2, 1, 0, 3, 3, 3, 0,
            2, 2, 0, 0, 2, 2, 2, 0, 1, 3, 1, 0, 3, 3, 3, 0,
            1, 2, 0, 0, 2, 2, 2, 0, 1, 2, 1, 0, 3, 3, 3, 0,
            2, 2, 0, 0, 2, 2, 2, 0, 1, 3, 1, 0, 3, 3, 3, 0,
            2, 2, 0, 0, 2, 2, 2, 0, 1, 0, 1, 0, 3, 3, 3, 0,
            2, 2, 0, 0, 2, 2, 2, 0, 1, 3, 1, 0, 0, 3, 0, 0,
            2, 2, 2, 0, 2, 2, 2, 0, 1, 2, 1, 0, 3, 3, 3, 0,
            2, 2, 0, 0, 2, 2, 2, 0, 1, 3, 1, 0, 3, 3, 3, 0,
            2, 2, 0, 0, 2, 2, 2, 0, 1, 2, 1, 0, 3, 3, 3, 0,
            2, 2, 0, 0, 2, 2, 2, 0, 1, 3, 1, 0, 3, 3, 3, 0,
            2, 2, 0, 0, 2, 2, 2, 0, 1, 2, 1, 0, 3, 3, 3, 0,
            2, 2, 0, 0, 2, 2, 2, 0, 1, 3, 1, 0, 3, 3, 3, 0,
        };

        int[] _instructionCycles = {
            7, 6, 2, 8, 3, 3, 5, 5, 3, 2, 2, 2, 4, 4, 6, 6,
            2, 5, 2, 8, 4, 4, 6, 6, 2, 4, 2, 7, 4, 4, 7, 7,
            6, 6, 2, 8, 3, 3, 5, 5, 4, 2, 2, 2, 4, 4, 6, 6,
            2, 5, 2, 8, 4, 4, 6, 6, 2, 4, 2, 7, 4, 4, 7, 7,
            6, 6, 2, 8, 3, 3, 5, 5, 3, 2, 2, 2, 3, 4, 6, 6,
            2, 5, 2, 8, 4, 4, 6, 6, 2, 4, 2, 7, 4, 4, 7, 7,
            6, 6, 2, 8, 3, 3, 5, 5, 4, 2, 2, 2, 5, 4, 6, 6,
            2, 5, 2, 8, 4, 4, 6, 6, 2, 4, 2, 7, 4, 4, 7, 7,
            2, 6, 2, 6, 3, 3, 3, 3, 2, 2, 2, 2, 4, 4, 4, 4,
            2, 6, 2, 6, 4, 4, 4, 4, 2, 5, 2, 5, 5, 5, 5, 5,
            2, 6, 2, 6, 3, 3, 3, 3, 2, 2, 2, 2, 4, 4, 4, 4,
            2, 5, 2, 5, 4, 4, 4, 4, 2, 4, 2, 4, 4, 4, 4, 4,
            2, 6, 2, 8, 3, 3, 5, 5, 2, 2, 2, 2, 4, 4, 6, 6,
            2, 5, 2, 8, 4, 4, 6, 6, 2, 4, 2, 7, 4, 4, 7, 7,
            2, 6, 2, 8, 3, 3, 5, 5, 2, 2, 2, 2, 4, 4, 6, 6,
            2, 5, 2, 8, 4, 4, 6, 6, 2, 4, 2, 7, 4, 4, 7, 7,
        };

        int[] _instructionPageCycles = {
            0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
            1, 1, 0, 0, 0, 0, 0, 0, 0, 1, 0, 0, 1, 1, 0, 0,
            0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
            1, 1, 0, 0, 0, 0, 0, 0, 0, 1, 0, 0, 1, 1, 0, 0,
            0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
            1, 1, 0, 0, 0, 0, 0, 0, 0, 1, 0, 0, 1, 1, 0, 0,
            0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
            1, 1, 0, 0, 0, 0, 0, 0, 0, 1, 0, 0, 1, 1, 0, 0,
            0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
            1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
            0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
            1, 1, 0, 1, 0, 0, 0, 0, 0, 1, 0, 1, 1, 1, 1, 1,
            0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
            1, 1, 0, 0, 0, 0, 0, 0, 0, 1, 0, 0, 1, 1, 0, 0,
            0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
            1, 1, 0, 0, 0, 0, 0, 0, 0, 1, 0, 0, 1, 1, 0, 0,
        };

        private Instruction[] InitInstructions() => new Instruction[]
        {
            //  0    1    2    3    4    5    6    7    8    9    A    B    C    D    E    F
            brk, ora, xxx, xxx, xxx, ora, asl, xxx, php, ora, asl, xxx, xxx, ora, asl, xxx, // 0
            bpl, ora, xxx, xxx, xxx, ora, asl, xxx, clc, ora, xxx, xxx, xxx, ora, asl, xxx, // 1
            jsr, and, xxx, xxx, bit, and, rol, xxx, plp, and, rol, xxx, bit, and, rol, xxx, // 2
            bmi, and, xxx, xxx, xxx, and, rol, xxx, sec, and, xxx, xxx, xxx, and, rol, xxx, // 3
            rti, eor, xxx, xxx, xxx, eor, lsr, xxx, pha, eor, lsr, xxx, jmp, eor, lsr, xxx, // 4
            bvc, eor, xxx, xxx, xxx, eor, lsr, xxx, cli, eor, xxx, xxx, xxx, eor, lsr, xxx, // 5
            rts, adc, xxx, xxx, xxx, adc, ror, xxx, pla, adc, ror, xxx, jmp, adc, ror, xxx, // 6
            bvs, adc, xxx, xxx, xxx, adc, ror, xxx, sei, adc, xxx, xxx, xxx, adc, ror, xxx, // 7
            xxx, sta, xxx, xxx, sty, sta, stx, xxx, dey, xxx, txa, xxx, sty, sta, stx, xxx, // 8
            bcc, sta, xxx, xxx, sty, sta, stx, xxx, tya, sta, txs, xxx, xxx, sta, xxx, xxx, // 9
            ldy, lda, ldx, xxx, ldy, lda, ldx, xxx, tay, lda, tax, xxx, ldy, lda, ldx, xxx, // A
            bcs, lda, xxx, xxx, ldy, lda, ldx, xxx, clv, lda, tsx, xxx, ldy, lda, ldx, xxx, // B
            cpy, cmp, xxx, xxx, cpy, cmp, dec, xxx, iny, cmp, dex, xxx, cpy, cmp, dec, xxx, // C
            bne, cmp, xxx, xxx, xxx, cmp, dec, xxx, cld, cmp, xxx, xxx, xxx, cmp, dec, xxx, // D
            cpx, sbc, xxx, xxx, cpx, sbc, inc, xxx, inx, sbc, nop, xxx, cpx, sbc, inc, xxx, // E
            beq, sbc, xxx, xxx, xxx, sbc, inc, xxx, sed, sbc, xxx, xxx, xxx, sbc, inc, xxx  // F
        };
    }
}