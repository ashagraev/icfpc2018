namespace Solution
{
    using System;
    using System.IO;

    using Solution.Strategies;

    internal class Program
    {
        private static void Main(string[] args)
        {
            var modelName = "FA010";
            var model = new TModel($"Data/Problems/{modelName}_tgt.mdl");

            void TestStrategy(IStrategy strategy, bool saveTrace = false)
            {
                var trace = strategy.MakeTrace(model);
                var state = new TState(model);
                var reader = new TCommandsReader(trace);
                while (!reader.AtEnd())
                {
                    state.Step(reader);
                }

                Console.WriteLine($"=== {strategy.Name}");
                Console.WriteLine(state.HasValidFinalState());
                Console.WriteLine(state.Energy);

                if (saveTrace)
                {
                    File.WriteAllBytes("trace", TraceSerializer.Serialize(trace));
                }
            }

            //TestStrategy(new DumpCubeStrategy());
            TestStrategy(new BetterCubeStrategy());
            //TestStrategy(new TTraceReaderStrategy($"data/DefaultTraces"));
            TestStrategy(new BfsStrategy(), true);
        }
    }
}