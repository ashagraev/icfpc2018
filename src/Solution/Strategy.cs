using System;
using System.Collections.Generic;
using System.Text;

namespace Solution
{
    interface TStrategy
    {
        TCommands MakeTrace(TModel model);
    }
}
