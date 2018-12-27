// Copyright © John Gietzen. All Rights Reserved. This source is subject to the MIT license. Please see license.md for more information.

namespace RandomAccessPerlinNoise
{
    using System;
    using System.Linq;
    using MurMurHashAlgorithm;
    using RandomImpls;

    public class NoiseGenerator
    {
        private readonly Interpolation interpolation;
        private readonly int levels;
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

            this.size = size;

            this.interpolation = interpolation ?? throw new ArgumentNullException(nameof(interpolation));
        }

        public void Fill(Array array, long[] location)
        {
            if (array == null)
            {
                throw new ArgumentNullException(nameof(array));
            }

            if (array.Rank != this.size.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(array));
            }

            if (location == null)
            {
                throw new ArgumentNullException(nameof(location));
            }

            if (location.Length != this.size.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(location));
            }

            var rands = this.InitializeRandoms(this.seed, location);

            var levels = new Array[this.levels];
            for (var i = 0; i < this.levels; i++)
            {
                levels[i] = BuildLevel(i, this.size, rands);
            }

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
                    value += this.Interpolate(levels[i], indices, size) * this.persistences[i];
                }

                return value / this.scale;
            });
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

        private static Array BuildLevel(int level, int[] baseSize, Array randoms)
        {
            var size = new int[baseSize.Length];
            for (var i = 0; i < size.Length; i++)
            {
                size[i] = baseSize[i] * (1 << level);
            }

            var noise = Array.CreateInstance(typeof(Array), baseSize.Select(i => 2).ToArray());
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

        private double Interpolate(Array array, int[] indices, int[] size)
        {
            var zeroCell = (Array)array.GetValue(new int[array.Rank]);
            var levelSize = new int[zeroCell.Rank];
            for (var i = 0; i < levelSize.Length; i++)
            {
                levelSize[i] = zeroCell.GetLength(i);
            }

            var sourceIndices = new int[indices.Length];
            var portions = new double[indices.Length];
            for (var i = 0; i < portions.Length; i++)
            {
                int remainder;
                sourceIndices[i] = Math.DivRem(indices[i] * levelSize[i], size[i], out remainder);
                portions[i] = ((double)remainder) / size[i];
            }

            return this.Interpolate(array, levelSize, new int[indices.Length], sourceIndices, 0, portions);
        }

        private double Interpolate(Array array, int[] levelSize, int[] parentIndex, int[] subIndex, int index, double[] portions)
        {
            var nextIndex = index + 1;

            if (index >= parentIndex.Length)
            {
                var sub = (Array)array.GetValue(parentIndex);
                return (double)sub.GetValue(subIndex);
            }
            else
            {
                var origIndexVal = subIndex[index];

                var a = this.Interpolate(array, levelSize, parentIndex, subIndex, nextIndex, portions);

                subIndex[index] = origIndexVal + 1;
                if (subIndex[index] >= levelSize[index])
                {
                    subIndex[index] = 0;
                    parentIndex[index] = 1;
                }

                var b = this.Interpolate(array, levelSize, parentIndex, subIndex, nextIndex, portions);

                subIndex[index] = origIndexVal;
                parentIndex[index] = 0;

                return this.interpolation(a, b, portions[index]);
            }
        }
    }
}
