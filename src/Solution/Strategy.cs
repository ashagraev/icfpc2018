namespace Solution
{
    using System.Collections.Generic;

    internal interface IStrategy
    {
        string Name { get; }
        List<ICommand> MakeTrace(TModel model);
    }
}