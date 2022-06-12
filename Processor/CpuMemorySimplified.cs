using System.Collections.ObjectModel;

namespace Processor;

public class CpuMemorySimplified : ICpuMemory
{
    private List<byte> _mem;
    public ReadOnlyCollection<byte> Mem => _mem.AsReadOnly();
    public CpuMemorySimplified(string prgPath)
    {
        _mem = File.ReadAllBytes(prgPath).ToList();
    }
    public byte Read(ushort address)
    {
        return _mem[address];
    }

    public ushort Read16(ushort address)
    {
        if (address == 0xFFFC)//инициализация PC
        {
            return 0;
        }
        throw new NotImplementedException();
    }

    public ushort Read16Wrap(ushort address)
    {
        throw new NotImplementedException();
    }

    public void Write(ushort address, byte value)
    {
        _mem[address] = value;
    }

    public void Write16(ushort address, ushort value)
    {
        throw new NotImplementedException();
    }
}