using System;
using System.IO;
using System.Text;

namespace sample
{
    enum EHarmonics
    {
        High,
        Low
    }

    internal class TCoord
    {
        public int X;
        public int Y;
        public int Z;
    }

    internal class TBot
    {
        public int Bid;
        public TCoord Coord;
        public int[] Seeds;
    }

    internal class TState
    {
        public byte R;

        public int Energy = 0;
        public EHarmonics Harmonics = EHarmonics.Low;

        public int[,,] Matrix;
        public TBot[] Bots;
        public object[] Commands;

        public void Load(string path)
        {
            var fs = new FileStream(path, FileMode.Open, FileAccess.Read);
            var br = new BinaryReader(fs, new ASCIIEncoding());

            int R = br.ReadByte();
            Matrix = new int[R, R, R];

            var bytesCount = (int) Math.Ceiling((float) Matrix.Length / 8);
            var bytes = br.ReadBytes(bytesCount);

            for (var x = 0; x < R; ++x)
            for (var y = 0; y < R; ++y)
            for (var z = 0; z < R; ++z)
            {
                var bitNumber = x * R * R + y * R + z;

                var byteNumber = bitNumber / 8;

                var shift = bitNumber % 8;

                var mask = 1 << shift;
                int curByte = bytes[byteNumber];
                var curRes = curByte & mask;

                Matrix[x, y, z] = curRes > 0 ? 1 : 0;
            }
        }
    }
}