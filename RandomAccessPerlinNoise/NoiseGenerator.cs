// Copyright © John Gietzen. All Rights Reserved. This source is subject to the MIT license. Please see license.md for more information.

namespace RandomAccessPerlinNoise
{
    using System;
    using System.Linq;
    using MurMurHashAlgorithm;
    using RandomImpls;

    public class NoiseGenerator
    {
        private readonly int dimensions;
        private readonly Interpolation interpolation;
        private readonly int levels;
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

            var levels = this.BuildLevels(location);

            var size = new int[array.Rank];
            for (var i = 0; i < array.Rank; i++)
            {
                size[i] = array.GetLength(i);
            }

            Fill(array, indices =>
            {
                var value = 0.0D;

                for (var i = 0; i < levels.Length; i++)
                {
                    value += this.Interpolate(levels[i], this.levelSizes[i], indices, size) * this.persistences[i];
                }

                return value / this.scale;
            });
        }

        public double GetValue(double[] coordinate) => this.GetValue(new long[this.dimensions], coordinate);

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

            var levels = this.BuildLevels(location);

            var value = 0.0D;

            for (var level = 0; level < levels.Length; level++)
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

                value += this.Interpolate(levels[level], levelSize, new int[this.dimensions], sourceIndices, 0, portions) * this.persistences[level];
            }

            return value / this.scale;
        }

        protected virtual Random GetRandom(long seed, long[] location)
        {
            var a = BitConverter.GetBytes(seed);
            var len = a.Length;

            var seedBytes = new byte[len * (location.Length + 1)];
            Array.Copy(a, 0, seedBytes, 0, a.Length);

            for (var i = 0; i < location.Length; i++)
            {
                Array.Copy(BitConverter.GetBytes(location[i]), 0, seedBytes, len * (i + 1), len);
            }

            return new HashAlgorithmRandom(new MurMurHash3Algorithm128x64(seed), seedBytes);
        }

        private static Array BuildLevel(int[] size, Array randoms)
        {
            var noise = Array.CreateInstance(typeof(Array), size.Select(i => 2).ToArray());
            Fill(noise, indices =>
            {
                var rand = (Random)randoms.GetValue(indices);

                var data = Array.CreateInstance(typeof(double), size);
                Fill(data, i => rand.NextDouble());
                return data;
            });

            return noise;
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

        private Array[] BuildLevels(long[] location)
        {
            var rands = this.InitializeRandoms(this.seed, location);

            var levels = new Array[this.levels];
            for (var i = 0; i < this.levels; i++)
            {
                levels[i] = BuildLevel(this.levelSizes[i], rands);
            }

            return levels;
        }

        private Array InitializeRandoms(long seed, long[] location)
        {
            var rands = Array.CreateInstance(typeof(Random), location.Select(i => 2).ToArray());
            Fill(rands, new int[location.Length], 0, offsets =>
            {
                var actual = new long[location.Length];
                for (var i = 0; i < location.Length; i++)
                {
                    actual[i] = location[i] + offsets[i];
                }

                return this.GetRandom(seed, actual);
            });

            return rands;
        }

        private double Interpolate(Array level, int[] levelSize, int[] indices, int[] size)
        {
            var sourceIndices = new int[this.dimensions];
            var portions = new double[this.dimensions];
            for (var i = 0; i < this.dimensions; i++)
            {
                sourceIndices[i] = Math.DivRem(indices[i] * levelSize[i], size[i], out var remainder);
                portions[i] = ((double)remainder) / size[i];
            }

            return this.Interpolate(level, levelSize, new int[this.dimensions], sourceIndices, 0, portions);
        }

        private double Interpolate(Array level, int[] levelSize, int[] parentIndex, int[] subIndex, int index, double[] portions)
        {
            var nextIndex = index + 1;

            if (index >= parentIndex.Length)
            {
                var sub = (Array)level.GetValue(parentIndex);
                return (double)sub.GetValue(subIndex);
            }
            else
            {
                var origIndexVal = subIndex[index];

                var a = this.Interpolate(level, levelSize, parentIndex, subIndex, nextIndex, portions);

                subIndex[index] = origIndexVal + 1;
                if (subIndex[index] >= levelSize[index])
                {
                    subIndex[index] = 0;
                    parentIndex[index] = 1;
                }

                var b = this.Interpolate(level, levelSize, parentIndex, subIndex, nextIndex, portions);

                subIndex[index] = origIndexVal;
                parentIndex[index] = 0;

                return this.interpolation(a, b, portions[index]);
            }
        }
    }
}
