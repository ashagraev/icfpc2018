namespace Solution.Strategies
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    // [BrokenStrategy]
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
                public TCoord? MoveTarget;

                public TCoord? Target => FillTarget ?? FissionTarget ?? MoveTarget;

                // a trace to some cell near target
                public List<ICommand> MoveCommands;
                public int NextCommand;
            }

            private readonly TModel model;
            private readonly int maxBots;
            private TState state;
            private List<Bot> bots;

            private int[,,] depth;

            private readonly HashSet<TCoord> addedPositions;
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
                for (var i = 2; i <= 20; ++i)
                {
                    bots[0].Seeds.Add(i);
                }

                addedPositions = new HashSet<TCoord>();
                availablePositions = new HashSet<TCoord>();
                for (var x = 0; x < model.R; ++x)
                {
                    for (var z = 0; z < model.R; ++z)
                    {
                        if (model[x, 0, z] == 1)
                        {
                            addedPositions.Add(new TCoord(x, 0, z));
                            availablePositions.Add(new TCoord(x, 0, z));
                        }
                    }
                }

                interferedCells = new HashSet<TCoord>();

                CalcDepth();
            }

            private void CalcDepth()
            {
                depth = new int[model.R, model.R, model.R];
                var queue = new Queue<TCoord>();
                var queue2 = new Queue<TCoord>();
                queue.Enqueue(new TCoord(0, 0, 0));
                depth[0, 0, 0] = 1;
                while (queue.Count != 0)
                {
                    var cur = queue.Dequeue();
                    if (model[cur] != 0)
                    {
                        int d = depth[cur.X, cur.Y, cur.Z];
                        foreach(var n in cur.ManhattenNeighbours().Where(n => n.IsValid(model.R) && depth[n.X, n.Y, n.Z] == 0))
                        {
                            depth[n.X, n.Y, n.Z] = d + 1;
                            queue.Enqueue(n);
                        }
                    }
                    else
                    {
                        int d = depth[cur.X, cur.Y, cur.Z];
                        queue2.Enqueue(cur);
                        while (queue2.Count != 0)
                        {
                            var cur2 = queue2.Dequeue();
                            foreach (var n in cur2.ManhattenNeighbours().Where(n => n.IsValid(model.R) && depth[n.X, n.Y, n.Z] == 0))
                            {
                                if (model[n] == 0)
                                {
                                    depth[n.X, n.Y, n.Z] = d;
                                    queue2.Enqueue(n);
                                }
                                else
                                {
                                    depth[n.X, n.Y, n.Z] = d + 1;
                                    queue.Enqueue(n);
                                }
                            }
                        }
                    }
                }
            }

            public IEnumerable<ICommand> MakeTrace()
            {
                var idleSteps = 0;

                while (true)
                {
                    var idle = true;
                    interferedCells.Clear();

                    foreach (var bot in bots)
                    {
                        interferedCells.Add(bot.Coord);
                    }

                    List<Bot> newBots = null;

                    if (availablePositions.Count == 0 && bots.Count == 1 && bots[0].Coord.IsAtStart())
                    {
                        yield return new Halt();
                        yield break;
                    }

                    foreach (var bot in bots)
                    {
                        if ((bot.Target == null) || !CanMove(bot))
                        {
                            ChooseNewTarget(bot, newBots);
                        }

                        if ((bot.Target != null) && CanMove(bot))
                        {
                            idle = false;
                            var pc = bot.Coord;
                            yield return MoveBot(bot, ref newBots);
                            Console.WriteLine($"{pc} -> {bot.Coord}");
                        }
                        else
                        {
                            yield return new Wait();
                        }
                    }

                    if (idle)
                    {
                        // we are stuck. Just produce some garbage trace
                        if (++idleSteps >= 2)
                        {
                            Console.WriteLine("STUCK");
                            yield break;
                            throw new Exception("STUCK");
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
                            TCoord cur = bot.Coord;
                            TCoord dst = cur;
                            dst.Apply(m.Diff);
                            var step = new CoordDiff(Math.Sign(m.Diff.Dx), Math.Sign(m.Diff.Dy), Math.Sign(m.Diff.Dz));
                            do
                            {
                                cur.Apply(step);
                                if (!IsFree(cur))
                                {
                                    return false;
                                }
                            }
                            while (!cur.Equals(dst));

                            return true;
                        case LMove m:
                            throw new NotImplementedException();
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
                            bot.Coord.Apply(m.Diff);
                            ++bot.NextCommand;

                            // TODO: mark interfered cells
                            return m;
                        case LMove m:
                            throw new NotImplementedException();

                            // TODO: update coords and interferedCells
                            ++bot.NextCommand;
                            return m;
                        default:
                            throw new Exception("WTF");
                    }
                }

                if (bot.FissionTarget != null)
                {
                    throw new NotImplementedException();
                    var m = bot.Seeds.Count;

                    // TODO: add bot into newBots
                    bot.FissionTarget = null;

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
                    availablePositions.Remove(bot.FillTarget.Value);
                    state.Matrix[bot.FillTarget.Value.X, bot.FillTarget.Value.Y, bot.FillTarget.Value.Z] = 1;
                    foreach (var c in bot.FillTarget.Value.ManhattenNeighbours())
                    {
                        if (c.IsValid(model.R) && model[c] != 0 && !addedPositions.Contains(c))
                        {
                            addedPositions.Add(c);
                            availablePositions.Add(c);
                        }
                    }

                    var ret = new Fill
                    {
                        Diff = bot.FillTarget.Value.Diff(bot.Coord)
                    };
                    bot.FillTarget = null;
                    return ret;
                }

                throw new Exception("WTF");
            }

            private void ChooseNewTarget(Bot bot, List<Bot> newBots)
            {
                bot.FissionTarget = null;
                bot.FillTarget = null;
                bot.MoveCommands = null;
                bot.NextCommand = 0;

                // temporary no multiplying
                if (false && (bots.Count + (newBots?.Count ?? 0) < maxBots))
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

                if (availablePositions.Count == 0)
                {
                    bot.MoveTarget = new TCoord(0, 0, 0);
                    var (path, cost) = FindPath(bot.Coord, bot.MoveTarget.Value);
                    if (path == null)
                    {
                        throw new Exception("NO PATH");
                    }

                    bot.MoveCommands = path;
                    bot.NextCommand = 0;

                    return;
                }

                var bestFillTarget = (Rank: double.MinValue, Coord: default(TCoord));
                foreach (var coord in availablePositions)
                {
                    var rank = CalcRank(bot, coord);
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
                        if (coord.IsValid(model.R) && state.Matrix[coord.X, coord.Y, coord.Z] == 0)
                        {
                            var (path, cost) = FindPath(bot.Coord, coord);
                            if ((path != null) && (cost < bestMovementTarget.Cost))
                            {
                                bestMovementTarget = (Cost: cost, Path: path);
                            }
                        }
                    }

                    if (bestMovementTarget.Path != null)
                    {
                        bot.FillTarget = bestFillTarget.Coord;
                        bot.MoveCommands = bestMovementTarget.Path;

                        /*
                                                Console.WriteLine($"BC: {bestMovementTarget.Cost}");
                                                foreach (var c in bestMovementTarget.Path)
                                                {
                                                    Console.WriteLine(c);
                                                }
                                                */
                    }
                }

                Console.WriteLine($"COORDS: {bot.Coord}, TARGET: {bot.Target}, D: {(bot.Target == null ? -1 : depth[bot.Target.Value.X, bot.Target.Value.Y, bot.Target.Value.Z])}, M: {bot.MoveCommands?.Count}");
            }

            private bool IsFree(TCoord coord) => !interferedCells.Contains(coord) && (state.Matrix[coord.X, coord.Y, coord.Z] == 0);

            private double CalcRank(Bot bot, TCoord coord) =>
                depth[coord.X, coord.Y, coord.Z] * 65536 - bot.Coord.Diff(coord).MLen();

            private struct TCellData
            {
                public TCoord From;
                public int Cost;

                public bool Visited => Cost != 0;
            }

            private (List<ICommand> Path, int Cost) FindPath(TCoord src, TCoord dst)
            {
                // THIS IS VERY DUMB ALGO. BECAUSE I'M TOO STUPID TOO CODE DJIKSTRA PROPERLY
                // TODO: support LMoves
                var cellData = new TCellData[model.R, model.R, model.R];
                var queue = new Queue<TCoord>();
                queue.Enqueue(src);
                cellData[src.X, src.Y, src.Z].Cost = 1;
                while (queue.Count != 0)
                {
                    var cur = queue.Dequeue();
                    if (cur.Equals(dst))
                    {
                        var modifiedCost = cellData[cur.X, cur.Y, cur.Z].Cost - depth[cur.X, cur.Y, cur.Z] * 65536;
                        return (RecreatePath(), modifiedCost);
                    }

                    var clen = Math.Min(cur.Diff(dst).CLen(), Constants.StraightMoveCorrection);

                    var (minDx, maxDx) = FindRange(1, 0, 0);
                    var (minDy, maxDy) = FindRange(0, 1, 0);
                    var (minDz, maxDz) = FindRange(0, 0, 1);

                    // super heuristic
                    for (var i = clen; i > 0; --i)
                    {
                        DoVisits(i);
                    }

                    for (var i = clen + 1; i < Constants.StraightMoveCorrection; ++i)
                    {
                        DoVisits(i);
                    }

                    (int, int) FindRange(int dx, int dy, int dz)
                    {
                        var min = 0;
                        var minCoord = cur;
                        do
                        {
                            minCoord.Apply(new CoordDiff(-1 * dx, -1 * dy, -1 * dz));
                            --min;
                        }
                        while ((min >= -Constants.StraightMoveCorrection) && minCoord.IsValid(model.R) && IsFree(minCoord));

                        var max = 0;
                        var maxCoord = cur;
                        do
                        {
                            maxCoord.Apply(new CoordDiff(dx, dy, dz));
                            ++max;
                        }
                        while ((max <= Constants.StraightMoveCorrection) && maxCoord.IsValid(model.R) && IsFree(maxCoord));

                        return (min + 1, max - 1);
                    }

                    void DoVisits(int dist)
                    {
                        TryVisit(-dist >= minDx, dist, -dist, 0, 0);
                        TryVisit(dist <= maxDx, dist, dist, 0, 0);
                        TryVisit(-dist >= minDy, dist, 0, -dist, 0);
                        TryVisit(dist <= maxDy, dist, 0, dist, 0);
                        TryVisit(-dist >= minDz, dist, 0, 0, -dist);
                        TryVisit(dist <= maxDz, dist, 0, 0, dist);
                    }

                    void TryVisit(bool ok, int dist, int dx, int dy, int dz)
                    {
                        if (!ok)
                        {
                            return;
                        }

                        var next = new TCoord(cur.X + dx, cur.Y + dy, cur.Z + dz);
                        var curCost = cellData[cur.X, cur.Y, cur.Z].Cost;
                        if (next.IsValid(model.R) && !cellData[next.X, next.Y, next.Z].Visited)
                        {
                            // TODO: add energy maintainance into cost
                            cellData[next.X, next.Y, next.Z].From = cur;
                            cellData[next.X, next.Y, next.Z].Cost = curCost + (2 * dist);
                            queue.Enqueue(next);

                            // if (!IsFree(next)) throw new Exception("WTF");
                        }
                    }
                }

                return (null, int.MaxValue);

                List<ICommand> RecreatePath()
                {
                    var path = new List<ICommand>();
                    var cur = dst;
                    while (!cur.Equals(src))
                    {
                        var from = cellData[cur.X, cur.Y, cur.Z].From;
                        path.Add(
                            new StraightMove
                            {
                                Diff = cur.Diff(from)
                            });
                        cur = from;
                    }

                    path.Reverse();
                    return path;
                }
            }
        }
    }
}