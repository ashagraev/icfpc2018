namespace TestStrategies
{
    using System;
    using System.Linq;

    using Solution;
    using Solution.Strategies;

    class Program
    {
        static void Main(string[] args)
        {
            var strategies = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(assembly => assembly.GetTypes())
                .Where(
                    type =>
                        type.IsClass &&
                        typeof(IStrategy).IsAssignableFrom(type) &&
                        type.GetCustomAttributes(typeof(BrokenStrategy), true).Length == 0)
                .Select(CreateStrategy)
                .Where(strategy => strategy != null);

            StrategyTester.Test("Data/Problems", "Data/BestTraces", "Data/DefaultTraces", strategies);
        }

        private static IStrategy CreateStrategy(Type type)
        {
            try
            {
                return (IStrategy)Activator.CreateInstance(type);
            }
            catch (Exception e)
            {
                Console.WriteLine($"!!!! Failed to create strategy ${type.FullName}: {e}");
                return null;
            }
        }
    }
}
