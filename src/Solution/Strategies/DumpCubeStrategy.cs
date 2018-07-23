namespace Solution.Strategies
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net.Sockets;

    using Void = Solution.Void;

    public class TBorders
    {
        public int MinX = 1000000;
        public int MinZ = 1000000;

        public int MaxX;
        public int MaxZ;
    }

    public class TDumpCubeTraverse
    {
        private readonly List<TBorders> LevelBorders;

        private CoordDiff Direction;
        private TCoord Current = new TCoord();

        private readonly int R;
        private readonly int MaxY;

        private bool LetGoBack;
        private bool SearchStartPosition = true;

        private bool JustStarted = true;
        private bool Enough = false;

        public TDumpCubeTraverse(TModel model)
        {
            R = model.R;

            LevelBorders = new List<TBorders>();

            for (var y = 0; y < R; ++y)
            {
                var thisLevelBorders = new TBorders();
                for (var x = 0; x < R; ++x)
                {
                    for (var z = 0; z < R; ++z)
                    {
                        if (model[x, y, z] > 0)
                        {
                            MaxY = Math.Max(MaxY, y);

                            thisLevelBorders.MinX = Math.Min(thisLevelBorders.MinX, x);
                            thisLevelBorders.MaxX = Math.Max(thisLevelBorders.MaxX, x);

                            thisLevelBorders.MinZ = Math.Min(thisLevelBorders.MinZ, z);
                            thisLevelBorders.MaxZ = Math.Max(thisLevelBorders.MaxZ, z);
                        }
                    }
                }

                if (thisLevelBorders.MaxX == thisLevelBorders.MinX)
                {
                    ++thisLevelBorders.MaxX;
                    --thisLevelBorders.MinX;
                }
                if (thisLevelBorders.MaxZ == thisLevelBorders.MinZ)
                {
                    ++thisLevelBorders.MaxZ;
                    --thisLevelBorders.MinZ;
                }

                LevelBorders.Add(thisLevelBorders);
            }
        }

        public TDumpCubeTraverse(TModel srcModel, TModel dstModel)
        {
            R = srcModel.R;

            LevelBorders = new List<TBorders>();

            for (var y = 0; y < R; ++y)
            {
                var thisLevelBorders = new TBorders();
                for (var x = 0; x < R; ++x)
                {
                    for (var z = 0; z < R; ++z)
                    {
                        if (srcModel[x, y, z] > 0 || dstModel[x, y, z] > 0)
                        {
                            MaxY = Math.Max(MaxY, y);

                            thisLevelBorders.MinX = Math.Min(thisLevelBorders.MinX, x);
                            thisLevelBorders.MaxX = Math.Max(thisLevelBorders.MaxX, x);

                            thisLevelBorders.MinZ = Math.Min(thisLevelBorders.MinZ, z);
                            thisLevelBorders.MaxZ = Math.Max(thisLevelBorders.MaxZ, z);
                        }
                    }
                }

                if (thisLevelBorders.MaxX == thisLevelBorders.MinX)
                {
                    ++thisLevelBorders.MaxX;
                    --thisLevelBorders.MinX;
                }
                if (thisLevelBorders.MaxZ == thisLevelBorders.MinZ)
                {
                    ++thisLevelBorders.MaxZ;
                    --thisLevelBorders.MinZ;
                }

                LevelBorders.Add(thisLevelBorders);
            }
        }

        public CoordDiff GetDirection() => Direction;

        public CoordDiff FillPreviousDirection()
        {
            var fillDirection = Direction;
            fillDirection.Dx = -fillDirection.Dx;
            fillDirection.Dy = -fillDirection.Dy;
            fillDirection.Dz = -fillDirection.Dz;
            return fillDirection;
        }

        private TCoord NextForReturn()
        {
            if (Current.X > 0)
            {
                Direction.Dx = -1;
                Direction.Dy = 0;
                Direction.Dz = 0;
            }
            else if (Current.Z > 0)
            {
                Direction.Dx = 0;
                Direction.Dy = 0;
                Direction.Dz = -1;
            }
            else
            {
                Direction.Dx = 0;
                Direction.Dy = -1;
                Direction.Dz = 0;
            }

            Current.Apply(Direction);
            return Current;
        }

        public TCoord NextDestroy()
        {
            if (!JustStarted && Current.Y == 0)
            {
                return NextForReturn();
            }

            JustStarted = false;
            if (!Enough && Current.Y != MaxY + 1)
            {
                Direction.Dx = 0;
                Direction.Dy = 1;
                Direction.Dz = 0;
                Current.Apply(Direction);
                return Current;
            }
            else if (!Enough)
            {
                SearchStartPosition = true;
                Enough = true;
            }

            var borders = LevelBorders[Current.Y - 1];

            if (SearchStartPosition)
            {
                var targetX = ((Current.Y - (MaxY + 1)) % 2 == 0) ? borders.MinX : borders.MaxX;
                var targetZ = ((Current.Y - (MaxY + 1)) % 2 == 0) ? borders.MinZ : borders.MaxZ;

                if (Current.X < targetX)
                {
                    Direction.Dx = 1;
                    Direction.Dy = 0;
                    Direction.Dz = 0;
                    Current.Apply(Direction);
                    return Current;
                }

                if (Current.Z < targetZ)
                {
                    Direction.Dx = 0;
                    Direction.Dy = 0;
                    Direction.Dz = 1;
                    Current.Apply(Direction);
                    return Current;
                }

                if (Current.X > targetX)
                {
                    Direction.Dx = -1;
                    Direction.Dy = 0;
                    Direction.Dz = 0;
                    Current.Apply(Direction);
                    return Current;
                }

                if (Current.Z > targetZ)
                {
                    Direction.Dx = 0;
                    Direction.Dy = 0;
                    Direction.Dz = -1;
                    Current.Apply(Direction);
                    return Current;
                }

                SearchStartPosition = false;
            }

            if ((Current.Y - (MaxY + 1)) % 2 == 0)
            {
                if ((Current.X > borders.MaxX) && (Current.Z > borders.MaxZ)) // end of life
                {
                    Direction.Dx = 0;
                    Direction.Dy = -1;
                    Direction.Dz = 0;
                    SearchStartPosition = true;
                }
                else if ((Current.X == borders.MinX) && (Current.Z == borders.MinZ)) // walk forward X
                {
                    Direction.Dx = 1;
                    Direction.Dy = 0;
                    Direction.Dz = 0;
                }
                else if ((Direction.Dx == 1) && (Current.X > borders.MaxX)) // one step forward Z
                {
                    Direction.Dx = 0;
                    Direction.Dy = 0;
                    Direction.Dz = 1;
                }
                else if ((Direction.Dz == 1) && (Current.X > borders.MaxX)) // walk backward X
                {
                    Direction.Dx = -1;
                    Direction.Dy = 0;
                    Direction.Dz = 0;
                }
                else if ((Direction.Dx == -1) && (Current.X < borders.MinX)) // one step forward Z
                {
                    Direction.Dx = 0;
                    Direction.Dy = 0;
                    Direction.Dz = 1;
                }
                else if ((Direction.Dz == 1) && (Current.X < borders.MinX)) // walk again forward X
                {
                    Direction.Dx = 1;
                    Direction.Dy = 0;
                    Direction.Dz = 0;
                }
            }
            else
            {
                if ((Current.X < borders.MinX) && (Current.Z < borders.MinZ)) // end of life
                {
                    Direction.Dx = 0;
                    Direction.Dy = -1;
                    Direction.Dz = 0;
                    SearchStartPosition = true;
                }
                else if ((Current.X == borders.MaxX) && (Current.Z == borders.MaxZ)) // walk backward X
                {
                    Direction.Dx = -1;
                    Direction.Dy = 0;
                    Direction.Dz = 0;
                }
                else if ((Direction.Dx == -1) && (Current.X < borders.MinX)) // one step backward Z
                {
                    Direction.Dx = 0;
                    Direction.Dy = 0;
                    Direction.Dz = -1;
                }
                else if ((Direction.Dz == -1) && (Current.X < borders.MinX)) // walk forward X
                {
                    Direction.Dx = 1;
                    Direction.Dy = 0;
                    Direction.Dz = 0;
                }
                else if ((Direction.Dx == 1) && (Current.X > borders.MaxX)) // one step backward Z
                {
                    Direction.Dx = 0;
                    Direction.Dy = 0;
                    Direction.Dz = -1;
                }
                else if ((Direction.Dz == -1) && (Current.X > borders.MaxX)) // walk again backward X
                {
                    Direction.Dx = -1;
                    Direction.Dy = 0;
                    Direction.Dz = 0;
                }
            }

            Current.Apply(Direction);
            return Current;
        }

        public TCoord Next()
        {
            if (LetGoBack || (Current.Y > MaxY + 1))
            {
                LetGoBack = true;
                return NextForReturn();
            }

            if (Current.Y == 0)
            {
                Direction.Dx = 0;
                Direction.Dy = 1;
                Direction.Dz = 0;
                Current.Apply(Direction);
                return Current;
            }

            var borders = LevelBorders[Current.Y - 1];

            if (SearchStartPosition)
            {
                var targetX = Current.Y % 2 == 1 ? borders.MinX : borders.MaxX;
                var targetZ = Current.Y % 2 == 1 ? borders.MinZ : borders.MaxZ;

                if (Current.X < targetX)
                {
                    Direction.Dx = 1;
                    Direction.Dy = 0;
                    Direction.Dz = 0;
                    Current.Apply(Direction);
                    return Current;
                }

                if (Current.Z < targetZ)
                {
                    Direction.Dx = 0;
                    Direction.Dy = 0;
                    Direction.Dz = 1;
                    Current.Apply(Direction);
                    return Current;
                }

                if (Current.X > targetX)
                {
                    Direction.Dx = -1;
                    Direction.Dy = 0;
                    Direction.Dz = 0;
                    Current.Apply(Direction);
                    return Current;
                }

                if (Current.Z > targetZ)
                {
                    Direction.Dx = 0;
                    Direction.Dy = 0;
                    Direction.Dz = -1;
                    Current.Apply(Direction);
                    return Current;
                }

                SearchStartPosition = false;
            }

            if (Current.Y % 2 == 1)
            {
                if ((Current.X > borders.MaxX) && (Current.Z > borders.MaxZ)) // end of life
                {
                    Direction.Dx = 0;
                    Direction.Dy = 1;
                    Direction.Dz = 0;
                    SearchStartPosition = true;
                }
                else if ((Current.X == borders.MinX) && (Current.Z == borders.MinZ)) // walk forward X
                {
                    Direction.Dx = 1;
                    Direction.Dy = 0;
                    Direction.Dz = 0;
                }
                else if ((Direction.Dx == 1) && (Current.X > borders.MaxX)) // one step forward Z
                {
                    Direction.Dx = 0;
                    Direction.Dy = 0;
                    Direction.Dz = 1;
                }
                else if ((Direction.Dz == 1) && (Current.X > borders.MaxX)) // walk backward X
                {
                    Direction.Dx = -1;
                    Direction.Dy = 0;
                    Direction.Dz = 0;
                }
                else if ((Direction.Dx == -1) && (Current.X < borders.MinX)) // one step forward Z
                {
                    Direction.Dx = 0;
                    Direction.Dy = 0;
                    Direction.Dz = 1;
                }
                else if ((Direction.Dz == 1) && (Current.X < borders.MinX)) // walk again forward X
                {
                    Direction.Dx = 1;
                    Direction.Dy = 0;
                    Direction.Dz = 0;
                }
            }
            else if (Current.Y % 2 == 0)
            {
                if ((Current.X < borders.MinX) && (Current.Z < borders.MinZ)) // end of life
                {
                    Direction.Dx = 0;
                    Direction.Dy = 1;
                    Direction.Dz = 0;
                    SearchStartPosition = true;
                }
                else if ((Current.X == borders.MaxX) && (Current.Z == borders.MaxZ)) // walk backward X
                {
                    Direction.Dx = -1;
                    Direction.Dy = 0;
                    Direction.Dz = 0;
                }
                else if ((Direction.Dx == -1) && (Current.X < borders.MinX)) // one step backward Z
                {
                    Direction.Dx = 0;
                    Direction.Dy = 0;
                    Direction.Dz = -1;
                }
                else if ((Direction.Dz == -1) && (Current.X < borders.MinX)) // walk forward X
                {
                    Direction.Dx = 1;
                    Direction.Dy = 0;
                    Direction.Dz = 0;
                }
                else if ((Direction.Dx == 1) && (Current.X > borders.MaxX)) // one step backward Z
                {
                    Direction.Dx = 0;
                    Direction.Dy = 0;
                    Direction.Dz = -1;
                }
                else if ((Direction.Dz == -1) && (Current.X > borders.MaxX)) // walk again backward X
                {
                    Direction.Dx = -1;
                    Direction.Dy = 0;
                    Direction.Dz = 0;
                }
            }

            Current.Apply(Direction);
            return Current;
        }
    }

    public class DumpCubeStrategy : IStrategy
    {
        public string Name => nameof(DumpCubeStrategy);

        public List<ICommand> MakeTrace(TModel model)
        {
            ICommand modifyCommand = new Fill();
            ((Fill)modifyCommand).Diff.Dy = -1;
            if (model.Name.Contains("FD"))
            {
                modifyCommand = new Void();
                ((Void)modifyCommand).Diff.Dy = -1;
            }

      //      TState state = new TState(model);

            var result = new List<ICommand>();

            result.Add(new Flip());

            var current = new TCoord();
            var dumpCureTraverse = new TDumpCubeTraverse(model);

            var iteration = 0;
            while ((iteration == 0) || !current.IsAtStart())
            {
                if (iteration == 295)
                {
                    int a = 0;
                }

                var next = model.Name.Contains("FA") ? dumpCureTraverse.Next() : dumpCureTraverse.NextDestroy();
                var move = new StraightMove();
                move.Diff = dumpCureTraverse.GetDirection();
                result.Add(move);
 //               {
 //                   List<ICommand> ss = new List<ICommand>();
   //                 ss.Add(move);
   //                 TCommandsReader cr = new TCommandsReader(ss);
   //                 state.Step(cr);
   //             }

                if ((next.Y > 0) && (model[next.X, next.Y - 1, next.Z] > 0))
                {
                    result.Add(modifyCommand);
      //              {
      //                  List<ICommand> ss = new List<ICommand>();
       //                 ss.Add(move);
       //                 TCommandsReader cr = new TCommandsReader(ss);
       //                 state.Step(cr);
        //            }
                }

                current = next;
                ++iteration;
            }

            result.Add(new Flip());
            result.Add(new Halt());

            return result;
        }

        public List<ICommand> MakeReassemblyTrace(TModel srcModel, TModel tgtModel)
        {
            Fill doFill = new Fill();
            doFill.Diff.Dy = -1;

            Void doVoid = new Void();
            doVoid.Diff.Dy = -1;

            var result = new List<ICommand>();

            result.Add(new Flip());

            var current = new TCoord();
            var dumpCureTraverse = new TDumpCubeTraverse(srcModel, tgtModel);

            TState state = new TState(srcModel);
            List<ICommand> ss = new List<ICommand>();

            var iteration = 0;
            while ((iteration == 0) || !current.IsAtStart())
            {
                var next = dumpCureTraverse.Next();

                if (srcModel[next] > 0)
                {
                    Void curVoid = new Void();
                    curVoid.Diff = dumpCureTraverse.GetDirection();
                    result.Add(curVoid);

                    {
                        if (ss.Count == 0)
                        {
                            ss.Add(curVoid);
                        }
                        else
                        {
                            ss[0] = curVoid;
                        }
                        ss.Add(curVoid);
                        TCommandsReader cr = new TCommandsReader(ss);
                        state.Step(cr);
                    }
                }

                var move = new StraightMove();
                move.Diff = dumpCureTraverse.GetDirection();
                result.Add(move);

                {
                    if (ss.Count == 0)
                    {
                        ss.Add(move);
                    }
                    else
                    {
                        ss[0] = move;
                    }

                    TCommandsReader cr = new TCommandsReader(ss);
                    state.Step(cr);
                }

                if ((next.Y > 0) && (tgtModel[next.X, next.Y - 1, next.Z] > 0) && state.Matrix[next.X, next.Y - 1, next.Z] == 0)
                {
                    result.Add(doFill);
                    {
                        if (ss.Count == 0)
                        {
                            ss.Add(doFill);
                        }
                        else
                        {
                            ss[0] = doFill;
                        }
                        TCommandsReader cr = new TCommandsReader(ss);
                        state.Step(cr);
                    }
                }
                if ((next.Y > 0) && (tgtModel[next.X, next.Y - 1, next.Z] == 0) && state.Matrix[next.X, next.Y - 1, next.Z] > 0)
                {
                    result.Add(doVoid);
                    {
                        if (ss.Count == 0)
                        {
                            ss.Add(doVoid);
                        }
                        else
                        {
                            ss[0] = doVoid;
                        }

                        TCommandsReader cr = new TCommandsReader(ss);
                        state.Step(cr);
                    }
                }

                current = next;
                ++iteration;
            }

            result.Add(new Flip());
            result.Add(new Halt());

            return result;
        }
    }
}