using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sample;

namespace Tests
{
    [TestClass]
    public class TraceReaderTest
    {
        [TestMethod]
        public void TestSimpleCommands()
        {
            var commands = TraceReader.Read(new byte[] { 0b1111_1111, 0b1111_1110, 0b1111_1101 });
            Assert.AreEqual(3, commands.Count);
            Assert.IsTrue(commands[0] is Halt);
            Assert.IsTrue(commands[1] is Wait);
            Assert.IsTrue(commands[2] is Flip);
        }

        [TestMethod]
        public void TestStraightMove()
        {
            var commands = TraceReader.Read(new byte[] {
                0b00010100, 0b00011011, 0b00110100, 0b00001011,
            });
            Assert.AreEqual(2, commands.Count);
            var sm1 = commands[0] as StraightMove;
            Assert.AreEqual(12, sm1.Diff.Dx);
            Assert.AreEqual(0, sm1.Diff.Dy);
            Assert.AreEqual(0, sm1.Diff.Dz);
            var sm2 = commands[1] as StraightMove;
            Assert.AreEqual(0, sm2.Diff.Dx);
            Assert.AreEqual(0, sm2.Diff.Dy);
            Assert.AreEqual(-4, sm2.Diff.Dz);
        }

        [TestMethod]
        public void TestLMove()
        {
            var commands = TraceReader.Read(new byte[]
            {
                0b10011100, 0b00001000, 0b11101100, 0b01110011
            });
            Assert.AreEqual(2, commands.Count);
            var lm1 = commands[0] as LMove;
            Assert.AreEqual(3, lm1.Diff1.Dx);
            Assert.AreEqual(0, lm1.Diff1.Dy);
            Assert.AreEqual(0, lm1.Diff1.Dz);
            Assert.AreEqual(0, lm1.Diff2.Dx);
            Assert.AreEqual(-5, lm1.Diff2.Dy);
            Assert.AreEqual(0, lm1.Diff2.Dz);
            var lm2 = commands[1] as LMove;
            Assert.AreEqual(0, lm2.Diff1.Dx);
            Assert.AreEqual(-2, lm2.Diff1.Dy);
            Assert.AreEqual(0, lm2.Diff1.Dz);
            Assert.AreEqual(0, lm2.Diff2.Dx);
            Assert.AreEqual(0, lm2.Diff2.Dy);
            Assert.AreEqual(2, lm2.Diff2.Dz);
        }
    }
}