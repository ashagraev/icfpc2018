namespace Solution
{
    using System.Collections.Generic;
    using System.IO;

    using Solution.Strategies;

    internal class Program
    {
        private static void Main(string[] args)
        {
            TModel model = new TModel("problems/LA001_tgt.mdl");

            AlexShBaseStrategy strategy = new AlexShBaseStrategy();
            List<ICommand> trace = strategy.MakeTrace(model);
            File.WriteAllBytes("trace", TraceSerializer.Serialize(trace));
        }
    }
}