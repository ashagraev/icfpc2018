namespace TestStrategies
{
    using Solution;
    using Solution.Strategies;

    class Program
    {
        static void Main(string[] args)
        {
            StrategyTester.Test(
                "Data/Problems",
                "Data/BestTraces",
                new IStrategy[]
                {
                    new TTraceReaderStrategy("Data/DefaultTraces"),
                    new AlexShBaseStrategy(),
                }
            );
        }
    }
}
