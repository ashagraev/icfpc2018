namespace Solution
{
    using System;

    internal class Program
    {
        private static void Main(string[] args)
        {
            var state = TState.LoadFromFile("problems/LA130_tgt.mdl");

            var commands = TraceReader.Read("traces/LA186.nbt");

            var step = 0;
            var commandsReader = new TCommandsReader(commands);
            while (!commandsReader.AtEnd())
            {
                state.Step(commandsReader);
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