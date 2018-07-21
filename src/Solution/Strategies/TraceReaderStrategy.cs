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
            var modelName = Path.GetFileNameWithoutExtension(model.Path);
            if (modelName.EndsWith("_tgt"))
            {
                modelName = modelName.Remove(modelName.Length - 4);
            }

            return TraceReader.Read($"{tracesDir}/{modelName}.nbt");
        }
    }
}