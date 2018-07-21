namespace Solution
{
    using System;
    using System.Collections.Generic;

    using Solution.Strategies;

    internal class Program
    {
        private static void Main(string[] args)
        {
            StrategyTester.Test(
                "Data/Problems",
                new[]
                    {
                       new TTraceReaderStrategy("Data/DefaultTraces") 
                    }
            );

            List<ICommand> commands = AlexShBaseStrategy.MakeTrace(model);
            TCommandsReader commandsReader = new TCommandsReader(commands);

            int step = 0;
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