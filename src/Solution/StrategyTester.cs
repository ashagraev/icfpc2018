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
        public static void Test(string modelsDirectory, string bestStrategiesDirectory, string defaultTracesDirectory, IEnumerable<IStrategy> strategiesEnum)
        {
            var strategyStats = new Dictionary<String, long>();
            var strategies = strategiesEnum.ToArray();
            var bestStrategy = new TTraceReaderStrategy("Data/BestTraces");

            var models = LoadModels(modelsDirectory);
            foreach (var model in models)
            {
                var traceFile = $"{bestStrategiesDirectory}/{model.Name}.nbt";

                if (!Path.GetFileName(model.Name).StartsWith("FA"))
                {
                    // TODO: remove this stupid hack when our strategies are able to destroy/reassemble models.
                    File.Copy($"{defaultTracesDirectory}/{Path.GetFileName(traceFile)}", traceFile, true);
                    continue;
                }

                Console.WriteLine($"{model.Name}");
                var (best, _) = RunStrategy(model, bestStrategy);

                foreach (var strategy in strategies)
                {
                    var (energy, commands) = RunStrategy(model, strategy);

                    if (energy != null)
                    {
                        if (strategyStats.ContainsKey(strategy.Name))
                        {
                            strategyStats[strategy.Name] += energy.Value;
                        }
                        else
                        {
                            strategyStats[strategy.Name] = energy.Value;
                        }
                    }

                    if ((energy != null) && ((best == null) || (energy < best)))
                    {
                        Console.WriteLine("  NEW BEST!!!");
                        best = energy;

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

            var baselineStrategy = new TTraceReaderStrategy("Data/DefaultTraces");

            foreach (IStrategy s in strategies)
            {
                Console.WriteLine(s.Name);
                Console.WriteLine(strategyStats[s.Name]);
                Console.WriteLine((float) strategyStats[s.Name] / strategyStats[baselineStrategy.Name]);
                Console.WriteLine("");
            }

            MakeSubmission(bestStrategiesDirectory, defaultTracesDirectory);
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

        private static void MakeSubmission(string bestStrategiesDirectory, string defaultTracesDirectory)
        {
            var submissionDirectory = "SubmissionContents";
            if (Directory.Exists(submissionDirectory))
            {
                Directory.Delete(submissionDirectory, true);
            }
            Directory.CreateDirectory(submissionDirectory);

            foreach (var trace in Directory.EnumerateFiles(defaultTracesDirectory))
            {
                var traceBaseName = Path.GetFileName(trace);
                var ourBestTrace = $"{bestStrategiesDirectory}/{traceBaseName}";
                File.Copy(ourBestTrace, $"{submissionDirectory}/{traceBaseName}");
            }

            File.Delete("submission.zip");
            ZipFile.CreateFromDirectory(submissionDirectory, "submission.zip");
            Directory.Delete(submissionDirectory, true);

            var sha256 = SHA256.Create();
            byte[] hash = null;
            using (var f = File.OpenRead("submission.zip"))
            {
                hash = sha256.ComputeHash(f);
            }

            File.Delete("submission.sha256.txt");
            using (var f = File.OpenWrite("submission.sha256.txt"))
            {
                var hex = BitConverter.ToString(hash).Replace("-", string.Empty);
                f.Write(Encoding.UTF8.GetBytes(hex));
            }
        }
    }
}