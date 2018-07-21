namespace Solution
{
    using System.Collections.Generic;

    public interface IStrategy
    {
        string Name { get; }
        List<ICommand> MakeTrace(TModel model);
    }
}