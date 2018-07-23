namespace Solution.Strategies
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    [BrokenStrategy]
    public class StupidFissionFusionStategy : IStrategy
    {
        public string Name => nameof(StupidFissionFusionStategy);

        public List<ICommand> MakeTrace(TModel src, TModel model)
        {
            if (src.NumFilled != 0)
            {
                return null;
            }

            var rangeX = new Range();
            var rangeY = new Range();
            var rangeZ = new Range();

            ComputeBoundingBox(rangeX, rangeY, rangeZ, model);

            var result = new List<ICommand>();
            result.Add(new Flip());

            var midX = rangeX.Mean();
            var midY = rangeY.Mean();
            var midZ = rangeZ.Mean();

            var initialBs = new BotState { Position = new Coord() };

            while (true)
            {
                initialBs.Move(midX, midY, midZ);
                var cmd = initialBs.CurrentCommand;
                if (cmd == null)
                {
                    break;
                }

                result.Add(cmd);
            }

            result.Add(new Fission(0, 1, 0, 10));
            result.Add(new Fission(1, 0, 0, 5));
            result.Add(new Wait());

            var bss = new List<BotState>
            {
                new BotState { Position = new Coord(initialBs.Position.X, initialBs.Position.Y, initialBs.Position.Z) },
                new BotState { Position = new Coord(initialBs.Position.X, initialBs.Position.Y + 1, initialBs.Position.Z) },
                new BotState { Position = new Coord(initialBs.Position.X + 1, initialBs.Position.Y, initialBs.Position.Z) },
            };

            var r = model.R;
            for (var x = 0; x < r; ++x)
            {
                for (var y = 0; y < r; ++y)
                {
                    for (var z = 0; z < r; ++z)
                    {
                        if (model[x, y, z] > 0)
                        {
                            int botIndex;
                            if (y >= midY)
                            {
                                botIndex = 1;
                            }
                            else if (x <= midX)
                            {
                                botIndex = 0;
                            }
                            else
                            {
                                botIndex = 2;
                            }

                            var bot = bss[botIndex];
                            if (!bot.YToVoxels.ContainsKey(y))
                            {
                                bot.YToVoxels.Add(y, new HashSet<Coord>());
                            }

                            bot.YToVoxels[y].Add(new Coord(x, y, z));
                        }
                    }
                }
            }

            void Move(Action moveBots)
            {
                while (true)
                {
                    moveBots();

                    var cmds = bss.Select(x => x.CurrentCommand).ToArray();
                    if (cmds.All(x => x == null))
                    {
                        break;
                    }

                    foreach (var cmd in cmds)
                    {
                        result.Add(cmd == null ? new Wait() : cmd);
                    }
                }
            }

            Move(
                () =>
                {
                    bss[0].Move(0, 0, 0);
                    bss[1].Move(0, midY + 1, 0);
                    bss[2].Move(midX + 1, 0, 0);
                });

            bss[1].TopMost = true;
            Move(
                () =>
                {
                    foreach (var bs in bss)
                    {
                        bs.Fill(midY);
                    }
                });

            void MoveOneBot(int index, int toX, int toY, int toZ)
            {
                Move(
                    () =>
                    {
                        var bot = bss[index];
                        bot.Move(toX, toY, toZ);
                    });
            }

            MoveOneBot(0, 0, bss[0].Position.Y, 0);
            MoveOneBot(1, 0, bss[1].Position.Y, 0);
            MoveOneBot(0, 0, 0, 0);
            MoveOneBot(1, 0, 1, 0);
            MoveOneBot(2, rangeX.Max - 1, bss[2].Position.Y, rangeZ.Max);

            Move(() => bss[2].Finalize(model, rangeZ));

            MoveOneBot(2, 0, 2, 0);

            result.Add(new FusionP {Diff = new CoordDiff(0, 1, 0)});
            result.Add(new FusionS {Diff = new CoordDiff(0, -1, 0)});
            result.Add(new Wait());

            result.Add(new Wait());
            result.Add(new StraightMove { Diff = new CoordDiff(0, -1, 0) });

            result.Add(new FusionP {Diff = new CoordDiff(0, 1, 0)});
            result.Add(new FusionS {Diff = new CoordDiff(0, -1, 0)});

            result.Add(new Flip());
            result.Add(new Halt());

            return result;
        }

        private void ComputeBoundingBox(Range rangeX, Range rangeY, Range rangeZ, TModel model)
        {
            var r = model.R;
            for (var x = 0; x < r; ++x)
            {
                for (var y = 0; y < r; ++y)
                {
                    for (var z = 0; z < r; ++z)
                    {
                        if (model[x, y, z] > 0)
                        {
                            rangeX.Update(x);
                            rangeY.Update(y);
                            rangeZ.Update(z);
                        }
                    }
                }
            }
        }

        public class Coord : IEquatable<Coord>
        {
            public int X;
            public int Y;
            public int Z;

            public Coord()
            {
            }

            public Coord(int x, int y, int z)
            {
                X = x;
                Y = y;
                Z = z;
            }

            public override string ToString() => $"<{X}, {Y}, {Z}>";

            public int DistTo(Coord other)
            {
                return new CoordDiff
                {
                    Dx = X - other.X,
                    Dy = Y - other.Y,
                    Dz = Z - other.Z,
                }.MLen();
            }

            public bool Equals(Coord other) => X == other.X && Y == other.Y && Z == other.Z;

            public override bool Equals(object obj) => Equals((Coord)obj);

            public override int GetHashCode()
            {
                unchecked
                {
                    var hashCode = X;
                    hashCode = (hashCode * 397) ^ Y;
                    hashCode = (hashCode * 397) ^ Z;
                    return hashCode;
                }
            }
        }

        private class BotState
        {
            public Coord Position;
            public ICommand CurrentCommand;

            public Dictionary<int, HashSet<Coord>> YToVoxels = new Dictionary<int, HashSet<Coord>>();
            public Coord CurrentVoxel;
            public bool Halt;
            public int FinalDirection = -1;
            public HashSet<Coord> FinalCoords = new HashSet<Coord>();
            public bool TopMost;

            public void Move(int tgtX, int tgtY, int tgtZ)
            {
                int dx = 0;
                int dy = 0;
                int dz = 0;

                if (tgtX != Position.X)
                {
                    dx = GetMoveDelta(tgtX, Position.X);
                }
                else if (tgtY != Position.Y)
                {
                    dy = GetMoveDelta(tgtY, Position.Y);
                }
                else if (tgtZ != Position.Z)
                {
                    dz = GetMoveDelta(tgtZ, Position.Z);
                }

                if (dx != 0 || dy != 0 || dz != 0)
                {
                    CurrentCommand = new StraightMove
                    {
                        Diff = new CoordDiff(dx, dy, dz)
                    };
                    Position.X += dx;
                    Position.Y += dy;
                    Position.Z += dz;
                }
                else
                {
                    CurrentCommand = null;
                }
            }

            public void Fill(int midY)
            {
                if (Halt)
                {
                    CurrentCommand = null;
                    return;
                }

                if (CurrentVoxel != null)
                {
                    var target = new Coord(CurrentVoxel.X, CurrentVoxel.Y + 1, CurrentVoxel.Z);
                    if (target.Equals(Position))
                    {
                        CurrentCommand = new Fill(0, -1, 0);
                        CurrentVoxel = null;
                    }
                    else
                    {
                        Move(target.X, target.Y, target.Z);
                    }

                    return;
                }

                HashSet<Coord> voxelsToFill;
                YToVoxels.TryGetValue(Position.Y - 1, out voxelsToFill);
                if (voxelsToFill == null || voxelsToFill.Count == 0)
                {
                    var nextY = Position.Y + 1;
                    var shouldStop = !TopMost && nextY >= midY;
                    if (YToVoxels.Count > 0)
                    {
                        shouldStop |= nextY > YToVoxels.Keys.Max() + 1;
                    }
                    if (shouldStop)
                    {
                        CurrentCommand = null;
                        Halt = true;
                    }
                    else
                    {
                        Move(Position.X, nextY, Position.Z);
                    }
                    return;
                }

                Coord bestVoxel = null;
                int bestDistance = int.MaxValue;
                foreach (var voxel in voxelsToFill)
                {
                    var candDist = voxel.DistTo(Position);
                    if (bestVoxel == null || candDist < bestDistance)
                    {
                        bestVoxel = voxel;
                        bestDistance = candDist;
                    }
                }

                voxelsToFill.Remove(bestVoxel);
                CurrentVoxel = bestVoxel;
                CurrentCommand = new Wait();
            }

            public void Finalize(TModel model, Range rangeZ)
            {
                if (model[Position.X + 1, Position.Y, Position.Z] > 0)
                {
                    var toFill = new Coord(Position.X + 1, Position.Y, Position.Z);
                    if (!FinalCoords.Contains(toFill))
                    {
                        CurrentCommand = new Fill(1, 0, 0);
                        FinalCoords.Add(toFill);
                        return;
                    }
                }

                var nextZ = Position.Z + FinalDirection;
                if (nextZ < 0 || nextZ > rangeZ.Max)
                {
                    if (Position.X == 0)
                    {
                        CurrentCommand = null;
                    }
                    else
                    {
                        Move(Position.X - 1, Position.Y, Position.Z);
                        FinalDirection = -FinalDirection;
                    }

                    return;
                }

                Move(Position.X, Position.Y, nextZ);
            }

            private int GetMoveDelta(int needed, int have)
            {
                return Math.Sign(needed - have) * Math.Min(Constants.StraightMoveCorrection, Math.Abs(needed - have));
            }
        }

        private class Range
        {
            public int Min = int.MaxValue;
            public int Max = int.MinValue;

            public void Update(int coord)
            {
                Min = Math.Min(Min, coord);
                Max = Math.Max(Max, coord);
            }

            public int Mean()
            {
                return (Min + Max) / 2;
            }
        }
    }
}