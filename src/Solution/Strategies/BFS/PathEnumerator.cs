namespace Solution.Strategies.BFS
{
    using System.Collections.Generic;
    using System.Runtime.InteropServices;

    internal class PathEnumerator
    {
        // Both of these are updated from outside!
        private TModel model;
        private TState state;
        private HashSet<TCoord> interferedCells;

        public PathEnumerator(TModel model, TState state, HashSet<TCoord> interferedCells)
        {
            this.model = model;
            this.state = state;
            this.interferedCells = interferedCells;

            cells = new CellData[model.R, model.R, model.R];
            queue = new Queue<TCoord>();
        }

        internal struct CellData
        {
            public TCoord From;
            public int Cost;
            public int Generation;

            public bool Visited(int curGeneration) => Generation == curGeneration;
        }

        private int curGeneration;
        private CellData[,,] cells;
        private Queue<TCoord> queue;

        public struct CoordWithPath
        {
            public readonly TCoord Coord;

            private readonly CellData[,,] cells;

            public CoordWithPath(TCoord coord, CellData[,,] cells)
            {
                Coord = coord;
                this.cells = cells;
            }

            public int Cost => cells[Coord.X, Coord.Y, Coord.Z].Cost;

            public List<ICommand> RecreatePath(TCoord src)
            {
                var path = new List<ICommand>();
                var cur = Coord;
                while (!cur.Equals(src))
                {
                    var from = cells[cur.X, cur.Y, cur.Z].From;
                    path.Add(
                        new StraightMove
                        {
                            Diff = cur.Diff(from)
                        });
                    cur = from;
                }

                path.Reverse();
                return path;
            }
        }


        public IEnumerable<CoordWithPath> EnumerateReachablePaths(TCoord src)
        {
            ++curGeneration;

            // THIS IS VERY DUMB ALGO. BECAUSE I'M TOO STUPID TO DO BETTER

            // TODO: support LMoves
            queue.Clear();
            queue.Enqueue(src);
            cells[src.X, src.Y, src.Z] = new CellData()
            {
                Cost = 0,
                Generation = curGeneration
            };

            yield return new CoordWithPath(src, cells);
            while (queue.Count != 0)
            {
                var cur = queue.Dequeue();

                // TODO: smarter precompute?
                var (minDx, maxDx) = FindRange(1, 0, 0);
                var (minDy, maxDy) = FindRange(0, 1, 0);
                var (minDz, maxDz) = FindRange(0, 0, 1);

                var toEnqueue = new List<TCoord>();

                // we visit (and yield) closes nodes first
                for (var i = 0; i < Constants.StraightMoveCorrection; ++i)
                {
                    foreach (var c in DoVisits(i))
                    {
                        yield return c;
                        toEnqueue.Add(c.Coord);
                    }
                }

                // but enqueue furthest first
                toEnqueue.Reverse();
                foreach (var c in toEnqueue)
                {
                    queue.Enqueue(c);
                }

                (int, int) FindRange(int dx, int dy, int dz)
                {
                    var min = 0;
                    var minCoord = cur;
                    do
                    {
                        minCoord.Apply(new CoordDiff(-1 * dx, -1 * dy, -1 * dz));
                        --min;
                    }
                    while ((min >= -Constants.StraightMoveCorrection) && minCoord.IsValid(model.R) && IsFree(minCoord));

                    var max = 0;
                    var maxCoord = cur;
                    do
                    {
                        maxCoord.Apply(new CoordDiff(dx, dy, dz));
                        ++max;
                    }
                    while ((max <= Constants.StraightMoveCorrection) && maxCoord.IsValid(model.R) && IsFree(maxCoord));

                    return (min + 1, max - 1);
                }

                IEnumerable<CoordWithPath> DoVisits(int dist)
                {
                    foreach (var c in TryVisit(-dist >= minDx, dist, -dist, 0, 0)) yield return c;
                    foreach (var c in TryVisit(dist <= maxDx, dist, dist, 0, 0)) yield return c;
                    foreach (var c in TryVisit(-dist >= minDy, dist, 0, -dist, 0)) yield return c;
                    foreach (var c in TryVisit(dist <= maxDy, dist, 0, dist, 0)) yield return c;
                    foreach (var c in TryVisit(-dist >= minDz, dist, 0, 0, -dist)) yield return c;
                    foreach (var c in TryVisit(dist <= maxDz, dist, 0, 0, dist)) yield return c;
                }

                IEnumerable<CoordWithPath> TryVisit(bool ok, int dist, int dx, int dy, int dz)
                {
                    if (!ok)
                    {
                        yield break;
                    }

                    var next = new TCoord(cur.X + dx, cur.Y + dy, cur.Z + dz);
                    var curCost = cells[cur.X, cur.Y, cur.Z].Cost;
                    if (next.IsValid(model.R) && !cells[next.X, next.Y, next.Z].Visited(curGeneration))
                    {
                        // TODO: add energy maintainance into cost
                        cells[next.X, next.Y, next.Z] = new CellData()
                        {
                            From = cur,
                            Cost = curCost + (2 * dist),
                            Generation = curGeneration,
                        };
                        yield return new CoordWithPath(next, cells);

                        // if (!IsFree(next)) throw new Exception("WTF");
                    }
                }
            }
        }

        private bool IsFree(TCoord coord) => (state.M(coord) == 0) && !interferedCells.Contains(coord);
    }
}