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
                public TCoord? FissionTarget;
                public TCoord? FillTarget;

                public TCoord? Target => FillTarget ?? FissionTarget;

                // a trace to some cell near target
                public List<ICommand> MoveCommands;
                public int NextCommand;
            }

            private readonly TModel model;
            private int maxBots;
            private TState state;
            private List<Bot> bots;
            private readonly int numFilled = 0;

            private readonly HashSet<TCoord> availablePositions;
            private readonly HashSet<TCoord> interferedCells;

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

                interferedCells = new HashSet<TCoord>();
            }

            public IEnumerable<ICommand> MakeTrace()
            {
                var idleSteps = 0;

                while (numFilled != model.NumFilled)
                {
                    var idle = true;
                    interferedCells.Clear();

                    foreach (var bot in bots)
                    {
                        interferedCells.Add(bot.Coord);
                    }

                    List<Bot> newBots = null;

                    foreach (var bot in bots)
                    {
                        if ((bot.Target == null) || !CanMove(bot))
                        {
                            ChooseNewTarget(bot, newBots);
                        }

                        if ((bot.Target != null) && CanMove(bot))
                        {
                            idle = false;
                            yield return MoveBot(bot, ref newBots);
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

            private bool CanMove(Bot bot)
            {
                if (bot.NextCommand < bot.MoveCommands.Count)
                {
                    switch (bot.MoveCommands[bot.NextCommand])
                    {
                        case StraightMove m:
                            // TODO
                            break;
                        case LMove m:
                            // TODO
                            break;
                        default:
                            throw new Exception("WTF");
                    }
                }

                return IsFree(bot.Target.Value);
            }

            private ICommand MoveBot(Bot bot, ref List<Bot> newBots)
            {
                if (bot.NextCommand < bot.MoveCommands.Count)
                {
                    switch (bot.MoveCommands[bot.NextCommand])
                    {
                        case StraightMove m:
                            // TODO: update coords and interferedCells
                            ++bot.NextCommand;
                            return m;
                        case LMove m:
                            // TODO: update coords and interferedCells
                            ++bot.NextCommand;
                            return m;
                        default:
                            throw new Exception("WTF");
                    }
                }

                if (bot.FissionTarget != null)
                {
                    var m = bot.Seeds.Count;

                    // TODO: add bot into newBots

                    // TODO: coorddiff
                    return new Fission
                    {
                        Diff = new CoordDiff
                        {
                            Dx = 0,
                            Dy = 0,
                            Dz = 0
                        },
                        M = m
                    };
                }

                if (bot.FillTarget != null)
                {
                    return new Fill()
                    {
                        Diff = new CoordDiff()
                        {
                            Dx = 0,
                            Dy = 0,
                            Dz = 0,
                        }
                    };
                }

                throw new Exception("WTF");
            }

            private void ChooseNewTarget(Bot bot, List<Bot> newBots)
            {
                bot.FissionTarget = null;
                bot.FillTarget = null;
                bot.MoveCommands = null;
                bot.NextCommand = 0;

                if (false && bots.Count + (newBots?.Count ?? 0) < maxBots)
                {
                    foreach (var coord in bot.Coord.NearNeighbours())
                    {
                        if (IsFree(coord))
                        {
                            bot.FissionTarget = coord;
                            return;
                        }
                    }
                }

                var bestFillTarget = (Rank: double.MinValue, Coord: default(TCoord));
                foreach (var coord in availablePositions)
                {
                    var rank = CalcRank(coord);
                    if (rank > bestFillTarget.Rank)
                    {
                        bestFillTarget = (Rank: rank, Coord: coord);
                    }
                }

                if (bestFillTarget.Rank != double.MinValue)
                {
                    var bestMovementTarget = (Cost: int.MaxValue, Path: (List<ICommand>)null);
                    foreach (var coord in bestFillTarget.Coord.ManhattenNeighbours())
                    {
                        var (path, cost) = FindPath(coord);
                        if (path != null && cost < bestMovementTarget.Cost)
                        {
                            bestMovementTarget = (Cost: cost, Path: path);
                        }
                    }

                    if (bestMovementTarget.Path != null)
                    {
                        bot.FillTarget = bestFillTarget.Coord;
                        bot.MoveCommands = bestMovementTarget.Path;
                    }
                }
            }

            private bool IsFree(TCoord coord)
            {
                return !interferedCells.Contains(coord) && model[coord.X, coord.Y, coord.Z] == 0;
            }

            double CalcRank(TCoord coord)
            {
                return 1;
            }

            (List<ICommand> Path, int Cost) FindPath(TCoord coord)
            {
                throw new NotImplementedException();
            }
        }
    }
}