namespace Processor;
using NUnit.Framework;
public class ProcessorTests
{
    [SetUp]
    public void Setup()
    {
        
    }

    [Test]
    public void Test1()
    {
        CpuMemorySimplified memory = new CpuMemorySimplified(AppDomain.CurrentDomain.BaseDirectory+"2plus2.txt");//A9 02 69 02 85 03
        Cpu cpu = new Cpu(memory);
        cpu.Step();
        cpu.Step();
        cpu.Step();
        Assert.AreEqual(memory.Mem[6],0x04);
    }
    [Test]
    public void Test2()
    {
        //источник: https://www.lysator.liu.se/~nisse/misc/6502-mul.html#:~:text=Since%206502%20lacks%20any%20multiplication,if%20I%20count%20it%20correctly.
        //два множителя 10 и 10 записаны в ячейках 0x30 и 0x31
        CpuMemorySimplified memory = new CpuMemorySimplified(AppDomain.CurrentDomain.BaseDirectory+"multiply.bin");
        Console.WriteLine(AppDomain.CurrentDomain.BaseDirectory+"multiply.bin");
        Cpu cpu = new Cpu(memory);
        for (int i = 0; i < 75; i++)
        {
            cpu.Step();
        }
        Assert.AreEqual(memory.Mem[0x30],100);
    }
}