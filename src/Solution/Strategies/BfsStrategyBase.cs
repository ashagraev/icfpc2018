namespace Solution.Strategies
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

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
                private TCoord Coord;

                // Targets
                public TCoord? FusionTarget;
                public TCoord? FillTarget;

                // a trace to some cell near target
                public List<ICommand> MoveCommands;
                public int NextCommand = 0;

                public bool HasTarget => (FusionTarget != null) && (FillTarget != null);
            }

            private readonly TModel model;
            private TState state;
            private List<Bot> bots;
            private readonly int numFilled = 0;

            private HashSet<TCoord> availablePositions;

            public Impl(TModel model, int maxBots)
            {
                this.model = model;
                state = new TState(this.model);

                for (var x = 0; x < model.R; ++x)
                {
                    for (var z = 0; x < model.R; ++z)
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
                        if (!bot.HasTarget || !CanMove(bot, interferedCells))
                        {
                            ChooseNewTarget(bot, ref interferedCells);
                        }

                        if (bot.HasTarget && CanMove(bot, interferedCells))
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
            }

            private ICommand MoveBot(Bot bot, ref HashSet<TCoord> interferedCells, ref List<Bot> newBots)
            {
                return null;
            }

            private void ChooseNewTarget(Bot bot, ref HashSet<TCoord> interferedCells)
            {
            }
        }
    }
}