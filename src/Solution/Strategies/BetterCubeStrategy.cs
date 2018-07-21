
namespace Solution.Strategies
{
    using System;
    using System.Collections.Generic;
    using System.Reflection.Emit;

    public class TBetterCubeTraverse
    {
        private List<TBorders> LevelBorders;

        private CoordDiff Direction = new CoordDiff();
        private TCoord Current = new TCoord();

        private int R = 0;
        private int MaxY = 0;

        private bool LetGoBack = false;
        private bool SearchStartPosition = true;

        public TBetterCubeTraverse(TModel model)
        {
            R = model.R;

            LevelBorders = new List<TBorders>();

            for (int y = 0; y < R; ++y)
            {
                TBorders thisLevelBorders = new TBorders();
                for (int x = 0; x < R; ++x)
                    for (int z = 0; z < R; ++z)
                        if (model[x, y, z] > 0)
                        {
                            MaxY = Math.Max(MaxY, y);

                            thisLevelBorders.MinX = Math.Min(thisLevelBorders.MinX, x);
                            thisLevelBorders.MaxX = Math.Max(thisLevelBorders.MaxX, x);

                            thisLevelBorders.MinZ = Math.Min(thisLevelBorders.MinZ, z);
                            thisLevelBorders.MaxZ = Math.Max(thisLevelBorders.MaxZ, z);
                        }
                LevelBorders.Add(thisLevelBorders);
            }
        }

        public CoordDiff GetDirection()
        {
            return Direction;
        }

        public CoordDiff FillPreviousDirection()
        {
            CoordDiff fillDirection = Direction;
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

        public TCoord Next(TModel model)
        {
            if (LetGoBack || Current.Y > MaxY + 1)
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

            TBorders borders = LevelBorders[Current.Y - 1];

            if (SearchStartPosition)
            {
                int targetX = (Current.Y % 2 == 1) ? borders.MinX : borders.MaxX;
                int targetZ = (Current.Y % 2 == 1) ? borders.MinZ : borders.MaxZ;

                if (Current.X < targetX)
                {
                    Direction.Dx = Math.Min(15, targetX - Current.X);
                    Direction.Dy = 0;
                    Direction.Dz = 0;
                    Current.Apply(Direction);
                    return Current;
                }
                if (Current.Z < targetZ)
                {
                    Direction.Dx = 0;
                    Direction.Dy = 0;
                    Direction.Dz = Math.Min(15, targetZ - Current.Z);
                    Current.Apply(Direction);
                    return Current;
                }
                if (Current.X > targetX)
                {
                    Direction.Dx = Math.Max(-15, targetX - Current.X);
                    Direction.Dy = 0;
                    Direction.Dz = 0;
                    Current.Apply(Direction);
                    return Current;
                }
                if (Current.Z > targetZ)
                {
                    Direction.Dx = 0;
                    Direction.Dy = 0;
                    Direction.Dz = Math.Max(-15, targetZ - Current.Z);
                    Current.Apply(Direction);
                    return Current;
                }

                SearchStartPosition = false;
            }

            if (Current.Y % 2 == 1)
            {
                if (Current.X > borders.MaxX && Current.Z > borders.MaxZ)               // end of life
                {
                    Direction.Dx = 0;
                    Direction.Dy = 1;
                    Direction.Dz = 0;
                    SearchStartPosition = true;
                }
                else if (Current.X == borders.MinX && Current.Z == borders.MinZ)                  // walk forward X
                {
                    Direction.Dx = 1;
                    Direction.Dy = 0;
                    Direction.Dz = 0;
                }
                else if (Direction.Dx > 0 && Current.X > borders.MaxX)           // one step forward Z
                {
                    Direction.Dx = 0;
                    Direction.Dy = 0;
                    Direction.Dz = 1;
                }
                else if (Direction.Dz > 0 && Current.X > borders.MaxX)           // walk backward X
                {
                    Direction.Dx = -1;
                    Direction.Dy = 0;
                    Direction.Dz = 0;
                }
                else if (Direction.Dx < 0 && Current.X < borders.MinX)              // one step forward Z
                {
                    Direction.Dx = 0;
                    Direction.Dy = 0;
                    Direction.Dz = 1;
                }
                else if (Direction.Dz > 0 && Current.X < borders.MinX)               // walk again forward X
                {
                    Direction.Dx = 1;
                    Direction.Dy = 0;
                    Direction.Dz = 0;
                }
            }
            else if (Current.Y % 2 == 0)
            {
                if (Current.X < borders.MinX && Current.Z < borders.MinZ)               // end of life
                {
                    Direction.Dx = 0;
                    Direction.Dy = 1;
                    Direction.Dz = 0;
                    SearchStartPosition = true;
                }
                else if (Current.X == borders.MaxX && Current.Z == borders.MaxZ)          // walk backward X
                {
                    Direction.Dx = -1;
                    Direction.Dy = 0;
                    Direction.Dz = 0;
                }
                else if (Direction.Dx < 0 && Current.X < borders.MinX)              // one step backward Z
                {
                    Direction.Dx = 0;
                    Direction.Dy = 0;
                    Direction.Dz = -1;
                }
                else if (Direction.Dz < 0 && Current.X < borders.MinX)              // walk forward X
                {
                    Direction.Dx = 1;
                    Direction.Dy = 0;
                    Direction.Dz = 0;
                }
                else if (Direction.Dx > 0 && Current.X > borders.MaxX)           // one step backward Z
                {
                    Direction.Dx = 0;
                    Direction.Dy = 0;
                    Direction.Dz = -1;
                }
                else if (Direction.Dz < 0 && Current.X > borders.MaxX)          // walk again backward X
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

    public class BetterCubeStrategy : IStrategy
    {
        public string Name => nameof(BetterCubeStrategy);

        public List<ICommand> MakeTrace(TModel model)
        {
            EHarmonics curHarmonics = EHarmonics.Low;

            List<ICommand> result = new List<ICommand>();

            TCoord current = new TCoord();
            TBetterCubeTraverse betterCureTraverse = new TBetterCubeTraverse(model);

            int iteration = 0;
            while (iteration == 0 || !current.IsAtStart())
            {
                TCoord next = betterCureTraverse.Next(model);
                StraightMove move = new StraightMove();
                move.Diff = betterCureTraverse.GetDirection();
                result.Add(move);

                if (next.Y > 0 && model[next.X, next.Y - 1, next.Z] > 0)
                {
                    if (curHarmonics == EHarmonics.Low)
                    {
                        result.Add(new Flip());
                        curHarmonics = EHarmonics.High;
                    }

                    Fill fill = new Fill();
                    fill.Diff.Dy = -1;
                    result.Add(fill);
                }
                else if (curHarmonics == EHarmonics.High)
                {
                    result.Add(new Flip());
                    curHarmonics = EHarmonics.Low;
                }

                current = next;
                ++iteration;
            }

            if (curHarmonics == EHarmonics.High)
            {
                result.Add(new Flip());
            }

            result.Add(new Halt());

            return result;
        }
    }
}