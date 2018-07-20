using System.IO;
using System.Numerics;
using System.Runtime.ExceptionServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;

namespace Sample
{
    using System;

    internal class TMdl
    {
        public byte R;
        public int[,,] Matrix;

        public TMdl()
        {

        }

        public TMdl(string path)
        {
            TMdl result = new TMdl();

            FileStream fs = new FileStream(path, FileMode.Open, FileAccess.Read);
            BinaryReader br = new BinaryReader(fs, new ASCIIEncoding());

            R = br.ReadByte();
            Matrix = new int[R, R, R];

            int bytesCount = (int) Math.Ceiling((float)Matrix.Length / 8);
            byte[] bytes = br.ReadBytes(bytesCount);

            for (int x = 0; x < R; ++x)
            {
                for (int y = 0; y < R; ++y)
                {
                    for (int z = 0; z < R; ++z)
                    {
                        int bitNumber = x * R * R + y * R + z;

                        int byteNumber = bitNumber / 8;
                        int shift = bitNumber % 8;

                        int mask = 1 << (7 - shift);
                        int curByte = bytes[byteNumber];
                        int curRes = curByte & mask;

                        Matrix[x, y, z] = curRes > 0 ? 1 : 0;
                    }
                }
            }

            for (int x = 0; x < R; ++x)
            {
                for (int z = 0; z < R; ++z)
                {
                    Console.Write(Matrix[x, 0, z]);
                }
                Console.WriteLine();
            }
        }

    }

    internal class Program
    {
        private static void Main(string[] args)
        {
            TMdl mdl = new TMdl("problems/LA001_tgt.mdl");
        }
    }
}
