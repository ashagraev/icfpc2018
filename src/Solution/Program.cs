namespace Solution
{
    using System;
    using System.IO;

    using Solution.Strategies;

    internal class Program
    {
        private static void Main(string[] args)
        {
            var modelName = "FA160";
            //var modelName = "FA001";

            //var srcModel = new TModel($"Data/Problems/{modelName}_src.mdl");
            var tgtModel = new TModel($"Data/Problems/{modelName}_tgt.mdl");
            var srcModel = TModel.MakeEmpty(tgtModel.Name, tgtModel.R);

            void TestStrategy(IStrategy strategy, bool saveTrace = false)
            {
                Console.WriteLine($"=== {strategy.Name}");

                try
                {
                    var trace = strategy.MakeTrace(srcModel, tgtModel);
                    if (saveTrace)
                    {
                        File.WriteAllBytes("trace.nbt", TraceSerializer.Serialize(trace));
                    }
                    if (trace == null)
                    {
                        Console.WriteLine("  empty trace");
                        return;
                    }
                    var state = new TState(srcModel);
                    var reader = new TCommandsReader(trace);
                    while (!reader.AtEnd())
                    {
                        state.Step(reader);
                    }

                    Console.WriteLine(state.HasValidFinalState(tgtModel));
                    Console.WriteLine(state.Energy);
                }
                catch (Exception e)
                {
                    Console.Write($"BROKENL {e}");
                }
            }

            //TestStrategy(new DumpCubeStrategy());
            //TestStrategy(new DumpCubeStrategy(), true);
            TestStrategy(new TTraceReaderStrategy($"data/DefaultTraces"));
            TestStrategy(new BfsStrategy(), true);
        }
    }
}