namespace Sample
{
    public class CoordDiff
    {
        public int Dx;
        public int Dy;
        public int Dz;
    }

    public class Halt
    {
    }

    public class Wait
    {
    }

    public class Flip
    {
    }

    public class StraightMove
    {
        public CoordDiff Diff;
    }

    public class LMove
    {
        public CoordDiff Diff1;
        public CoordDiff Diff2;
    }

    public class Fission
    {
        public CoordDiff Diff;
        public int M;
    }

    public class Fill
    {
        public CoordDiff Diff;
    }

    public class Fusion
    {
        public CoordDiff Diff;
    }
}