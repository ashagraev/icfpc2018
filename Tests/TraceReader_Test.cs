namespace Tests
{
    using System.Threading.Tasks.Sources;

    using Microsoft.VisualStudio.TestPlatform.ObjectModel;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    using Solution;

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
            var commands = TraceReader.Read(
                new byte[]
                    {
                        0b00010100,
                        0b00011011,
                        0b00110100,
                        0b00001011
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
            var commands = TraceReader.Read(
                new byte[]
                    {
                        0b10011100,
                        0b00001000,
                        0b11101100,
                        0b01110011
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

        [TestMethod]
        public void TestNearCommands()
        {
            var commands = TraceReader.Read(new byte[]
            {
                0b00111111,
                0b10011110,
                0b01110101,
                0b00000101,
                0b01010011,
                0b10111010,
            });
            Assert.AreEqual(5, commands.Count);
            var fp = commands[0] as FusionP;
            Assert.AreEqual(-1, fp.Diff.Dx);
            Assert.AreEqual(1, fp.Diff.Dy);
            Assert.AreEqual(0, fp.Diff.Dz);
            var fs = commands[1] as FusionS;
            Assert.AreEqual(1, fs.Diff.Dx);
            Assert.AreEqual(-1, fs.Diff.Dy);
            Assert.AreEqual(0, fs.Diff.Dz);
            var fss = commands[2] as Fission;
            Assert.AreEqual(0, fss.Diff.Dx);
            Assert.AreEqual(0, fss.Diff.Dy);
            Assert.AreEqual(1, fss.Diff.Dz);
            var fill = commands[3] as Fill;
            Assert.AreEqual(0, fill.Diff.Dx);
            Assert.AreEqual(-1, fill.Diff.Dy);
            Assert.AreEqual(0, fill.Diff.Dz);
            var @void = commands[4] as Void;
            Assert.AreEqual(1, @void.Diff.Dx);
            Assert.AreEqual(0, @void.Diff.Dy);
            Assert.AreEqual(1, @void.Diff.Dz);
        }
    }
}