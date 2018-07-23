namespace Solution.Strategies
{
    public class BfsStrategy : BfsStrategyBase
    {
        public override string Name => nameof(BfsStrategy);

        public BfsStrategy()
            : base(40)
        {

        }
    }
}