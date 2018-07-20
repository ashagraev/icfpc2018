namespace Solution
{
    using System;
    using System.Collections.Generic;

    public struct CoordDiff
    {
        public int Dx;
        public int Dy;
        public int Dz;

        public int MLen() => Math.Abs(Dx) + Math.Abs(Dy) + Math.Abs(Dz);

        public override string ToString() => string.Format("<dx={0}, dy={1}, dz={2}>", Dx, Dy, Dz);
    }

    public class Halt
    {
        public override string ToString() => "Halt";
    }

    public class Wait
    {
        public override string ToString() => "Wait";
    }

    public class Flip
    {
        public override string ToString() => "Flip";
    }

    public class StraightMove
    {
        public CoordDiff Diff;

        public override string ToString() => string.Format("SMove(d={0})", Diff);
    }

    public class LMove
    {
        public CoordDiff Diff1;
        public CoordDiff Diff2;

        public override string ToString() => string.Format("LMove(d1={0}, d2={1})", Diff1, Diff2);
    }

    public class Fission
    {
        public CoordDiff Diff;
        public int M;

        public override string ToString() => string.Format("Fission(d={0}, m={1})", Diff, M);
    }

    public class Fill
    {
        public CoordDiff Diff;

        public override string ToString() => string.Format("Fill(d={0})", Diff);
    }

    public class FusionP
    {
        public CoordDiff Diff;

        public override string ToString() => string.Format("FusionP(d={0})", Diff);
    }

    public class FusionS
    {
        public CoordDiff Diff;

        public override string ToString() => string.Format("FusionS(d={0})", Diff);
    }

    public class TCommands
    {
        public List<object> Commands;
        public int Pos = 0;

        public bool AtEnd() => Pos >= Commands.Count;

        public void Advance(int count)
        {
            Pos += count;
        }

        public object GetCommand(int idx)
        {
            return Commands[Pos + idx];
        }
    }
}