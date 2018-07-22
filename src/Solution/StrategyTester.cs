namespace Solution
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.IO.Compression;
    using System.Linq;
    using System.Reflection.Metadata.Ecma335;
    using System.Security.Cryptography;
    using System.Text;
    using System.Threading.Tasks;

    using Solution.Strategies;

    public class StrategyTester
    {
        private Object Lock = new Object();

        private string BestStrategiesDirectory;
        private string DefaultTracesDirectory;
        private TTraceReaderStrategy BestStrategy;
        private TTraceReaderStrategy BaselineStrategy;
        private IStrategy[] Strategies;
        private Dictionary<string, long> StrategyStats;

        public void Test(string modelsDirectory, string bestStrategiesDirectory, string defaultTracesDirectory, IEnumerable<IStrategy> strategiesEnum)
        {
            BestStrategiesDirectory = bestStrategiesDirectory;
            DefaultTracesDirectory = defaultTracesDirectory;

            StrategyStats = new Dictionary<String, long>();
            Strategies = strategiesEnum.ToArray();
            BestStrategy = new TTraceReaderStrategy("Data/BestTraces");
            BaselineStrategy = new TTraceReaderStrategy("Data/DefaultTraces");

            foreach (var strategy in Strategies)
            {
                StrategyStats[strategy.Name] = 0;
            }
            StrategyStats[BaselineStrategy.Name] = 0;

            var models = LoadModels(modelsDirectory);
            Parallel.ForEach<TModel>(models, new ParallelOptions { MaxDegreeOfParallelism = 15 }, ProcessModel);

            foreach (IStrategy s in Strategies)
            {
                if (!StrategyStats.ContainsKey(s.Name))
                {
                    continue;
                }

                Console.WriteLine(s.Name);
                Console.WriteLine(StrategyStats[s.Name]);
                Console.WriteLine((float) StrategyStats[s.Name] / StrategyStats[BaselineStrategy.Name]);
                Console.WriteLine("");
            }

            MakeSubmission(bestStrategiesDirectory, defaultTracesDirectory);
        }

        private void ProcessModel(TModel model)
        {
            var stream = new MemoryStream();
            var writer = new StreamWriter(stream);

            var traceFile = $"{BestStrategiesDirectory}/{model.Name}.nbt";

            if (!Path.GetFileName(model.Name).StartsWith("FD"))
            {
                return;
            }

            IStrategy[] allowedStrategies = Strategies;
            if (!Path.GetFileName(model.Name).StartsWith("FA"))
            {
                // TODO: remove this stupid hack when our strategies are able to destroy/reassemble models.
                allowedStrategies = new IStrategy[1];
                allowedStrategies[0] = new DumpCubeStrategy();
//                File.Copy($"{DefaultTracesDirectory}/{Path.GetFileName(traceFile)}", traceFile, true);
            }

            writer.WriteLine($"{model.Name}");
            var (best, _) = RunStrategy(model, BaselineStrategy, writer);
            if (best != null) {
                StrategyStats[BaselineStrategy.Name] += best.Value;
            }

            foreach (var strategy in allowedStrategies)
            {
                var (energy, commands) = RunStrategy(model, strategy, writer);

                if (energy != null)
                {
                    lock (Lock)
                    {
                        StrategyStats[strategy.Name] += energy.Value;
                    }
                }

                if ((energy != null) && ((best == null) || (energy < best)))
                {
                    writer.WriteLine("  NEW BEST!!!");
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

            writer.Flush();

            stream.Seek(0, SeekOrigin.Begin);
            StreamReader reader = new StreamReader(stream);

            lock (Lock) {
                Console.Write(reader.ReadToEnd());
            }
    }

        private List<TModel> LoadModels(string modelsDirectory)
        {
            List<TModel> result = new List<TModel>();
            foreach (var file in Directory.EnumerateFiles(modelsDirectory))
            {
                if (Path.GetExtension(file) == ".mdl")
                {
                    result.Add(new TModel(file));
                }
            }
            //      result.Reverse();
            return result;
        }

        private (long? energy, List<ICommand> commands) RunStrategy(TModel model, IStrategy strategy, StreamWriter writer)
        {
            writer.Write($"  {strategy.Name}: ");

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
                        writer.Write(".");
                    }
                }

                writer.Write(state.Energy);

                if (!state.HasValidFinalState())
                {
                    writer.WriteLine(" !!FAIL!! ");
                    return (null, null);
                }

                writer.WriteLine();
                return (state.Energy, commands);
            }
            catch (Exception e)
            {
                writer.WriteLine($"exception: {e}");
                return (null, null);
            }
        }

        private void MakeSubmission(string bestStrategiesDirectory, string defaultTracesDirectory)
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