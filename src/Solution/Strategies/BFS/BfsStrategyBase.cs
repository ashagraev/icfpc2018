namespace Solution.Strategies
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using Solution.Strategies.BFS;

    public abstract class BfsStrategyBase : IStrategy
    {
        private readonly int maxBots;

        internal const bool Trace = false;

        public virtual string Name => nameof(BfsStrategyBase);

        protected BfsStrategyBase(int maxBots)
        {
            this.maxBots = maxBots;
        }

        public List<ICommand> MakeTrace(TModel src, TModel model)
        {
            if (src.NumFilled != 0)
            {
                return null;
            }

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
                public TCoord? FusionPTarget;
                public TCoord? FusionSTarget;
                public TCoord? FillTarget;
                public TCoord? MoveTarget;

                // fusions what were already acted
                public TCoord? ActedFusionPTarget;
                public TCoord? ActedFusionSTarget;

                public TCoord? Target => FillTarget ?? FissionTarget ?? FusionPTarget ?? FusionSTarget ?? MoveTarget;

                // a trace to some cell near target
                public List<ICommand> MoveCommands;
                public int NextCommand;

                public bool Acted = false;
                public bool MustDie = false; // marks fused bots
            }

            private readonly TModel model;
            private readonly int maxBots;
            private int[,,] depth_;

            private readonly TState state;
            private List<Bot> bots;
            private readonly HashSet<TCoord> addedPositions;
            private readonly HashSet<TCoord> availablePositions;
            private readonly Dictionary<TCoord, Bot> botPositions;
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
                botPositions = new Dictionary<TCoord, Bot>();
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
                        botPositions.Add(bot.Coord, bot);
                        interferedCells.Add(bot.Coord);
                    }

                    var newBots = new List<Bot>();
                    int numDeadBots = 0;
                    var filledCoords = new List<TCoord>();
                    int numPlannedFusions = bots.Count(b => b.FusionSTarget != null);

                    foreach (var b in bots)
                    {
                        b.Acted = false;
                        b.MustDie = false;
                        b.ActedFusionPTarget = null;
                        b.ActedFusionSTarget = null;
                    }

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
                            ChooseNewTarget(bot, newBots, ref numPlannedFusions);
                        }

                        if ((bot.Target != null) && CanMove(bot))
                        {
                            idle = false;
                            var pc = bot.Coord;
                            yield return MoveBot(bot, newBots, ref numDeadBots, filledCoords);

                            if (Trace)
                            {
                                Console.WriteLine($"{bot.Id} move {pc} -> {bot.Coord} ({!bot.MustDie}, {numDeadBots})");
                            }
                        }
                        else
                        {
                            if (Trace)
                            {
                                Console.WriteLine($"{bot.Id} waits at {bot.Coord}");
                            }

                            yield return new Wait();
                        }

                        bot.Acted = true;
                    }

                    if (Trace)
                    {
                        Console.WriteLine("-----");
                    }

                    if (idle)
                    {
                        // we are stuck. Just produce some garbage trace
                        if (++idleSteps >= 2)
                        {
                            Console.WriteLine($"STUCK  {numFilled}/{model.NumFilled}");
                            if (Trace)
                            {
                                foreach (var b in bots)
                                {
                                    ChooseNewTarget(b, newBots, ref numPlannedFusions);
                                    Console.WriteLine($"{b.Id}: {b.Coord}, T: {b.Target}");
                                }
                            }

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

                    if (numDeadBots != 0)
                    {
                        bots = bots.Where(bot => !bot.MustDie).ToList();

                        foreach (var b in bots)
                        {
                            var seeds = string.Join(",", b.Seeds);
                            if (Trace)
                            {
                                Console.WriteLine($"{b.Id}: {b.Coord}, S: {seeds}");
                            }
                        }
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
                        // Console.WriteLine($"  {numFilled} / {model.NumFilled}. {availablePositions.Count(p => Depth(p) == currentDepth)} for {bots.Count} bots");
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
                        case Wait w:
                            return true;
                        case LMove m:
                            throw new NotImplementedException();
                            break;
                        default:
                            throw new Exception("WTF");
                    }
                }

                if (bot.FusionPTarget != null)
                {
                    Bot another;
                    return botPositions.TryGetValue(bot.FusionPTarget.Value, out another) &&
                           (another.FusionSTarget ?? another.ActedFusionSTarget) != null &&
                           (another.FusionSTarget ?? another.ActedFusionSTarget).Value.Equals(bot.Coord);
                }

                if (bot.FusionSTarget != null)
                {
                    Bot another;
                    return botPositions.TryGetValue(bot.FusionSTarget.Value, out another) &&
                           (another.FusionPTarget ?? another.ActedFusionPTarget) != null &&
                           (another.FusionPTarget ?? another.ActedFusionPTarget).Value.Equals(bot.Coord);
                }

                return IsFree(bot.Target.Value);
            }

            private ICommand MoveBot(Bot bot, List<Bot> newBots, ref int numDeadBots, List<TCoord> filledCoords)
            {
                if (bot.NextCommand < (bot.MoveCommands?.Count ?? 0))
                {
                    switch (bot.MoveCommands[bot.NextCommand])
                    {
                        case StraightMove m:
                            TraceMove(bot.Coord, m, coord => interferedCells.Add(coord));
                            bot.Coord.Apply(m.Diff);
                            break;
                        case LMove m:
                            throw new NotImplementedException();
                        case Wait w:
                            break;
                        default:
                            throw new Exception($"WTF, unexpected command: {bot.GetType().FullName}");
                    }

                    ++bot.NextCommand;
                    if (bot.MoveTarget != null && bot.NextCommand == bot.MoveCommands.Count)
                    {
                        bot.MoveTarget = null;
                    }

                    return bot.MoveCommands[bot.NextCommand - 1];
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

                if (bot.FusionPTarget != null)
                {
                    interferedCells.Add(bot.FusionPTarget.Value);
                    if (botPositions.TryGetValue(bot.FusionPTarget.Value, out var another))
                    {
                        var ret = new FusionP { Diff = bot.FusionPTarget.Value.Diff(bot.Coord) };

                        bot.Seeds.Add(another.Id);
                        bot.Seeds.AddRange(another.Seeds);
                        bot.Seeds.Sort();
                        bot.ActedFusionPTarget = bot.FusionPTarget;
                        bot.FusionPTarget = null;

                        return ret;
                    }

                    throw new InvalidOperationException("Trying to run fusion on not a bot cell!");
                }
                else if (bot.FusionSTarget != null)
                {
                    var ret = new FusionS { Diff = bot.FusionSTarget.Value.Diff(bot.Coord) };

                    interferedCells.Add(bot.FusionSTarget.Value);
                    ++numDeadBots;
                    bot.MustDie = true;
                    interferedCells.Add(bot.FusionSTarget.Value);
                    bot.ActedFusionSTarget = bot.FusionSTarget;
                    bot.FusionSTarget = null;

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

                throw new Exception($"WTF {numFilled} / {model.NumFilled}");
            }

            private void ChooseNewTarget(Bot bot, List<Bot> newBots, ref int numPlannedFusions)
            {
                if (bot.FusionSTarget != null)
                {
                    throw new Exception($"Impossible: fusionS target should always be actionable... {bot.Id}");
                }

                if (bot.FusionPTarget != null)
                {
                    // remove actions from a friend also
                    if (botPositions.TryGetValue(bot.FusionPTarget.Value, out var another) && another.FusionSTarget != null)
                    {
                        bot.FusionPTarget = null;

                        another.FusionSTarget = null;
                        another.NextCommand = 0;
                        another.MoveCommands = null;
                    }
                    else
                    {
                        throw new Exception($"Impossible: secondary bot should always be waiting inplace! Only primary can cancel fusion... {bot.Id}");
                    }
                }

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

                if (availablePositions.Count == 0 && bots.Count == 1)
                {
                    foreach (var c in PathEnumerator.EnumerateReachablePaths(bot.Coord, 1000))
                    {
                        if (c.Coord.IsAtStart())
                        {
                            bot.MoveTarget = c.Coord;
                            bot.NextCommand = 0;
                            bot.MoveCommands = c.RecreatePath(bot.Coord);
                            return;
                        }
                    }

                    throw new Exception("No path to origin found for last bot!");
                }

                if (availablePositions.Count * 2 < (bots.Count - numPlannedFusions))
                {
                    Fuse(bot);
                    ++numPlannedFusions;
                    return;
                }

                foreach (var c in PathEnumerator.EnumerateReachablePaths(bot.Coord, currentDepth))
                {
                    if (Depth(c.Coord) < currentDepth)
                    {
                        var neighbours = c.Coord.NearNeighbours()
                            .Where(
                                n =>
                                    n.IsValid(model.R) &&
                                    Depth(n) == currentDepth &&
                                    availablePositions.Contains(n) &&
                                    (!botPositions.ContainsKey(n)));
                        foreach (var n in neighbours)
                        {
                            bot.FillTarget = n;
                            bot.MoveCommands = c.RecreatePath(bot.Coord);
                            if (Trace)
                            {
                                Console.WriteLine(
                                    $"COORDS: {bot.Id}@{bot.Coord}, FILL: {bot.Target}, D: {(bot.Target == null ? -1 : Depth(bot.Target.Value))}, M: {bot.MoveCommands?.Count} "
                                    + string.Join(", ", bot.MoveCommands ?? new List<ICommand>()));
                            }

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
                        if (Trace)
                        {
                            Console.WriteLine(
                                $"COORDS: {bot.Id}@{bot.Coord}, MOVE: {bot.Target}, D: {(bot.Target == null ? -1 : Depth(bot.Target.Value))}, M: {bot.MoveCommands?.Count} "
                                + string.Join(", ", bot.MoveCommands ?? new List<ICommand>()));
                        }

                        return;
                    }
                }
            }

            private void Fuse(Bot bot)
            {
                foreach (var c in PathEnumerator.EnumerateReachablePaths(bot.Coord, 1000))
                {
                    foreach (var n in c.Coord.NearNeighbours().Where(n => n.IsValid(model.R)))
                    {
                        if (botPositions.TryGetValue(n, out var another))
                        {
                            if (another.Target == null && !another.Acted && another.Id != bot.Id)
                            {
                                // come and eat!
                                bot.FusionPTarget = n;
                                bot.NextCommand = 0;
                                bot.MoveCommands = c.RecreatePath(bot.Coord);

                                // just wait!
                                another.FusionSTarget = c.Coord;
                                another.NextCommand = 0;
                                another.MoveCommands = bot.MoveCommands.Select(_ => (ICommand)new Wait()).ToList();

                                if (Trace)
                                {
                                    Console.WriteLine(
                                        $"COORDS: {bot.Id}@{bot.Coord}, FUSEP: {bot.Target}, D: {(bot.Target == null ? -1 : Depth(bot.Target.Value))}, M: {bot.MoveCommands?.Count} "
                                        + string.Join(", ", bot.MoveCommands ?? new List<ICommand>()));

                                    Console.WriteLine(
                                        $"COORDS: {another.Id}@{another.Coord}, FUSES: {another.Target}, D: {(another.Target == null ? -1 : Depth(another.Target.Value))}, M: {another.MoveCommands?.Count} "
                                        + string.Join(", ", another.MoveCommands ?? new List<ICommand>()));
                                }


                                return;
                            }
                        }
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