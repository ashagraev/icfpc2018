namespace Solution.Strategies
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    [BrokenStrategy]
    internal class BottomUpAndBackStrategy : IStrategy
    {
        public string Name => nameof(BottomUpAndBackStrategy);

        public List<ICommand> MakeTrace(TModel model)
        {
            var impl = new Impl(model);
            return impl.MakeTrace().ToList();
        }

        internal class Impl
        {
            private class ComponentState
            {
                private int AttachedBots;
            }

            private int[,,] ConnectedComponent;

            private readonly TModel Model;
            private TState State;

            public Impl(TModel model)
            {
                Model = model;
                FindConnectedComponents();
            }

            public IEnumerable<ICommand> MakeTrace()
            {
                yield break;
            }

            private void FindConnectedComponents()
            {
                var nextComponentIdx = 0;
                ConnectedComponent = new int[Model.R, Model.R, Model.R];
                var queue = new Queue<(int x, int z)>();
                for (var x = 0; x < Model.R; ++x)
                {
                    for (var y = 0; y < Model.R; ++y)
                    {
                        for (var z = 0; z < Model.R; ++z)
                        {
                            if (ConnectedComponent[x, y, z] != 0)
                            {
                                continue;
                            }

                            var idx = ++nextComponentIdx;

                            void go(int x1, int z1)
                            {
                                if ((x1 >= 0) && (x1 < Model.R) && (z1 >= 0) && (z1 < Model.R) && (ConnectedComponent[x1, y, z1] == 0))
                                {
                                    ConnectedComponent[x1, y, z1] = idx;
                                    queue.Enqueue((x1, z1));
                                }
                            }

                            go(x, z);

                            while (queue.Count != 0)
                            {
                                var (x1, z1) = queue.Dequeue();
                                go(x1 - 1, z1);
                                go(x1 + 1, z1);
                                go(x1, z1 - 1);
                                go(z1, z1 + 1);
                            }
                        }
                    }
                }

                Console.WriteLine(nextComponentIdx);
            }
        }
    }
}