namespace Solution
{
    using System;
    using System.Collections.Generic;
    using System.IO;

    using Solution.Strategies;

    internal class Program
    {
        private static void Main(string[] args)
        {
            TModel model = new TModel("problems/LA001_tgt.mdl");

            DumpCubeStrategy strategy = new DumpCubeStrategy();
            List<ICommand> trace = strategy.MakeTrace(model);
            List<ICommand> baselineTrace = TraceReader.Read("traces/LA001.nbt");

            {
                TState state = new TState(model);
                TCommandsReader reader = new TCommandsReader(trace);
                while (!reader.AtEnd())
                {
                    state.Step(reader);
                }

                Console.WriteLine(state.HasValidFinalState());
                Console.WriteLine(state.Energy);
            }
            {
                TState state = new TState(model);
                TCommandsReader reader = new TCommandsReader(baselineTrace);
                while (!reader.AtEnd())
                {
                    state.Step(reader);
                }

                Console.WriteLine(state.HasValidFinalState());
                Console.WriteLine(state.Energy);
            }

            File.WriteAllBytes("trace", TraceSerializer.Serialize(trace));
        }
    }
}