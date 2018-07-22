namespace Solution.Strategies
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    // [BrokenStrategy]
    public abstract class BfsStrategyBase : IStrategy
    {
        private readonly int maxBots = 2;

        public virtual string Name => nameof(BfsStrategyBase);

        public List<ICommand> MakeTrace(TModel model)
        {
            var impl = new Impl(model, maxBots);
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
            private int[,,] depth_;

            private readonly TState state;
            private List<Bot> bots;
            private readonly HashSet<TCoord> addedPositions;
            private readonly HashSet<TCoord> availablePositions;
            private readonly HashSet<TCoord> interferedCells;
            private int currentDepth;

            int Depth(TCoord c) => depth_[c.X, c.Y, c.Z];

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
                depth_ = new int[model.R, model.R, model.R];
                var queue = new Queue<TCoord>();
                var queue2 = new Queue<TCoord>();
                queue.Enqueue(new TCoord(0, 0, 0));
                depth_[0, 0, 0] = 1;
                while (queue.Count != 0)
                {
                    var cur = queue.Dequeue();
                    if (model[cur] != 0)
                    {
                        var d = Depth(cur);
                        foreach (var n in cur.ManhattenNeighbours().Where(n => n.IsValid(model.R) && (Depth(n) == 0)))
                        {
                            depth_[n.X, n.Y, n.Z] = d + 1;
                            queue.Enqueue(n);
                        }
                    }
                    else
                    {
                        var d = Depth(cur);
                        queue2.Enqueue(cur);
                        while (queue2.Count != 0)
                        {
                            var cur2 = queue2.Dequeue();
                            foreach (var n in cur2.ManhattenNeighbours().Where(n => n.IsValid(model.R) && (Depth(n) == 0)))
                            {
                                if (model[n] == 0)
                                {
                                    depth_[n.X, n.Y, n.Z] = d;
                                    queue2.Enqueue(n);
                                }
                                else
                                {
                                    depth_[n.X, n.Y, n.Z] = d + 1;
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

                    var newBots = new List<Bot>();

                    if ((availablePositions.Count == 0) && (bots.Count == 1) && bots[0].Coord.IsAtStart())
                    {
                        yield return new Halt();
                        yield break;
                    }

                    if (availablePositions.Count != 0)
                    {
                        currentDepth = availablePositions.Select(Depth).Max();
                    }

                    foreach (var bot in bots)
                    {
                        if ((bot.Target == null) || !CanMove(bot) || (bot.FillTarget != null && Depth(bot.FillTarget.Value) != currentDepth))
                        {
                            ChooseNewTarget(bot, newBots);
                        }

                        if ((bot.Target != null) && CanMove(bot))
                        {
                            idle = false;
                            var pc = bot.Coord;
                            yield return MoveBot(bot, newBots);

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

                    if (newBots.Count > 0)
                    {
                        // Console.WriteLine("HEY");
                        bots.AddRange(newBots);
                        bots = bots.OrderBy(bot => bot.Id).ToList();
                    }
                }
            }

            private bool CanMove(Bot bot)
            {
                if (bot.NextCommand < (bot.MoveCommands?.Count ?? 0))
                {
                    switch (bot.MoveCommands[bot.NextCommand])
                    {
                        case StraightMove m:
                            var good = true;
                            TraceMove(
                                bot.Coord,
                                m,
                                coord =>
                                {
                                    if (!IsFree(coord))
                                    {
                                        good = false;
                                    }
                                });
                            return good;
                        case LMove m:
                            throw new NotImplementedException();
                            break;
                        default:
                            throw new Exception("WTF");
                    }
                }

                return IsFree(bot.Target.Value);
            }

            private ICommand MoveBot(Bot bot, List<Bot> newBots)
            {
                if (bot.NextCommand < (bot.MoveCommands?.Count ?? 0))
                {
                    switch (bot.MoveCommands[bot.NextCommand])
                    {
                        case StraightMove m:
                            TraceMove(bot.Coord, m, coord => interferedCells.Add(coord));
                            bot.Coord.Apply(m.Diff);
                            ++bot.NextCommand;
                            return m;
                        case LMove m:
                            throw new NotImplementedException();
                        default:
                            throw new Exception("WTF");
                    }
                }

                if (bot.FissionTarget != null)
                {
                    interferedCells.Add(bot.FissionTarget.Value);
                    var m = bot.Seeds.Count / 2;
                    newBots.Add(
                        new Bot()
                        {
                            Id = bot.Seeds[0],
                            Coord = bot.FissionTarget.Value,
                            Seeds = bot.Seeds.Skip(1).Take(m).ToList()
                        });
                    bot.Seeds.RemoveRange(0, m + 1);

                    var ret = new Fission
                    {
                        Diff = bot.FissionTarget.Value.Diff(bot.Coord),
                        M = m,
                    };
                    bot.FissionTarget = null;
                    return ret;
                }

                if (bot.FillTarget != null)
                {
                    availablePositions.Remove(bot.FillTarget.Value);
                    state.Matrix[bot.FillTarget.Value.X, bot.FillTarget.Value.Y, bot.FillTarget.Value.Z] = 1;
                    foreach (var c in bot.FillTarget.Value.ManhattenNeighbours())
                    {
                        if (c.IsValid(model.R) && (model[c] != 0) && !addedPositions.Contains(c))
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
                
                if (bots.Count + newBots.Count < maxBots &&
                    bot.Seeds.Count > 0 &&
                    availablePositions.Count(p => Depth(p) == currentDepth) > bots.Count * 2)
                {
                    foreach (var coord in bot.Coord.NearNeighbours().Where(n => n.IsValid(model.R) && IsFree(n)))
                    {
                        bot.FissionTarget = coord;
                        return;
                    }
                }

                if (availablePositions.Count == 0)
                {
                    bot.MoveTarget = new TCoord(0, 0, 0);
                    foreach (var c in EnumerateReachablePaths(bot.Coord))
                    {
                        Console.WriteLine(c.Coord);
                        if (c.Coord.IsAtStart())
                        {
                            bot.MoveCommands = c.RecreatePath(bot.Coord);
                            bot.NextCommand = 0;
                            break;
                        }
                    }

                    if (bot.MoveCommands == null)
                    {
                        throw new Exception("NO PATH TO ORIGIN!");
                    }
                    
                    return;
                }

                foreach (var c in EnumerateReachablePaths(bot.Coord))
                {
                    if (Depth(c.Coord) < currentDepth)
                    {
                        var neighbours = c.Coord.ManhattenNeighbours()
                            .Where(
                                n =>
                                    n.IsValid(model.R) &&
                                    Depth(n) == currentDepth &&
                                    availablePositions.Contains(n));
                        foreach (var n in neighbours)
                        {
                            bot.FillTarget = n;
                            bot.MoveCommands = c.RecreatePath(bot.Coord);
                            return;
                        }

                    }
                }
                

                // Console.WriteLine($"COORDS: {bot.Coord}, TARGET: {bot.Target}, D: {(bot.Target == null ? -1 : Depth(bot.Target.Value))}, M: {bot.MoveCommands?.Count}");
            }

            private bool IsFree(TCoord coord) => (state.M(coord) == 0) && !interferedCells.Contains(coord);

            internal struct CellData
            {
                public TCoord From;
                public int Cost;

                public bool Visited => Cost != 0;
            }

            internal struct CoordWithPath
            {
                public readonly TCoord Coord;

                private readonly CellData[,,] cellData;

                public CoordWithPath(TCoord coord, CellData[,,] cellData)
                {
                    Coord = coord;
                    this.cellData = cellData;
                }

                public int Cost => cellData[Coord.X, Coord.Y, Coord.Z].Cost;

                public List<ICommand> RecreatePath(TCoord src)
                {
                    var path = new List<ICommand>();
                    var cur = Coord;
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

            private IEnumerable<CoordWithPath> EnumerateReachablePaths(TCoord src) {
                // THIS IS VERY DUMB ALGO. BECAUSE I'M TOO STUPID TO DO BETTER
                // TODO: support LMoves
                var cellData = new CellData[model.R, model.R, model.R];
                var queue = new Queue<TCoord>();
                queue.Enqueue(src);
                cellData[src.X, src.Y, src.Z].Cost = 1;

                yield return new CoordWithPath(src, cellData);
                while (queue.Count != 0)
                {
                    var cur = queue.Dequeue();

                    // TODO: smarter precompute?
                    var (minDx, maxDx) = FindRange(1, 0, 0);
                    var (minDy, maxDy) = FindRange(0, 1, 0);
                    var (minDz, maxDz) = FindRange(0, 0, 1);

                    var toEnqueue = new List<TCoord>();

                    // we visit (and yield) closes nodes first
                    for (var i = 0; i < Constants.StraightMoveCorrection; ++i)
                    {
                        foreach (var c in DoVisits(i))
                        {
                            yield return c;
                            toEnqueue.Add(c.Coord);
                        }
                    }

                    // but enqueue furthest first
                    toEnqueue.Reverse();
                    foreach (var c in toEnqueue)
                    {
                        queue.Enqueue(c);
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

                    IEnumerable<CoordWithPath> DoVisits(int dist)
                    {
                        foreach (var c in TryVisit(-dist >= minDx, dist, -dist, 0, 0)) yield return c;
                        foreach (var c in TryVisit(dist <= maxDx, dist, dist, 0, 0)) yield return c;
                        foreach (var c in TryVisit(-dist >= minDy, dist, 0, -dist, 0)) yield return c;
                        foreach (var c in TryVisit(dist <= maxDy, dist, 0, dist, 0)) yield return c;
                        foreach (var c in TryVisit(-dist >= minDz, dist, 0, 0, -dist)) yield return c;
                        foreach (var c in TryVisit(dist <= maxDz, dist, 0, 0, dist)) yield return c;
                    }

                    IEnumerable<CoordWithPath> TryVisit(bool ok, int dist, int dx, int dy, int dz)
                    {
                        if (!ok)
                        {
                            yield break;
                        }

                        var next = new TCoord(cur.X + dx, cur.Y + dy, cur.Z + dz);
                        var curCost = cellData[cur.X, cur.Y, cur.Z].Cost;
                        if (next.IsValid(model.R) && !cellData[next.X, next.Y, next.Z].Visited)
                        {
                            // TODO: add energy maintainance into cost
                            cellData[next.X, next.Y, next.Z].From = cur;
                            cellData[next.X, next.Y, next.Z].Cost = curCost + (2 * dist);
                            yield return new CoordWithPath(next, cellData);

                            // if (!IsFree(next)) throw new Exception("WTF");
                        }
                    }
                }
            }
        }

        private static void TraceMove(TCoord botCoord, StraightMove m, Action<TCoord> action)
        {
            var cur = botCoord;
            var dst = cur;
            dst.Apply(m.Diff);
            var step = new CoordDiff(Math.Sign(m.Diff.Dx), Math.Sign(m.Diff.Dy), Math.Sign(m.Diff.Dz));
            do
            {
                cur.Apply(step);
                action(cur);
            }
            while (!cur.Equals(dst));
        }
    }
}