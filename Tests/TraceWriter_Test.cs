namespace Tests
{
    using System;
    using System.IO;

    using Microsoft.VisualStudio.TestTools.UnitTesting;

    using Solution;

    [TestClass]
    public class TraceWriter_Test
    {
        [TestMethod]
        public void TestRoundtrip()
        {
            var inBytes = File.ReadAllBytes("LA008.nbt");
            var commands = TraceReader.Read(inBytes);
            var outBytes = TraceWriter.Write(commands);
            Assert.AreEqual(inBytes.Length, outBytes.Length);
            for (var i = 0; i < inBytes.Length; i++)
            {
                Assert.AreEqual(inBytes[i], outBytes[i], string.Format("byte #{0}", i));
            }
        }
    }
}