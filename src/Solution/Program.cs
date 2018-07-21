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
            File.WriteAllBytes("trace", TraceSerializer.Serialize(trace));
        }
    }
}