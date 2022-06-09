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
        CpuMemorySimplified memory = new CpuMemorySimplified("../TestFiles/2plus2.txt");//A9 02 69 02 85 03
        Cpu cpu = new Cpu(memory);
        cpu.Step();
        cpu.Step();
        cpu.Step();
        Assert.AreEqual(memory.Mem[6],0x04);
    }
}