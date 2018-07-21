namespace Solution
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.IO.Compression;
    using System.Linq;
    using System.Security.Cryptography;
    using System.Text;

    using Solution.Strategies;

    public static class StrategyTester
    {
        public static void Test(string modelsDirectory, string bestStrategiesDirectory, IEnumerable<IStrategy> strategiesEnum)
        {
            var strategies = strategiesEnum.ToArray();
            var bestStrategy = new TTraceReaderStrategy("Data/BestTraces");

            var models = LoadModels(modelsDirectory);
            foreach (var model in models)
            {
                Console.WriteLine($"{model.Name}");
                var (best, _) = RunStrategy(model, bestStrategy);

                foreach (var strategy in strategies)
                {
                    var (energy, commands) = RunStrategy(model, strategy);
                    if ((energy != null) && ((best == null) || (energy < best)))
                    {
                        Console.WriteLine("  NEW BEST!!!");
                        best = energy;
                        var traceFile = $"{bestStrategiesDirectory}/{model.Name}.nbt";

                        File.Delete(traceFile);
                        File.Delete($"{traceFile}.tmp");

                        using (var f = File.OpenWrite($"{traceFile}.tmp"))
                        {
                            f.Write(TraceSerializer.Serialize(commands));
                        }

                        File.Move($"{traceFile}.tmp", traceFile);

                        using (var f = File.OpenWrite($"{traceFile}.winner.txt"))
                        {
                            f.Write(Encoding.UTF8.GetBytes($"Strategy: {strategy.Name}"));
                        }
                    }
                }
            }

            MakeSubmission(bestStrategiesDirectory);
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

        private static (long? energy, List<ICommand> commands) RunStrategy(TModel model, IStrategy strategy)
        {
            Console.Write($"  {strategy.Name}: ");

            try
            {
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
                    Console.WriteLine(" !!FAIL!! ");
                    return (null, null);
                }

                Console.WriteLine();
                return (state.Energy, commands);
            }
            catch (Exception e)
            {
                Console.WriteLine($"exception: {e}");
                return (null, null);
            }
        }

        private static void MakeSubmission(string bestStrategiesDirectory)
        {
            ZipFile.CreateFromDirectory(bestStrategiesDirectory, "submission.zip");

            var sha256 = SHA256.Create();
            byte[] hash = null;
            using (var f = File.OpenRead("submission.zip"))
            {
                hash = sha256.ComputeHash(f);
            }

            using (var f = File.OpenWrite("submission.sha256.txt"))
            {
                var hex = BitConverter.ToString(hash).Replace("-", string.Empty);
                f.Write(Encoding.UTF8.GetBytes(hex));
            }
        }
    }
}