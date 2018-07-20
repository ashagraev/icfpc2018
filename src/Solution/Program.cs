namespace Solution
{
    using System;
    using System.Collections.Generic;

    internal class Program
    {
        private static void Main(string[] args)
        {
            var state = new TState();
            state.Load("problems/LA001_tgt.mdl");

            List<object> commands = TraceReader.Read("traces/LA001.nbt");

            while (commands.Count > 0)
            {
                state.ApplyCommands(commands);
            }

            Console.WriteLine(state.HasValidFinalState());
        }
    }
}