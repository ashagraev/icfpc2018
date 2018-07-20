namespace Solution
{
    using System;
    using System.Collections.Generic;

    internal class Program
    {
        private static void Main(string[] args)
        {
            var state = new TState();
            state.Load("problems/LA186_tgt.mdl");

            TCommands commands = new TCommands();
            commands.Commands = TraceReader.Read("traces/LA186.nbt");

            int step = 0;
            while (!commands.AtEnd())
            {
                state.ApplyCommands(commands);
                step += 1;

                if (step % 1000000 == 0)
                {
                    Console.WriteLine(step);
                }
            }

            Console.WriteLine(state.HasValidFinalState());
            Console.WriteLine(state.Energy);
        }
    }
}