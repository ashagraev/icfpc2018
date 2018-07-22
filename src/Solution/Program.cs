namespace Solution
{
    using System;
    using System.IO;

    using Solution.Strategies;

    internal class Program
    {
        private static void Main(string[] args)
        {
            var modelName = "FD151";
            var suffix = modelName.Contains("FA") ? "_tgt" : "src";
            var model = new TModel($"Data/Problems/{modelName}_{suffix}.mdl");

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
                    File.WriteAllBytes("C://GitHub//icfpc2018//trace1", TraceSerializer.Serialize(trace));
                }
            }

            //TestStrategy(new DumpCubeStrategy());
            TestStrategy(new DumpCubeStrategy(), true);
            TestStrategy(new TTraceReaderStrategy($"data/DefaultTraces"));
            //TestStrategy(new BfsStrategy(), true);
        }
    }
}