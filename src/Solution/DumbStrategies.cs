using System;
using System.Collections.Generic;
using System.Text;

namespace Solution
{
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

                Console.Write(current.X);
                Console.Write(" ");
                Console.Write(current.Y);
                Console.Write(" ");
                Console.Write(current.Z);
                Console.WriteLine();

                ++iteration;
            }

            result.Add(new Flip());
            result.Add(new Halt());

            return result;
        }
    }

    public class AlexShBaseStrategy : IStrategy
    {
        public static void AddTransition(List<ICommand> commands, TCoord current, TCoord target, TModel model, bool doFill)
        {
            Fill fill = new Fill();
            fill.Diff.Dx = 0;
            fill.Diff.Dy = 0;
            fill.Diff.Dz = 0;

            if (model[current.X, current.Y, current.Z] > 0 && doFill)
            {
                commands.Add(fill);
            }

            int xDiff = target.X > current.X ? 1 : -1;
            int yDiff = target.Y > current.Y ? 1 : -1;
            int zDiff = target.Z > current.Z ? 1 : -1;

            while (Math.Abs(current.X - target.X) > 1 ||
                   Math.Abs(current.Y - target.Y) > 1 ||
                   Math.Abs(current.Z - target.Z) > 1)
            {
                LMove lMove = new LMove();

                lMove.Diff1.Dx = Math.Abs(current.X - target.X) > 1 ? xDiff : 0;
                lMove.Diff1.Dy = Math.Abs(current.Y - target.Y) > 1 ? yDiff : 0;
                lMove.Diff1.Dz = Math.Abs(current.Z - target.Z) > 1 ? zDiff : 0;

                lMove.Diff2.Dx = Math.Abs(current.X - target.X) > 0 ? xDiff : 0;
                lMove.Diff2.Dy = Math.Abs(current.Y - target.Y) > 0 ? yDiff : 0;
                lMove.Diff2.Dz = Math.Abs(current.Z - target.Z) > 0 ? zDiff : 0;

                commands.Add(lMove);

                current.X += lMove.Diff1.Dx + lMove.Diff2.Dx;
                current.Y += lMove.Diff1.Dy + lMove.Diff2.Dy;
                current.Z += lMove.Diff1.Dz + lMove.Diff2.Dz;

                if (model[current.X, current.Y, current.Z] > 0 && doFill)
                {
                    commands.Add(fill);
                }
            }

            while (Math.Abs(current.X - target.X) > 0 ||
                   Math.Abs(current.Y - target.Y) > 0 ||
                   Math.Abs(current.Z - target.Z) > 0)
            {
                StraightMove sMove = new StraightMove();

                sMove.Diff.Dx = Math.Abs(current.X - target.X) > 0 ? xDiff : 0;
                sMove.Diff.Dy = Math.Abs(current.Y - target.Y) > 0 ? yDiff : 0;
                sMove.Diff.Dz = Math.Abs(current.Z - target.Z) > 0 ? zDiff : 0;

                commands.Add(sMove);

                current.X += sMove.Diff.Dx;
                current.Y += sMove.Diff.Dy;
                current.Z += sMove.Diff.Dz;

                if (model[current.X, current.Y, current.Z] > 0 && doFill)
                {
                    commands.Add(fill);
                }
            }
        }

        public static TCoord RectangleTraverse(List<ICommand> commands, int minX, int minZ, int maxX, int maxZ, int y, TModel model)
        {
            TCoord current;
            current.X = minX;
            current.Y = y;
            current.Z = minZ;

            while (current.X < maxX || current.Z < maxZ)
            {
                if (current.X < maxX)
                {
                    TCoord next = current;
                    next.X = maxX;
                    AddTransition(commands, current, next, model, true);
                    current = next;
                }
                if (current.Z < maxZ)
                {
                    TCoord next1 = current;
                    next1.Z += 1;

                    TCoord next2 = next1;
                    next2.X = minX;

                    AddTransition(commands, current, next1, model, true);
                    AddTransition(commands, next1, next2, model, true);

                    current = next2;
                }
                if (current.Z < maxZ)
                {
                    TCoord next = current;
                    next.Z += 1;

                    AddTransition(commands, current, next, model, true);

                    current = next;
                }
            }

            return current;
        }

        public List<ICommand> MakeTrace(TModel model)
        {
            List<ICommand> result = new List<ICommand>();

            int maxY = 0;
            for (var x = 0; x < model.R; ++x)
                for (var y = 0; y < model.R; ++y)
                    for (var z = 0; z < model.R; ++z)
                        if (model[x, y, z] > 0)
                            maxY = Math.Max(maxY, y);

            TCoord current;
            current.X = 0;
            current.Y = 0;
            current.Z = 0;

            for (int y = 0; y <= maxY; ++y)
            {
                int minX = model.R;
                int minZ = model.R;

                int maxX = 0;
                int maxZ = 0;

                for (var x = 0; x < model.R; ++x)
                    for (var z = 0; z < model.R; ++z)
                        if (model[x, y, z] > 0)
                        {
                            minX = Math.Min(minX, x);
                            minZ = Math.Min(minZ, z);

                            maxX = Math.Max(maxX, x);
                            maxZ = Math.Max(maxZ, z);
                        }

                TCoord startPoint;
                startPoint.X = minX;
                startPoint.Y = y;
                startPoint.Z = minZ;

                AddTransition(result, current, startPoint, model, false);

                current = RectangleTraverse(result, minX, minZ, maxX, maxZ, y, model);

                TCoord next = current;
                ++next.Y;
                AddTransition(result, current, next, model, false);
            }

            {
                TCoord next1 = current;
                next1.X = 0;
                next1.Z = 0;
                AddTransition(result, current, next1, model, false);

                TCoord next2 = next1;
                next2.Y = 0;

                AddTransition(result, next1, next2, model, false);
            }

            result.Add(new Halt());

            return result;
        }

        public string Name => "AlexSh";
    }
}
