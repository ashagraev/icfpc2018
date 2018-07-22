namespace Tests
{
    using System.IO;

    using Microsoft.VisualStudio.TestTools.UnitTesting;

    using Solution;

    [TestClass]
    public class ValidationTest
    {
        [TestMethod]
        public void TestValidTrace()
        {
            LoadTrace("FA055.nbt");
        }

        [TestMethod]
        public void TestFloating()
        {
            Assert.ThrowsException<InvalidStateException>(() => LoadTrace("floating.nbt"));
        }

        [TestMethod]
        public void TestCollision()
        {
            Assert.ThrowsException<InvalidStateException>(() => LoadTrace("collision.nbt"));
        }

        [TestMethod]
        public void TestBotCollision()
        {
            Assert.ThrowsException<InvalidStateException>(() => LoadTrace("bot_collision.nbt"));
        }

        [TestMethod]
        public void TestInvalidFusion()
        {
            Assert.ThrowsException<InvalidStateException>(() => LoadTrace("invalid_fusion.nbt"));
        }

        private void LoadTrace(string traceFile)
        {
            var trace = TraceReader.Read(File.ReadAllBytes(traceFile));
            var state = new TState(new TModel("FA055_tgt.mdl"), true);
            var reader = new TCommandsReader(trace);
            while (!reader.AtEnd())
            {
                state.Step(reader);
            }
        }
    }
}