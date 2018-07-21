namespace Solution
{
    using System.Collections.Generic;

    internal interface TStrategy
    {
        List<ICommand> MakeTrace(TModel model);
    }
}