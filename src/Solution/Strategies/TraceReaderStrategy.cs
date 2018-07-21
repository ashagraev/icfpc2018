namespace Solution.Strategies
{
    using System.Collections.Generic;
    using System.IO;

    public class TTraceReaderStrategy : IStrategy
    {
        private readonly string tracesDir;

        public TTraceReaderStrategy(string tracesDir)
        {
            this.tracesDir = tracesDir;
        }

        public string Name => "Traces:" + Path.GetFileName(tracesDir);

        List<ICommand> IStrategy.MakeTrace(TModel model)
        {
            var traceFile = $"{tracesDir}/{model.Name}.nbt";
            return File.Exists(traceFile)
                ? TraceReader.Read(traceFile)
                : new List<ICommand>();
        }
    }
}