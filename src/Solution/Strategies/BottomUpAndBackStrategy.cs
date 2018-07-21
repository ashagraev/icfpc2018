using System;
using System.Collections.Generic;
using System.Text;

namespace Solution.Strategies
{
    class BottomUpAndBackStrategy : IStrategy
    {
        public string Name => nameof(BottomUpAndBackStrategy);

        public List<ICommand> MakeTrace(TModel model)
        {
            return new List<ICommand>();
        }
    }
}
