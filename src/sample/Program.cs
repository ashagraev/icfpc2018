using System;
using System.IO;
using System.Text;

namespace Sample
{
    internal class TMdl
    {
        public int[,,] Matrix;
        public byte R;

        public TMdl()
        {
        }

        public TMdl(string path)
        {
            var result = new TMdl();

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

    internal class Program
    {
        private static void Main(string[] args)
        {
            var mdl = new TMdl("problems/LA001_tgt.mdl");
        }
    }
}