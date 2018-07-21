using System;
using System.Collections.Generic;
using System.Text;

namespace Solution
{
    class AlexShBaseStrategy
    {
        public static void AddTransition(List<ICommand> commands, TCoord current, TCoord target)
        {
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
            }
        }

        public static List<ICommand> MakeTrace(TModel model)
        {
            List<ICommand> result = new List<ICommand>();

            int minX = model.R;
            int minZ = model.R;

            for (var x = 0; x < model.R; ++x)
                for (var z = 0; z < model.R; ++z)
                    if (model[x, 0, z] > 0)
                    {
                        minX = Math.Min(minX, x);
                        minZ = Math.Min(minZ, z);
                    }

            TCoord target = new TCoord();
            target.X = minX;
            target.Z = minZ;

            TCoord start = new TCoord();
            start = target;
            start.X += 10;
            start.Z = 0;

            AddTransition(result, start, target);

            return result;
        }
    }
}
