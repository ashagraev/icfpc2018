namespace Tests
{
    using System;
    using System.IO;

    using Microsoft.VisualStudio.TestTools.UnitTesting;

    using Solution;

    [TestClass]
    public class TraceSerializer_Test
    {
        [TestMethod]
        public void TestRoundtrip()
        {
            var inBytes = File.ReadAllBytes("LA008.nbt");
            var commands = TraceReader.Read(inBytes);
            var outBytes = TraceSerializer.Serialize(commands);
            Assert.AreEqual(inBytes.Length, outBytes.Length);
            for (var i = 0; i < inBytes.Length; i++)
            {
                Assert.AreEqual(inBytes[i], outBytes[i], string.Format("byte #{0}", i));
            }
        }

        [TestMethod]
        public void TestInvalidCommands()
        {
            S(SM(1, 0, 0));
            S(SM(0, -13, 0));
            S(SM(0, 0, 15));
            Assert.ThrowsException<ArgumentOutOfRangeException>(() => S(SM(1, 1, 0)));
            Assert.ThrowsException<ArgumentOutOfRangeException>(() => S(SM(-16, 0, 0)));
            Assert.ThrowsException<ArgumentOutOfRangeException>(() => S(SM(0, 16, 0)));
            Assert.ThrowsException<ArgumentOutOfRangeException>(() => S(SM(-1, 0, 1)));

            S(LM(1, 0, 0, 0, -5, 0));
            S(LM(0, 3, 0, 0, -5, 0));
            S(LM(5, 0, 0, 0, 0, 5));

            Assert.ThrowsException<ArgumentOutOfRangeException>(() => S(LM(1, 1, 0, 1, 0, 0)));
            Assert.ThrowsException<ArgumentOutOfRangeException>(() => S(LM(-6, 0, 0, 0, 1, 0)));

            S(F(0, -1, 0));
            S(F(0, 0, 1));
            S(F(1, -1, 0));
            S(F(-1, 1, 0));

            Assert.ThrowsException<ArgumentOutOfRangeException>(() => S(F(1, 1, 1)));
            Assert.ThrowsException<ArgumentOutOfRangeException>(() => S(F(2, 1, 0)));
            Assert.ThrowsException<ArgumentOutOfRangeException>(() => S(F(0, -2, 0)));
            Assert.ThrowsException<ArgumentOutOfRangeException>(() => S(F(0, 0, 0)));
        }

        private byte[] S(ICommand cmd)
        {
            return TraceSerializer.Serialize(new[] { cmd });
        }

        private ICommand SM(int dx, int dy, int dz)
        {
            return new StraightMove
            {
                Diff = new CoordDiff
                {
                    Dx = dx,
                    Dy = dy,
                    Dz = dz,
                }
            };
        }

        private ICommand LM(int dx1, int dy1, int dz1, int dx2, int dy2, int dz2)
        {
            return new LMove
            {
                Diff1 = new CoordDiff
                {
                    Dx = dx1,
                    Dy = dy1,
                    Dz = dz1,
                },
                Diff2 = new CoordDiff
                {
                    Dx = dx2,
                    Dy = dy2,
                    Dz = dz2,
                }
            };
        }

        private ICommand F(int dx, int dy, int dz)
        {
            return new Fill
            {
                Diff = new CoordDiff
                {
                    Dx = dx,
                    Dy = dy,
                    Dz = dz,
                }
            };
        }
    }
}