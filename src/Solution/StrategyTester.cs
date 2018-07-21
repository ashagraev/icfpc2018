namespace Solution
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;

    internal static class StrategyTester
    {
        public static void Test(string modelsDirectory, IEnumerable<IStrategy> strategiesEum)
        {
            var strategies = strategiesEum.ToArray();
            var models = LoadModels(modelsDirectory);
            foreach (var model in models)
            {
                Console.WriteLine($"{model.Path}");
                foreach (var strategy in strategies)
                {
                    Console.Write($"  {strategy.Name}: ");
                    var commands = strategy.MakeTrace(model);

                    var state = new TState(model);
                    var step = 0;
                    var commandsReader = new TCommandsReader(commands);
                    while (!commandsReader.AtEnd())
                    {
                        state.Step(commandsReader);
                        step += 1;

                        if (step % 1000000 == 0)
                        {
                            Console.Write(".");
                        }
                    }

                    Console.Write(state.Energy);

                    if (!state.HasValidFinalState())
                    {
                        Console.Write(" !!FAIL!! ");
                    }

                    Console.WriteLine();
                }
            }
        }

        private static IEnumerable<TModel> LoadModels(string modelsDirectory)
        {
            foreach (var file in Directory.EnumerateFiles(modelsDirectory))
            {
                if (Path.GetExtension(file) == ".mdl")
                {
                    yield return new TModel(file);
                }
            }
        }
    }
}