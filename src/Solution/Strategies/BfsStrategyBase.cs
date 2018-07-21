namespace Solution.Strategies
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net.Sockets;

    [BrokenStrategy]
    public abstract class BfsStrategyBase : IStrategy
    {
        public string Name => nameof(BfsStrategyBase);

        public List<ICommand> MakeTrace(TModel model)
        {
            var impl = new Impl(model, 20);
            return impl.MakeTrace().ToList();
        }

        internal class Impl
        {
            private class Bot
            {
                public int Id;
                public List<int> Seeds;
                public TCoord Coord;

                // Targets
                public TCoord? FusionTarget;
                public TCoord? FillTarget;

                public TCoord? Target => FillTarget ?? FusionTarget;

                // a trace to some cell near target
                public List<ICommand> MoveCommands;
                public int NextCommand = 0;
            }

            private readonly TModel model;
            private int maxBots;
            private TState state;
            private List<Bot> bots;
            private readonly int numFilled = 0;

            private HashSet<TCoord> availablePositions;

            public Impl(TModel model, int maxBots)
            {
                this.model = model;
                this.maxBots = maxBots;
                state = new TState(this.model);

                bots = new List<Bot>
                {
                    new Bot
                    {
                        Id = 1,
                        Coord = new TCoord(0, 0, 0),
                        Seeds = new List<int>()
                    }
                };
                Console.WriteLine(bots.Count);
                for (var i = 2; i <= 20; ++i)
                {
                    bots[0].Seeds.Add(i);
                }

                availablePositions = new HashSet<TCoord>();
                for (var x = 0; x < model.R; ++x)
                {
                    for (var z = 0; z < model.R; ++z)
                    {
                        if (model[x, 0, z] == 1)
                        {
                            availablePositions.Add(new TCoord(x, 0, z));
                        }
                    }
                }
            }

            public IEnumerable<ICommand> MakeTrace()
            {
                var idleSteps = 0;
                var interferedCells = new HashSet<TCoord>();

                while (numFilled != model.NumFilled)
                {
                    var idle = true;
                    interferedCells.Clear();

                    List<Bot> newBots = null;

                    foreach (var bot in bots)
                    {
                        if (bot.Target == null || !CanMove(bot, interferedCells))
                        {
                            ChooseNewTarget(bot, ref interferedCells);
                        }

                        if (bot.Target != null && CanMove(bot, interferedCells))
                        {
                            idle = false;
                            yield return MoveBot(bot, ref interferedCells, ref newBots);
                        }
                        else
                        {
                            yield return new Wait();
                        }
                    }

                    if (idle)
                    {
                        // we are stuck. Just produce some garbage trace
                        if (++idleSteps >= 50)
                        {
                            yield break;
                        }
                    }
                    else
                    {
                        idleSteps = 0;
                    }

                    if (newBots != null)
                    {
                        bots.AddRange(newBots);
                        bots = bots.OrderBy(bot => bot.Id).ToList();
                    }
                }
            }

            private bool CanMove(Bot bot, HashSet<TCoord> interferedCells)
            {
                return false;
                if (bot.NextCommand < bot.MoveCommands.Count)
                {
                    switch (bot.MoveCommands[bot.NextCommand])
                    {
                        case StraightMove m:
                            break;
                        case LMove m:
                            break;
                        default:
                            throw new Exception("WTF");
                    }
                }
            }

            private ICommand MoveBot(Bot bot, ref HashSet<TCoord> interferedCells, ref List<Bot> newBots) => null;

            private void ChooseNewTarget(Bot bot, ref HashSet<TCoord> interferedCells)
            {
            }
        }
    }
}