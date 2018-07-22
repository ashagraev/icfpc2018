namespace Solution.Strategies
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using Solution.Strategies.BFS;

    // [BrokenStrategy]
    public abstract class BfsStrategyBase : IStrategy
    {
        private readonly int maxBots = 40;

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
                public TCoord? FusionTarget;
                public TCoord? FillTarget;
                public TCoord? MoveTarget;

                public TCoord? Target => FillTarget ?? FissionTarget ?? FusionTarget ?? MoveTarget;

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
            private readonly HashSet<TCoord> botPositions;
            private readonly HashSet<TCoord> interferedCells;
            private int currentDepth;
            private int numFilled = 0;

            private BFS.PathEnumerator PathEnumerator;

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
                for (var i = 2; i <= 40; ++i)
                {
                    bots[0].Seeds.Add(i);
                }

                addedPositions = new HashSet<TCoord>();
                availablePositions = new HashSet<TCoord>();
                botPositions = new HashSet<TCoord>();
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

                PathEnumerator = new PathEnumerator(model, state, depth_, interferedCells);
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
                    botPositions.Clear();
                    interferedCells.Clear();

                    foreach (var bot in bots)
                    {
                        botPositions.Add(bot.Coord);
                        interferedCells.Add(bot.Coord);
                    }

                    var newBots = new List<Bot>();
                    int? deadBotId = null;
                    var filledCoords = new List<TCoord>();

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
                            yield return MoveBot(bot, newBots, ref deadBotId, filledCoords);

                            // Console.WriteLine($"{pc} -> {bot.Coord}");
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
                            Console.WriteLine($"STUCK  {numFilled}/{model.NumFilled}");
                            /*
                            foreach (var b in bots)
                            {
                                ChooseNewTarget(b, newBots);
                                Console.WriteLine($"{b.Id}: {b.Coord}, T: {b.Target}");
                            }
                            */
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

                    if (deadBotId != null)
                    {
                        var liveBot = bots.First(IsPrimaryBotForFusion);
                        var deadBot = bots.First(bot => bot.Id == deadBotId);
                        liveBot.Seeds.Add(deadBot.Id);
                        deadBot.Seeds.AddRange(deadBot.Seeds);
                        bots = bots.Where(bot => bot.Id != deadBotId).ToList();
                    }

                    foreach (var c in filledCoords)
                    {
                        state.Matrix[c.X, c.Y, c.Z] = 1;
                        foreach (var n in c.ManhattenNeighbours())
                        {
                            if (n.IsValid(model.R) && model[n] != 0 && !addedPositions.Contains(n))
                            {
                                addedPositions.Add(n);
                                availablePositions.Add(n);
                            }
                        }
                    }

                    if (filledCoords.Count != 0)
                    {
                        numFilled += filledCoords.Count;
                        Console.WriteLine($"  {numFilled} / {model.NumFilled}. {availablePositions.Count(p => Depth(p) == currentDepth)} for {bots.Count} bots");
                        /*
                        foreach (var c in availablePositions.Where(c => Depth(c) == currentDepth))
                        {
                            Console.WriteLine($"  A: {c}");
                        }
                        */
                    }
                }
            }

            private bool CanMove(Bot bot)
            {
                if (bot.FusionTarget != null)
                {
                    return true;
                }

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

            private ICommand MoveBot(Bot bot, List<Bot> newBots, ref int? deadBotId, List<TCoord> filledCoords)
            {
                if (bot.MoveTarget != null && bot.MoveTarget.Value.Equals(bot.Coord))
                {
                    bot.MoveTarget = null;
                    return new Wait();
                }

                if (bot.NextCommand < (bot.MoveCommands?.Count ?? 0))
                {
                    switch (bot.MoveCommands[bot.NextCommand])
                    {
                        case StraightMove m:
                            TraceMove(bot.Coord, m, coord => interferedCells.Add(coord));
                            bot.Coord.Apply(m.Diff);
                            ++bot.NextCommand;
                            if (bot.MoveTarget != null && bot.NextCommand == bot.MoveCommands.Count)
                            {
                                bot.MoveTarget = null;
                            }
                            return m;
                        case LMove m:
                            throw new NotImplementedException();
                        default:
                            throw new Exception($"WTF, unexpected command: {bot.GetType().FullName}");
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

                if (bot.FusionTarget != null)
                {
                    interferedCells.Add(bot.FusionTarget.Value);

                    ICommand ret;
                    var diff = bot.FusionTarget.Value.Diff(bot.Coord);
                    if (IsPrimaryBotForFusion(bot))
                    {
                        ret = new FusionP { Diff = diff };
                    }
                    else
                    {
                        deadBotId = bot.Id;
                        ret = new FusionS { Diff = diff };
                    }

                    bot.FusionTarget = null;
                    return ret;
                }

                if (bot.FillTarget != null)
                {
                    interferedCells.Add(bot.FillTarget.Value);
                    availablePositions.Remove(bot.FillTarget.Value);
                    filledCoords.Add(bot.FillTarget.Value);

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
                    availablePositions.Count(p => Depth(p) == currentDepth) > (bots.Count + newBots.Count) * 2)
                {
                    foreach (var coord in bot.Coord.NearNeighbours().Where(n => n.IsValid(model.R) && IsFree(n) && Depth(n) < currentDepth))
                    {
                        bot.FissionTarget = coord;
                        return;
                    }
                }

                if (availablePositions.Count == 0)
                {
                    Fuse(bot);

                    if (bot.MoveCommands == null)
                    {
                        return;
                        throw new Exception("NO PATH TO ORIGIN!");
                    }

                    return;
                }
                
                foreach (var c in PathEnumerator.EnumerateReachablePaths(bot.Coord, currentDepth))
                {
                    if (Depth(c.Coord) < currentDepth)
                    {
                        var neighbours = c.Coord.ManhattenNeighbours()
                            .Where(
                                n =>
                                    n.IsValid(model.R) &&
                                    Depth(n) == currentDepth &&
                                    availablePositions.Contains(n) &&
                                    (!botPositions.Contains(n)));
                        foreach (var n in neighbours)
                        {
                            bot.FillTarget = n;
                            bot.MoveCommands = c.RecreatePath(bot.Coord);
                            // Console.WriteLine($"COORDS: {bot.Id}@{bot.Coord}, FILL: {bot.Target}, D: {(bot.Target == null ? -1 : Depth(bot.Target.Value))}, M: {bot.MoveCommands?.Count} " + string.Join(", ", bot.MoveCommands ?? new List<ICommand>()));
                            return;
                        }
                    }
                }

                if (!availablePositions.Contains(bot.Coord) || Depth(bot.Coord) != currentDepth)
                {
                    foreach (var c in PathEnumerator.EnumerateReachablePaths(bot.Coord, currentDepth).Where(c => Depth(c.Coord) == currentDepth - 2))
                    {
                        bot.MoveTarget = c.Coord;
                        bot.MoveCommands = c.RecreatePath(bot.Coord);
                        // Console.WriteLine($"COORDS: {bot.Id}@{bot.Coord}, MOVE: {bot.Target}, D: {(bot.Target == null ? -1 : Depth(bot.Target.Value))}, M: {bot.MoveCommands?.Count} "+ string.Join(", ", bot.MoveCommands ?? new List<ICommand>()));
                        return;
                    }
                }
            }

            private void Fuse(Bot bot)
            {
                if (bots.Count == 0)
                {
                    throw new Exception("Where did the bots go?");
                }

                if (bots.Count == 1)
                {
                    // All done here.
                    return;
                }

                var firstTwoBots = bots.OrderBy(x => x.Id).Take(2).ToArray();
                var (primary, secondary) = (firstTwoBots[0], firstTwoBots[1]);

                if (bot.Id != primary.Id && bot.Id != secondary.Id)
                {
                    // Our time has not come yet.
                    return;
                }

                var primaryDest = new TCoord(0, 0, 0);
                var secondaryDest = new TCoord(0, 1, 0);
                if (primary.Coord.Equals(primaryDest) && secondary.Coord.Equals(secondaryDest))
                {
                    // Let's fuse!
                    primary.FusionTarget = secondaryDest;
                    secondary.FusionTarget = primaryDest;
                    return;
                }

                var destCoord = bot.Id == primary.Id ? primaryDest : secondaryDest;

                bot.MoveTarget = destCoord;
                foreach (var c in PathEnumerator.EnumerateReachablePaths(bot.Coord, 1000))
                {
                    if (c.Coord.Equals(destCoord))
                    {
                        bot.MoveCommands = c.RecreatePath(bot.Coord);
                        bot.NextCommand = 0;
                        break;
                    }
                }
            }

            private bool IsPrimaryBotForFusion(Bot bot) => bot.Coord.IsAtStart();
            private bool IsFree(TCoord coord) => (state.M(coord) == 0) && !interferedCells.Contains(coord);

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
}