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
    using System.Threading;
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
        private readonly string[] AllowedPrefixes;

        public StrategyTester(string[] allowedPrefixes)
        {
            AllowedPrefixes = allowedPrefixes;
        }

        internal struct Task
        {
            public TModel Src;
            public TModel Tgt;

            public Task(TModel src, TModel tgt)
            {
                if (tgt.R == 0)
                {
                    tgt = TModel.MakeEmpty(src.Name, src.R);
                }

                if (src.R == 0)
                {
                    src = TModel.MakeEmpty(tgt.Name, tgt.R);
                }

                Src = src;
                Tgt = tgt;
            }

            public string Name => Tgt.NumFilled == 0 ? Src.Name : Tgt.Name;
        }

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

            var tasks = LoadTasks(modelsDirectory);
            tasks
                .AsParallel()
                .WithDegreeOfParallelism(15)
                .WithExecutionMode(ParallelExecutionMode.ForceParallelism)
                .Select(ProcessTask)
                .ToArray(); // force materialization!

            foreach (var s in Strategies)
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

        private int ProcessTask(Task task)
        {
            var stream = new MemoryStream();
            var writer = new StreamWriter(stream);

            var traceFile = $"{BestStrategiesDirectory}/{task.Name}.nbt";

            if (!AllowedPrefixes.Any(x => task.Name.StartsWith(x)))
            {
                writer.WriteLine($"Model {task.Name} is not allowed, copying the default trace");
                File.Copy($"{DefaultTracesDirectory}/{Path.GetFileName(traceFile)}", traceFile, true);
            }
            else
            {
                IStrategy[] allowedStrategies = Strategies;
                if (!Path.GetFileName(task.Name).StartsWith("FA"))
                {
                    allowedStrategies = new IStrategy[1];
                    allowedStrategies[0] = new DumpCubeStrategy();
                }

                writer.WriteLine($"{task.Name}");
                var (best, _) = RunStrategy(task, BaselineStrategy, writer);
                if (best != null)
                {
                    StrategyStats[BaselineStrategy.Name] += best.Value;
                }

                foreach (var strategy in allowedStrategies)
                {
                    var (energy, commands) = RunStrategy(task, strategy, writer);

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
            }

            writer.Flush();

            stream.Seek(0, SeekOrigin.Begin);
            StreamReader reader = new StreamReader(stream);

            lock (Lock) {
                Console.Write(reader.ReadToEnd());
            }

            return 0;
        }

        private List<KeyValuePair<TModel, TModel>> LoadReassemblyModels(string modelsDirectory)
        {
            List<KeyValuePair<TModel, TModel>> result = new List<KeyValuePair<TModel, TModel>>();

            foreach (var file in Directory.EnumerateFiles(modelsDirectory))
            {
                string name = Path.GetFileNameWithoutExtension(file);
                if (!name.Contains("FR") || !name.Contains("_tgt"))
                {
                    continue;
                }

                string srcPath = file.Replace("_tgt", "_src");

                TModel srcModel = new TModel(srcPath);
                TModel tgtModel = new TModel(file);

                result.Add(new KeyValuePair<TModel, TModel>(srcModel, tgtModel));
            }

            result.Reverse();
            return result;
        }

        private IEnumerable<Task> LoadTasks(string modelsDirectory)
        {
            foreach (var file in Directory.EnumerateFiles(modelsDirectory))
            {
                if (Path.GetExtension(file) == ".mdl")
                {
                    string name = Path.GetFileNameWithoutExtension(file);
                    if (name.StartsWith("FA"))
                    {
                        yield return new Task(new TModel(), new TModel(file));
                    }
                    else if (name.StartsWith("FD"))
                    {
                        yield return new Task(new TModel(file), new TModel());
                    }
                    else
                    {
                        if (name.Contains("_tgt"))
                        {
                            string srcPath = file.Replace("_tgt", "_src");
                            yield return new Task(new TModel(srcPath), new TModel(file));
                        }
                    }
                }
            }
        }

        private (long? energy, List<ICommand> commands) RunStrategy(Task task, IStrategy strategy, StreamWriter writer)
        {
            writer.Write($"  {strategy.Name}: ");

            try
            {
                var commands = strategy.MakeTrace(task.Src, task.Tgt);

                if (commands == null)
                {
                    return (null, null);
                }

                var state = new TState(task.Src);
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

                if (!state.HasValidFinalState(task.Tgt))
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