﻿namespace Solution
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text;

    internal enum EHarmonics
    {
        High,
        Low
    }

    internal struct TCoord
    {
        public int X;
        public int Y;
        public int Z;

        public void Apply(CoordDiff diff)
        {
            X += diff.Dx;
            Y += diff.Dy;
            Z += diff.Dz;
        }

        public bool IsAtStart() => (X == 0) && (Y == 0) && (Z == 0);
    }

    internal class TBot
    {
        public int Bid;
        public TCoord Coord;
        public List<int> Seeds = new List<int>();
    }

    internal class TState
    {
        public List<TBot> Bots = new List<TBot>();

        public long Energy;
        public EHarmonics Harmonics = EHarmonics.Low;

        public int[,,] Matrix;
        public int[,,] TargetMatrix;

        public byte R;

        public bool HasValidFinalState()
        {
            for (var x = 0; x < R; ++x)
            {
                for (var y = 0; y < R; ++y)
                {
                    for (var z = 0; z < R; ++z)
                    {
                        if (Matrix[x, y, z] != TargetMatrix[x, y, z])
                        {
                            return false;
                        }
                    }
                }
            }

            return (Bots.Count == 1) && Bots[0].Coord.IsAtStart() && (Harmonics == EHarmonics.Low);
        }

        public void Load(string path)
        {
            var fs = new FileStream(path, FileMode.Open, FileAccess.Read);
            var br = new BinaryReader(fs, new ASCIIEncoding());

            R = br.ReadByte();

            TargetMatrix = new int[R, R, R];
            Matrix = new int[R, R, R];

            var bytesCount = (int)Math.Ceiling((float)TargetMatrix.Length / 8);
            var bytes = br.ReadBytes(bytesCount);

            for (var x = 0; x < R; ++x)
            {
                for (var y = 0; y < R; ++y)
                {
                    for (var z = 0; z < R; ++z)
                    {
                        var bitNumber = (x * R * R) + (y * R) + z;

                        var byteNumber = bitNumber / 8;

                        var shift = bitNumber % 8;

                        var mask = 1 << shift;
                        int curByte = bytes[byteNumber];
                        var curRes = curByte & mask;

                        TargetMatrix[x, y, z] = curRes > 0 ? 1 : 0;
                    }
                }
            }

            var bot = new TBot();
            bot.Bid = 1;
            bot.Coord.X = 0;
            bot.Coord.Y = 0;
            bot.Coord.Z = 0;
            for (var i = 2; i <= 20; ++i)
            {
                bot.Seeds.Add(i);
            }
            Bots.Add(bot);

            Energy = 0;
            Harmonics = EHarmonics.Low;
        }

        public void ApplyCommands(TCommands commands)
        {
            if (Harmonics == EHarmonics.Low)
            {
                Energy += 3 * R * R * R;
            }
            else if (Harmonics == EHarmonics.High)
            {
                Energy += 30 * R * R * R;
            }

            Energy += 20 * Bots.Count;

            var botsCount = Bots.Count;
            for (var botIdx = 0; botIdx < botsCount; ++botIdx)
            {
                var command = commands.GetCommand(botIdx);
                var bot = Bots[botIdx];

                switch (command)
                {
                    case Halt halt: break;
                    case Wait wait: break;
                    case Flip flip:
                    {
                        if (Harmonics == EHarmonics.High)
                        {
                            Harmonics = EHarmonics.Low;
                        }
                        else
                        {
                            Harmonics = EHarmonics.High;
                        }

                        break;
                    }

                    case StraightMove move:
                    {
                        bot.Coord.Apply(move.Diff);
                        Energy += 2 * move.Diff.MLen();
                        break;
                    }

                    case LMove lMove:
                    {
                        bot.Coord.Apply(lMove.Diff1);
                        bot.Coord.Apply(lMove.Diff2);

                        Energy += 2 * (lMove.Diff1.MLen() + 2 + lMove.Diff2.MLen());

                        break;
                    }

                    case Fission fission:
                    {
                        bot.Seeds.Sort();

                        var newBot = new TBot();
                        newBot.Bid = bot.Seeds[0];

                        for (var i = 1; i <= fission.M; ++i)
                        {
                            newBot.Seeds.Append(bot.Seeds[i]);
                        }

                        bot.Seeds.RemoveRange(0, fission.M + 1);

                        newBot.Coord = bot.Coord;
                        newBot.Coord.Apply(fission.Diff);

                        Bots.Append(newBot);
                        Energy += 24;

                        break;
                    }

                    case Fill fill:
                    {
                        var newCoord = bot.Coord;
                        newCoord.Apply(fill.Diff);

                        if (Matrix[newCoord.X, newCoord.Y, newCoord.Z] > 0)
                        {
                            Energy += 6;
                        }
                        else
                        {
                            Matrix[newCoord.X, newCoord.Y, newCoord.Z] = 1;
                            Energy += 12;
                        }

                        break;
                    }

                    case FusionP fusionP:
                    {
                        break;
                    }

                    case FusionS fusionS:
                    {
                        break;
                    }

                    default: throw new InvalidOperationException("unknown item type");
                }
            }

            commands.Advance(botsCount);
        }
    }
}