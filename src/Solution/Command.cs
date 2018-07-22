namespace Solution
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Runtime.CompilerServices;

    public struct CoordDiff
    {
        public int Dx;
        public int Dy;
        public int Dz;

        public CoordDiff(int dx, int dy, int dz)
        {
            Dx = dx;
            Dy = dy;
            Dz = dz;
        }

        public int CLen() => Math.Max(Math.Abs(Dx), Math.Max(Math.Abs(Dy), Math.Abs(Dz)));

        public int MLen() => Math.Abs(Dx) + Math.Abs(Dy) + Math.Abs(Dz);

        public override string ToString() => string.Format("<dx={0}, dy={1}, dz={2}>", Dx, Dy, Dz);
    }

    public interface ICommand
    {
    };

    public class Halt : ICommand
    {
        public override string ToString() => "Halt";
    }

    public class Wait : ICommand
    {
        public override string ToString() => "Wait";
    }

    public class Flip : ICommand
    {
        public override string ToString() => "Flip";
    }

    public class StraightMove : ICommand
    {
        public CoordDiff Diff;

        public override string ToString() => string.Format("SMove(d={0})", Diff);
    }

    public class LMove : ICommand
    {
        public CoordDiff Diff1;
        public CoordDiff Diff2;

        public override string ToString() => string.Format("LMove(d1={0}, d2={1})", Diff1, Diff2);
    }

    public class Fission : ICommand
    {
        public CoordDiff Diff;
        public int M;

        public override string ToString() => string.Format("Fission(d={0}, m={1})", Diff, M);

        public Fission()
        {
        }

        public Fission(int dx, int dy, int dz, int m)
        {
            Diff = new CoordDiff(dx, dy, dz);
            M = m;
        }
    }

    public class Fill : ICommand
    {
        public CoordDiff Diff;

        public override string ToString() => string.Format("Fill(d={0})", Diff);

        public Fill()
        {
        }

        public Fill(int dx, int dy, int dz)
        {
            Diff = new CoordDiff(dx, dy, dz);
        }
    }

    public class FusionP : ICommand
    {
        public CoordDiff Diff;

        public override string ToString() => string.Format("FusionP(d={0})", Diff);
    }

    public class FusionS : ICommand
    {
        public CoordDiff Diff;

        public override string ToString() => string.Format("FusionS(d={0})", Diff);
    }
}