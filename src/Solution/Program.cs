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

            TDumpCubeTraverse dumpCureTraverse = new TDumpCubeTraverse(model);

            TCoord start = new TCoord();
            TCoord next = dumpCureTraverse.Next();
            while (!next.IsAtStart())
            {
                Console.Write(next.X);
                Console.Write(" ");
                Console.Write(next.Y);
                Console.Write(" ");
                Console.Write(next.Z);
                Console.WriteLine();

                next = dumpCureTraverse.Next();
            }

            DumpCubeStrategy strategy = new DumpCubeStrategy();
            List<ICommand> trace = strategy.MakeTrace(model);
            File.WriteAllBytes("trace", TraceSerializer.Serialize(trace));
        }
    }
}