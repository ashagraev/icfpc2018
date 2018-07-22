namespace Solution
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text;

    public enum EHarmonics
    {
        High,
        Low
    }

    public struct TCoord : IEquatable<TCoord>
    {
        public override string ToString() => "{" + $"{X}, {Y}, {Z}" + "}";

        public bool Equals(TCoord other) => (X == other.X) && (Y == other.Y) && (Z == other.Z);

        public override bool Equals(object obj) => Equals((TCoord)obj);

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = X;
                hashCode = (hashCode * 397) ^ Y;
                hashCode = (hashCode * 397) ^ Z;
                return hashCode;
            }
        }

        public int X;
        public int Y;
        public int Z;

        public TCoord(int x, int y, int z)
        {
            X = x;
            Y = y;
            Z = z;
        }

        public void Apply(CoordDiff diff)
        {
            X += diff.Dx;
            Y += diff.Dy;
            Z += diff.Dz;
        }

        public CoordDiff Diff(TCoord another) => new CoordDiff
        {
            Dx = X - another.X,
            Dy = Y - another.Y,
            Dz = Z - another.Z
        };

        public bool IsAtStart() => (X == 0) && (Y == 0) && (Z == 0);

        public IEnumerable<TCoord> NearNeighbours()
        {
            for (var dx = -1; dx <= 1; ++dx)
            {
                for (var dy = -1; dy <= 1; ++dy)
                {
                    for (var dz = -1; dz <= 1; ++dz)
                    {
                        var s = Math.Abs(dx) + Math.Abs(dy) + Math.Abs(dz);
                        if ((s != 0) && (s != 3))
                        {
                            yield return new TCoord(X + dx, Y + dy, Z + dz);
                        }
                    }
                }
            }
        }

        public IEnumerable<TCoord> ManhattenNeighbours()
        {
            yield return new TCoord(X - 1, Y, Z);
            yield return new TCoord(X + 1, Y, Z);
            yield return new TCoord(X, Y - 1, Z);
            yield return new TCoord(X, Y + 1, Z);
            yield return new TCoord(X, Y, Z - 1);
            yield return new TCoord(X, Y, Z + 1);
        }

        public bool IsValid(int r) => (X >= 0) && (X < r) && (Y >= 0) && (Y < r) && (Z >= 0) && (Z < r);
    }

    public class TBot
    {
        public int Bid;
        public TCoord Coord;
        public List<int> Seeds = new List<int>();
    }

    public struct TModel
    {
        public readonly string Name;
        public readonly int R;
        public readonly int NumFilled;

        private readonly int[,,] targetMatrix;

        public int this[int i, int j, int k] => targetMatrix[i, j, k];

        public int this[TCoord coord] => targetMatrix[coord.X, coord.Y, coord.Z];

        public TModel(string path)
        {
            Name = Path.GetFileNameWithoutExtension(path);
            if (Name.EndsWith("_tgt") || Name.EndsWith("_src"))
            {
                Name = Name.Remove(Name.Length - 4);
            }

            using (var fs = new FileStream(path, FileMode.Open, FileAccess.Read))
            {
                var br = new BinaryReader(fs, new ASCIIEncoding());

                R = br.ReadByte();
                NumFilled = 0;

                targetMatrix = new int[R, R, R];

                var bytesCount = (int)Math.Ceiling((float)targetMatrix.Length / 8);
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

                            var target = curRes > 0 ? 1 : 0;
                            targetMatrix[x, y, z] = target;
                            NumFilled += target;
                        }
                    }
                }
            }
        }
    }

    public class TCommandsReader
    {
        private readonly List<ICommand> Commands;
        private int Pos;

        public TCommandsReader(List<ICommand> commands) => Commands = commands;

        public void Advance(int count) => Pos += count;

        public bool AtEnd() => Pos >= Commands.Count;

        public ICommand GetCommand(int idx) => Commands[Pos + idx];
    }

    public class TState
    {
        public List<TBot> Bots = new List<TBot>();

        public long Energy;
        public EHarmonics Harmonics = EHarmonics.Low;

        public int[,,] Matrix;
        public TModel Model;

        public readonly bool EnableValidation;

        public TState(TModel model, bool enableValidation = false)
        {
            Model = model;
            EnableValidation = enableValidation;
            Matrix = new int[Model.R, Model.R, Model.R];

            var bot = new TBot
            {
                Bid = 1,
                Coord =
                {
                    X = 0,
                    Y = 0,
                    Z = 0
                }
            };
            for (var i = 2; i <= 40; ++i)
            {
                bot.Seeds.Add(i);
            }

            Bots.Add(bot);

            Energy = 0;
            Harmonics = EHarmonics.Low;
        }

        public static TState LoadFromFile(string path) => new TState(new TModel(path));

        public bool HasValidFinalState()
        {
            for (var x = 0; x < Model.R; ++x)
            {
                for (var y = 0; y < Model.R; ++y)
                {
                    for (var z = 0; z < Model.R; ++z)
                    {
                        if (Matrix[x, y, z] != Model[x, y, z])
                        {
                            return false;
                        }
                    }
                }
            }

            return (Bots.Count == 1) && Bots[0].Coord.IsAtStart() && (Harmonics == EHarmonics.Low);
        }

        public void Step(TCommandsReader commands)
        {
            if (Harmonics == EHarmonics.Low)
            {
                Energy += 3 * Model.R * Model.R * Model.R;
            }
            else if (Harmonics == EHarmonics.High)
            {
                Energy += 30 * Model.R * Model.R * Model.R;
            }

            Energy += 20 * Bots.Count;

            var botsCount = Bots.Count;
            var fusionPrimaries = new Dictionary<int, CoordDiff>();
            var fusionSecondaries = new Dictionary<int, CoordDiff>();
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

                        var newBot = new TBot { Bid = bot.Seeds[0] };

                        for (var i = 1; i <= fission.M; ++i)
                        {
                            newBot.Seeds.Append(bot.Seeds[i]);
                        }

                        bot.Seeds.RemoveRange(0, fission.M + 1);

                        newBot.Coord = bot.Coord;
                        newBot.Coord.Apply(fission.Diff);

                        Bots.Add(newBot);
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
                            if (EnableValidation)
                            {
                                if (Harmonics == EHarmonics.Low && !IsGrounded(newCoord, new HashSet<TCoord>()))
                                {
                                    throw new InvalidStateException($"{newCoord} is not grounded");
                                }
                            }
                            Energy += 12;
                        }

                        break;
                    }

                    case Void @void:
                    {
                        var newCoord = bot.Coord;
                        newCoord.Apply(@void.Diff);

                        if (Matrix[newCoord.X, newCoord.Y, newCoord.Z] > 0)
                        {
                            Matrix[newCoord.X, newCoord.Y, newCoord.Z] = 0;
                            Energy -= 12;
                        }
                        else
                        {
                            Energy += 3;
                        }

                        break;
                    }

                    case FusionP fusionP:
                    {
                        fusionPrimaries.Add(botIdx, fusionP.Diff);
                        break;
                    }

                    case FusionS fusionS:
                    {
                        fusionSecondaries.Add(botIdx, fusionS.Diff);
                        break;
                    }

                    default: throw new InvalidStateException($"unknown item type {command}");
                }
            }

            if (fusionPrimaries.Count > 0)
            {
                Fuse(fusionPrimaries, fusionSecondaries);
            }

            SortBots();

            commands.Advance(botsCount);
        }

        private bool IsGrounded(TCoord coord, HashSet<TCoord> visited)
        {
            if (visited.Contains(coord) || !coord.IsValid(Model.R) || Matrix[coord.X, coord.Y, coord.Z] == 0)
            {
                return false;
            }
            visited.Add(coord);
            if (coord.Y == 0)
            {
                return true;
            }

            return coord.ManhattenNeighbours().Any(x => IsGrounded(x, visited));
        }

        private void Fuse(Dictionary<int, CoordDiff> fusionPrimaries, Dictionary<int, CoordDiff> fusionSecondaries)
        {
            if (fusionPrimaries.Count != fusionSecondaries.Count)
            {
                throw new InvalidStateException(
                    $"Fusion count mismatch: {fusionPrimaries.Count} primaries, {fusionSecondaries.Count} secondaries");
            }

            foreach (var (primaryIdx, ndP) in fusionPrimaries)
            {
                var primaryCoord = Bots[primaryIdx].Coord;
                var secondaryCoord = primaryCoord;
                secondaryCoord.Apply(ndP);

                foreach (var (secondaryIdx, ndS) in fusionSecondaries)
                {
                    var sec = Bots[secondaryIdx];
                    if (sec.Coord.Equals(secondaryCoord))
                    {
                        var sanityCheckCoord = sec.Coord;
                        sanityCheckCoord.Apply(ndS);
                        if (!sanityCheckCoord.Equals(primaryCoord))
                        {
                            throw new InvalidStateException($"Fusion coord mismatch: {sanityCheckCoord} vs. {primaryCoord}");
                        }

                        var prim = Bots[primaryIdx];
                        prim.Seeds.Add(sec.Bid);
                        prim.Seeds.AddRange(sec.Seeds);
                        prim.Seeds.Sort();

                        Bots[secondaryIdx] = null;

                        Energy -= 24;

                        break;
                    }
                }
            }

            Bots = Bots.Where(x => x != null).ToList();
        }

        private void SortBots() => Bots.Sort((x, y) => x.Bid.CompareTo(y.Bid));
    }

    public class InvalidStateException : Exception
    {
        public InvalidStateException(string message)
            : base(message)
        {
        }
    }
}