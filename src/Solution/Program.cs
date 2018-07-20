namespace Solution
{
    using System;
    using System.Collections.Generic;

    internal class Program
    {
        private static void Main(string[] args)
        {
            var state = new TState();
            state.Load("problems/LA130_tgt.mdl");

            List<object> commands = TraceReader.Read("traces/LA130.nbt");

            int step = 0;
            while (commands.Count > 0)
            {
                state.ApplyCommands(commands);
                step += 1;

                if (step % 10000 == 0)
                {
                    Console.WriteLine(step);
                }
            }

            Console.WriteLine(state.HasValidFinalState());
            Console.WriteLine(state.Energy);
        }
    }
}