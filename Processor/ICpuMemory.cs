namespace Processor;

// Stub for other team
public interface ICpuMemory
{
    byte Read(ushort address);
    ushort Read16(ushort address);
    ushort Read16Wrap(ushort address);
    void Write(ushort address, byte value);
    void Write16(ushort address, ushort value);
}