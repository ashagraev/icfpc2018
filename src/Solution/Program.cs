namespace Solution
{
    using Solution.Strategies;

    internal class Program
    {
        private static void Main(string[] args)
        {
            StrategyTester.Test(
                "Data/Problems",
                new IStrategy[]
                {
                    new TTraceReaderStrategy("Data/DefaultTraces"),
                    new AlexShBaseStrategy(),
                }
            );
        }
    }
}