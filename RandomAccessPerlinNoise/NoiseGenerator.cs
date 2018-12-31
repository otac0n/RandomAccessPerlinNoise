// Copyright © John Gietzen. All Rights Reserved. This source is subject to the MIT license. Please see license.md for more information.

namespace RandomAccessPerlinNoise
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using MurMurHashAlgorithm;
    using RandomImpls;

    public class NoiseGenerator
    {
        private readonly int dimensions;
        private readonly Interpolation interpolation;
        private readonly int levels;
        private readonly SplayTreeDictionary<long[], Array[]> levelsCache;
        private readonly int[][] levelSizes;
        private readonly double persistence;
        private readonly double[] persistences;
        private readonly double scale;
        private readonly long seed;
        private readonly int[] size;
        private readonly bool smooth;

        public NoiseGenerator(long seed, double persistence, int levels, int[] size, bool smooth, Interpolation interpolation)
        {
            this.seed = seed;

            if (persistence < 0.0 || persistence > 1.0)
            {
                throw new ArgumentOutOfRangeException(nameof(persistence));
            }

            this.persistence = persistence;

            if (levels <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(levels));
            }

            this.levels = levels;

            this.persistences = Enumerable.Range(0, this.levels)
                                          .Select(l => Math.Pow(persistence, l))
                                          .ToArray();
            this.scale = this.persistences.Sum();

            this.smooth = smooth;

            if (size == null)
            {
                throw new ArgumentNullException(nameof(size));
            }
            else if (size.Length == 0)
            {
                throw new ArgumentOutOfRangeException(nameof(size));
            }

            this.size = Array.ConvertAll(size, s => s);
            this.dimensions = this.size.Length;

            this.levelSizes = new int[this.levels][];
            for (var level = 0; level < this.levels; level++)
            {
                this.levelSizes[level] = new int[this.dimensions];
                for (var i = 0; i < this.dimensions; i++)
                {
                    this.levelSizes[level][i] = this.size[i] * (1 << level);
                }
            }

            this.interpolation = interpolation ?? throw new ArgumentNullException(nameof(interpolation));
            this.levelsCache = new SplayTreeDictionary<long[], Array[]>(CacheKeyComparer.Instance);
        }

        public void Fill(Array array, long[] location)
        {
            if (array == null)
            {
                throw new ArgumentNullException(nameof(array));
            }

            if (array.Rank != this.dimensions)
            {
                throw new ArgumentOutOfRangeException(nameof(array));
            }

            if (location == null)
            {
                throw new ArgumentNullException(nameof(location));
            }

            if (location.Length != this.dimensions)
            {
                throw new ArgumentOutOfRangeException(nameof(location));
            }

            var region = this.GetRegion(location);

            var size = new int[array.Rank];
            for (var i = 0; i < array.Rank; i++)
            {
                size[i] = array.GetLength(i);
            }

            Fill(array, indices =>
            {
                var value = 0.0D;

                for (var i = 0; i < this.levels; i++)
                {
                    value += this.Interpolate(region, i, indices, size) * this.persistences[i];
                }

                return value / this.scale;
            });
        }

        public double GetValue(params double[] coordinate) => this.GetValue(new long[this.dimensions], coordinate);

        public double GetValue(long[] coordinate, double[] offset)
        {
            if (coordinate == null)
            {
                throw new ArgumentNullException(nameof(coordinate));
            }

            if (coordinate.Length != this.dimensions)
            {
                throw new ArgumentOutOfRangeException(nameof(coordinate));
            }

            if (offset == null)
            {
                throw new ArgumentNullException(nameof(offset));
            }

            if (offset.Length != this.dimensions)
            {
                throw new ArgumentOutOfRangeException(nameof(offset));
            }

            var location = new long[this.dimensions];
            for (var i = 0; i < location.Length; i++)
            {
                location[i] = coordinate[i] + (long)Math.Floor(offset[i]);
            }

            offset = Array.ConvertAll(offset, o => o - Math.Floor(o));

            var region = this.GetRegion(location);
            var zeroOffset = new int[this.dimensions];

            var value = 0.0D;

            for (var level = 0; level < this.levels; level++)
            {
                var sourceIndices = new int[this.dimensions];
                var portions = new double[this.dimensions];
                var levelSize = this.levelSizes[level];

                for (var i = 0; i < this.dimensions; i++)
                {
                    var dimValue = offset[i] * levelSize[i];
                    sourceIndices[i] = (int)dimValue;
                    portions[i] = dimValue - sourceIndices[i];
                }

                value += this.Interpolate(region, level, zeroOffset, sourceIndices, 0, portions) * this.persistences[level];
            }

            return value / this.scale;
        }

        protected virtual Random GetRandom(long seed, long[] location)
        {
            var seedBytes = new byte[(1 + this.dimensions) * sizeof(long)];
            Array.Copy(BitConverter.GetBytes(seed), 0, seedBytes, 0, sizeof(long));

            for (var i = 0; i < this.dimensions; i++)
            {
                Array.Copy(BitConverter.GetBytes(location[i]), 0, seedBytes, (i + 1) * sizeof(long), sizeof(long));
            }

            return new HashAlgorithmRandom(new MurMurHash3Algorithm128x64(seed), seedBytes);
        }

        private static Array BuildLevel(int[] size, Random random)
        {
            var data = Array.CreateInstance(typeof(double), size);
            Fill(data, i => random.NextDouble());
            return data;
        }

        private static void Fill<T>(Array array, Func<int[], T> getValue) => Fill(array, new int[array.Rank], 0, getValue);

        private static void Fill<T>(Array array, int[] indices, int index, Func<int[], T> getValue)
        {
            var nextIndex = index + 1;

            if (index >= indices.Length)
            {
                array.SetValue(getValue(indices), indices);
            }
            else
            {
                var length = array.GetLength(index);

                for (var i = 0; i < length; i++)
                {
                    indices[index] = i;
                    Fill(array, indices, nextIndex, getValue);
                }
            }
        }

        private Array[] BuildChunk(long[] location)
        {
            var rand = this.GetRandom(this.seed, location);

            var chunk = new Array[this.levels];
            for (var i = 0; i < this.levels; i++)
            {
                chunk[i] = BuildLevel(this.levelSizes[i], rand);
            }

            return chunk;
        }

        private Array[] GetOrBuildChunk(long[] location)
        {
            Array[] chunk;
            lock (this.levelsCache)
            {
                if (!this.levelsCache.TryGetValue(location, out chunk))
                {
                    this.levelsCache[location.ToArray()] = chunk = this.BuildChunk(location);
                    this.levelsCache.Trim(4 << this.dimensions);
                }
            }

            return chunk;
        }

        private Array GetRegion(long[] location)
        {
            var region = Array.CreateInstance(typeof(Array), Enumerable.Repeat(2, this.dimensions).ToArray());
            Fill(region, offsets =>
            {
                var actual = new long[location.Length];
                for (var i = 0; i < location.Length; i++)
                {
                    actual[i] = location[i] + offsets[i];
                }

                return this.GetOrBuildChunk(actual);
            });

            return region;
        }

        private double Interpolate(Array region, int level, int[] indices, int[] size)
        {
            var sourceIndices = new int[this.dimensions];
            var portions = new double[this.dimensions];
            for (var i = 0; i < this.dimensions; i++)
            {
                sourceIndices[i] = Math.DivRem(indices[i] * this.levelSizes[level][i], size[i], out var remainder);
                portions[i] = ((double)remainder) / size[i];
            }

            return this.Interpolate(region, level, new int[this.dimensions], sourceIndices, 0, portions);
        }

        private double Interpolate(Array region, int level, int[] regionIndex, int[] subIndex, int index, double[] portions)
        {
            if (index >= this.dimensions)
            {
                var chunk = (Array[])region.GetValue(regionIndex);
                return (double)chunk[level].GetValue(subIndex);
            }
            else
            {
                var nextIndex = index + 1;
                var origIndexVal = subIndex[index];
                var origLocation = regionIndex[index];

                var a = this.Interpolate(region, level, regionIndex, subIndex, nextIndex, portions);

                subIndex[index] = origIndexVal + 1;
                if (subIndex[index] >= this.levelSizes[level][index])
                {
                    subIndex[index] = 0;
                    regionIndex[index]++;
                }

                var b = this.Interpolate(region, level, regionIndex, subIndex, nextIndex, portions);

                subIndex[index] = origIndexVal;
                regionIndex[index] = origLocation;

                return this.interpolation(a, b, portions[index]);
            }
        }

        private class CacheKeyComparer : IComparer<long[]>
        {
            public static readonly CacheKeyComparer Instance = new CacheKeyComparer();

            private CacheKeyComparer()
            {
            }

            public int Compare(long[] x, long[] y)
            {
                if (object.ReferenceEquals(x, y))
                {
                    return 0;
                }
                else if (x is null)
                {
                    return -1;
                }
                else if (y is null)
                {
                    return 1;
                }

                int comp;
                if ((comp = x.Length.CompareTo(y.Length)) != 0)
                {
                    return comp;
                }

                for (var i = 0; i < x.Length; i++)
                {
                    if ((comp = x[i].CompareTo(y[i])) != 0)
                    {
                        return comp;
                    }
                }

                return 0;
            }
        }
    }
}
