// <copyright file="LayerFiller.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Solution.Strategies
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;
    using System.Dynamic;
    using System.Linq;
    using System.Reflection.Metadata.Ecma335;
    using System.Runtime.CompilerServices;
    using System.Runtime.ExceptionServices;
    using System.Runtime.InteropServices;
    using System.Runtime.Serialization;
    using System.Text.RegularExpressions;
    using System.Threading;

    public class LayerFiller
    {
        int[] GetLayerHistogram(int[][] layer)
        {
            int[] res = new int[layer.Length];
            for (int i = 0; i < layer.Length; ++i)
            {
                for (int j = 0; j < layer[i].Length; ++j)
                {
                    if (layer[i][j] != 0)
                    {
                        res[i]++;
                    }
                }
            }

            return res;
        }
        
        List<int>[] GetLayerSegmentsMap(int[][] layer)
        {
            List<int>[] segments = new List<int>[layer.Length];
            for (int i = 0; i < layer.Length; ++i)
            {
                if (layer[i][0] != 0)
                {
                    throw new Exception("edge block detected"); 
                }

                for (int j = 1; j < layer[i].Length; ++j)
                {
                   
                    if (layer[i][j - 1] != layer[i][j])
                    {
                        segments[i].Add(j);
                    }
                }

                if (segments[i].Count % 2 != 0)
                {
                    throw new Exception("edge block detected"); 
                }
            }

            return segments;
        }

        List<ICommand> MoveToPosition(TBot bot, TCoord to)
        {
            return MoveToPosition(bot, to.X, to.Y, to.Z);
        }
            
        List<ICommand> MoveToPosition(TBot bot, int x, int y, int z)
        {
            var res = new List<ICommand>();
            while (bot.Coord.X != x && bot.Coord.Y != y && bot.Coord.Z != z)
            {
                if (bot.Coord.X != x)
                {
                    var dx = x - bot.Coord.X;
                    bot.Coord.X += dx % Constants.StraightMoveCorrection;
                    StraightMove smove = new StraightMove();
                    smove.Diff = new CoordDiff(dx, 0, 0);
                    res.Add(smove);
                }
                if (bot.Coord.Y != y)
                {
                    var dy = y - bot.Coord.Y;
                    bot.Coord.Y += dy % Constants.StraightMoveCorrection;
                    StraightMove smove = new StraightMove();
                    smove.Diff = new CoordDiff(0, dy, 0);
                    res.Add(smove);
                }
                if (bot.Coord.Z != z)
                {
                    var dz = z - bot.Coord.Z;
                    bot.Coord.Z += dz % Constants.StraightMoveCorrection;
                    StraightMove smove = new StraightMove();
                    smove.Diff = new CoordDiff(0, 0, dz);
                    res.Add(smove);
                }
            }

            return res;
        }

        List<ICommand> FillLine(TBot bot, CoordDiff direction, CoordDiff botOffset)
        {
            TCoord x = bot.Coord;
            List<ICommand> res = new List<ICommand>();
            CoordDiff fillOffset = new CoordDiff(-botOffset.Dx, -botOffset.Dy, -botOffset.Dz);
            if (direction.Dx != 0)
            {
                int dif = direction.Dx < 0 ? -1 : 1;
                
                while(true){

                    Fill fc = new Fill();
                    fc.Diff = fillOffset;
                    res.Add(fc);
                    if (bot.Coord.X == x.X + direction.Dx + botOffset.Dx)
                    {
                        break;
                    }
                    StraightMove move = new StraightMove();
                    move.Diff = new CoordDiff(dif, 0, 0);
                    res.Add(move);
                }
            }
            if (direction.Dy != 0)
            {
                int dif = direction.Dy < 0 ? -1 : 1;
                
                while(true){

                    Fill fc = new Fill();
                    fc.Diff = fillOffset;
                    res.Add(fc);
                    if (bot.Coord.Y == x.Y + direction.Dy + botOffset.Dy)
                    {
                        break;
                    }
                    StraightMove move = new StraightMove();
                    move.Diff = new CoordDiff(0, dif, 0);
                    res.Add(move);
                }
            }
            if (direction.Dz != 0)
            {
                int dif = direction.Dz < 0 ? -1 : 1;
                
                while(true){

                    Fill fc = new Fill();
                    fc.Diff = fillOffset;
                    res.Add(fc);
                    if (bot.Coord.Z == x.Z + direction.Dz + botOffset.Dz)
                    {
                        break;
                    }
                    StraightMove move = new StraightMove();
                    move.Diff = new CoordDiff(0, 0, dif);
                    res.Add(move);
                }
            }

            return res;
        }


        List<Tuple<TCoord, CoordDiff>> GenerateCuts(int lineIndex, List<int> segmentsMap, int layerIndex)
        {
            List<Tuple<TCoord, CoordDiff>> res = new List<Tuple<TCoord, CoordDiff>>();
            for (int j = 1; j < segmentsMap.Count; j+=2)
            {
                var start = segmentsMap[j - 1];
                var end = segmentsMap[j];
                var pos = new TCoord(lineIndex, start, layerIndex);
                var direction = new CoordDiff(0, end, 0);
                res.Add(Tuple.Create<TCoord, CoordDiff>(pos, direction));
            }

            return res;

        }
        

        List<ICommand> GenerateCommandsForSegment(TBot bot, int lineIndex, List<int> segmentsMapRaw, int direction)
        {
            List<int> segmentsMap = new List<int>(segmentsMapRaw);
            
            if (direction < 0)
            {
                segmentsMap.Reverse();
            }

            List<Tuple<TCoord, CoordDiff>> cuts = GenerateCuts(lineIndex, segmentsMapRaw, direction);
            
            var botOffset = new CoordDiff(0, 1, 0);

            var res = new List<ICommand>();
            foreach (var cut in cuts)
            {
                res.AddRange(MoveToPosition(bot, cut.Item1));
                res.AddRange(FillLine(bot, cut.Item2, botOffset));
            }
            return res;
        }

        List<int> GetSplit(int[] histogram)
        {
            var res = new List<int>();
            res.Add(histogram.Length);
            return res;
        }

        int GetOptimalBotsCount(int[] layersSplitCoordinates, int[][] layer)
        {
            return 1;
        }

        List<ICommand> GenerateCommands(int botsCount, List<int> slices, List<int>[] segmentsMap, TBot bot)
        {
            return new List<ICommand>();
            
        }
        
        List<ICommand> FillLayer(TBot bot, int[][] layer, int[][] previousLayer)
        {
//            int[] histogram = GetLayerHistogram(layer);
            List<int>[] layerSegmentsMap = GetLayerSegmentsMap(layer);
            List<ICommand> res = new List<ICommand>();
            for (int i = 0; i < layerSegmentsMap.Length; ++i)
            {
                res.AddRange(GenerateCommandsForSegment(bot, i, layerSegmentsMap[i], (i % 2 == 0) ? 1 : -1));

            }

            return res;

        }
    }
}