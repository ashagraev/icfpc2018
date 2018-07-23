namespace Solution
{
    using System;
    using System.IO;

    using Solution.Strategies;

    internal class Program
    {
        private static void Main(string[] args)
        {
            var modelName = "FR008";

            var srcModel = new TModel($"Data/Problems/{modelName}_src.mdl");
            var tgtModel = new TModel($"Data/Problems/{modelName}_tgt.mdl");

            void TestStrategy(IStrategy strategy, bool saveTrace = false)
            {
                Console.WriteLine($"=== {strategy.Name}");

                var trace = strategy.MakeReassemblyTrace(srcModel, tgtModel);
                var state = new TState(srcModel);
                var reader = new TCommandsReader(trace);
                while (!reader.AtEnd())
                {
                    state.Step(reader);
                }

                Console.WriteLine(state.HasValidFinalState(tgtModel));
                Console.WriteLine(state.Energy);

                if (saveTrace)
                {
                    File.WriteAllBytes("trace.nbt", TraceSerializer.Serialize(trace));
                }
            }

            //TestStrategy(new DumpCubeStrategy());
            TestStrategy(new DumpCubeStrategy(), true);
            TestStrategy(new TTraceReaderStrategy($"data/DefaultTraces"));
            //TestStrategy(new BfsStrategy(), true);
        }
    }
}