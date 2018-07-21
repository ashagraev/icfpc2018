namespace Solution
{
    using System;
    using System.Collections.Generic;
    using System.Linq.Expressions;
    using System.Runtime.InteropServices.WindowsRuntime;

    public class TraceSerializer
    {
        public static byte[] Serialize(IEnumerable<object> commands)
        {
            var result = new List<byte>();
            foreach (var cmd in commands)
            {
                switch (cmd)
                {
                    case Halt _:
                        result.Add(0b1111_1111);
                        break;
                    case Wait _:
                        result.Add(0b1111_1110);
                        break;
                    case Flip _:
                        result.Add(0b1111_1101);
                        break;
                    case StraightMove sm:
                        var (axis, delta) = EncodeAxisAndDelta(sm.Diff, Constants.StraightMoveCorrection);
                        result.Add((byte)((axis << 4) | 0b100));
                        result.Add((byte)delta);
                        break;
                    case LMove lm:
                        var (axis1, delta1) = EncodeAxisAndDelta(lm.Diff1, Constants.LMoveCorrection);
                        var (axis2, delta2) = EncodeAxisAndDelta(lm.Diff2, Constants.LMoveCorrection);
                        result.Add((byte)((axis1 << 4) | (axis2 << 6) | 0b1100));
                        result.Add((byte)(delta1 | (delta2 << 4)));
                        break;
                    case FusionP fp:
                        result.Add((byte)(EncodeNearDiff(fp.Diff) | 0b111));
                        break;
                    case FusionS fs:
                        result.Add((byte)(EncodeNearDiff(fs.Diff) | 0b110));
                        break;
                    case Fission fss:
                        result.Add((byte)(EncodeNearDiff(fss.Diff) | 0b101));
                        result.Add((byte)fss.M);
                        break;
                    case Fill fill:
                        result.Add((byte)(EncodeNearDiff(fill.Diff) | 0b011));
                        break;
                }
            }

            return result.ToArray();
        }

        private static (int, int) EncodeAxisAndDelta(CoordDiff diff, int correction)
        {
            var nonZeroSum = (diff.Dx != 0 ? 1 : 0) + (diff.Dy != 0 ? 1 : 0) + (diff.Dz != 0 ? 1 : 0);
            if (nonZeroSum != 1)
            {
                throw new ArgumentOutOfRangeException(string.Format("Exactly one non-zero component expected: {0}", diff));
            }

            int shift;
            int axis;
            if (diff.Dx != 0)
            {
                shift = diff.Dx;
                axis = Constants.XAxis;
            }
            else if (diff.Dy != 0)
            {
                shift = diff.Dy;
                axis = Constants.YAxis;
            }
            else
            {
                shift = diff.Dz;
                axis = Constants.ZAxis;
            }

            if (Math.Abs(shift) > correction)
            {
                throw new ArgumentOutOfRangeException(string.Format("The diff component is too large: {0}", shift));
            }

            return (axis, shift + correction);
        }

        private static int EncodeNearDiff(CoordDiff diff)
        {
            var cl = diff.CLen();
            var ml = diff.MLen();
            if (cl != 1 || (ml <= 0 || ml > 2))
            {
                throw new ArgumentOutOfRangeException(string.Format("Invalid near diff: {0}", diff));
            }
            return ((diff.Dx + 1) * 9 + (diff.Dy + 1) * 3 + (diff.Dz + 1)) << 3;
        }
    }
}