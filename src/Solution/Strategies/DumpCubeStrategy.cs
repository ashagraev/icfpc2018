
namespace Solution.Strategies
{
    using System;
    using System.Collections.Generic;

    public class TDumpCubeTraverse
    {
        private CoordDiff Direction = new CoordDiff();
        private TCoord Current = new TCoord();

        private int R = 0;
        private int MaxY = 0;

        private bool LetGoBack = false;

        public TDumpCubeTraverse(TModel model)
        {
            R = model.R;

            for (int x = 0; x < R; ++x)
                for (int z = 0; z < R; ++z)
                for (int y = 0; y < R; ++y)
                    if (model[x, y, z] > 0)
                        MaxY = Math.Max(MaxY, y);
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

        public TCoord Next()
        {
            if (LetGoBack || Current.Y > MaxY)
            {
                LetGoBack = true;
                return NextForReturn();
            }

            if (Current.Y % 2 == 0)
            {
                if (Current.X + 1 == R && Current.Z + 1 == R)               // end of life
                {
                    Direction.Dx = 0;
                    Direction.Dy = 1;
                    Direction.Dz = 0;
                }
                else if (Current.X == 0 && Current.Z == 0)                  // walk forward X
                {
                    Direction.Dx = 1;
                    Direction.Dy = 0;
                    Direction.Dz = 0;
                }
                else if (Direction.Dx == 1 && Current.X + 1 == R)           // one step forward Z
                {
                    Direction.Dx = 0;
                    Direction.Dz = 1;
                }
                else if (Direction.Dz == 1 && Current.X + 1 == R)           // walk backward X
                {
                    Direction.Dx = -1;
                    Direction.Dz = 0;
                }
                else if (Direction.Dx == -1 && Current.X == 0)              // one step forward Z
                {
                    Direction.Dx = 0;
                    Direction.Dz = 1;
                }
                else if (Direction.Dz == 1 && Current.X == 0)               // walk again forward X
                {
                    Direction.Dx = 1;
                    Direction.Dz = 0;
                }
            }
            else if (Current.Y % 2 == 1)
            {
                if (Current.X == 0 && Current.Z == 0)               // end of life
                {
                    Direction.Dx = 0;
                    Direction.Dy = 1;
                    Direction.Dz = 0;
                }
                else if (Current.X + 1 == R && Current.Z + 1 == R)          // walk backward X
                {
                    Direction.Dx = -1;
                    Direction.Dy = 0;
                    Direction.Dz = 0;
                }
                else if (Direction.Dx == -1 && Current.X == 0)              // one step backward Z
                {
                    Direction.Dx = 0;
                    Direction.Dz = -1;
                }
                else if (Direction.Dz == -1 && Current.X == 0)              // walk forward X
                {
                    Direction.Dx = 1;
                    Direction.Dz = 0;
                }
                else if (Direction.Dx == 1 && Current.X + 1 == R)           // one step backward Z
                {
                    Direction.Dx = 0;
                    Direction.Dz = -1;
                }
                else if (Direction.Dz == -1 && Current.X + 1 == R)          // walk again backward X
                {
                    Direction.Dx = -1;
                    Direction.Dz = 0;
                }
            }

            Current.Apply(Direction);
            return Current;
        }
    }

    public class DumpCubeStrategy : IStrategy
    {
        public string Name => "DumpCube";

        public List<ICommand> MakeTrace(TModel model)
        {
            List<ICommand> result = new List<ICommand>();
            result.Add(new Flip());

            TCoord current = new TCoord();
            TDumpCubeTraverse dumpCureTraverse = new TDumpCubeTraverse(model);

            int iteration = 0;
            while (iteration == 0 || !current.IsAtStart())
            {
                TCoord next = dumpCureTraverse.Next();
                StraightMove move = new StraightMove();
                move.Diff = dumpCureTraverse.GetDirection();
                result.Add(move);

                if (model[current] > 0)
                {
                    Fill fill = new Fill();
                    fill.Diff = dumpCureTraverse.FillPreviousDirection();
                    result.Add(fill);
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