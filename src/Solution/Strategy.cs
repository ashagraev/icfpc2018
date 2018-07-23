namespace Solution
{
    using System;
    using System.Collections.Generic;

    public interface IStrategy
    {
        string Name { get; }
        List<ICommand> MakeTrace(TModel model);
        List<ICommand> MakeReassemblyTrace(TModel srcModel, TModel tgtModel);
    }

    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    public class BrokenStrategy : Attribute
    {
    }
}