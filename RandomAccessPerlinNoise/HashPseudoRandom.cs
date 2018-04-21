// Copyright © John Gietzen. All Rights Reserved. This source is subject to the MIT license. Please see license.md for more information.

namespace RandomAccessPerlinNoise
{
    using System;
    using System.Linq;
    using System.Security.Cryptography;

    public class HashPseudoRandom
    {
        private static readonly int DoubleExponentByteA;
        private static readonly int DoubleExponentByteB;
        private static readonly int DoubleSize;

        private readonly int blockWidth;
        private readonly HashAlgorithm hashAlgorithm;
        private readonly byte[] key;
        private byte[] currentBlock;
        private int currentOffset;

        static HashPseudoRandom()
        {
            var bytes = BitConverter.GetBytes(1.0D);
            DoubleSize = bytes.Length;
            DoubleExponentByteA = Enumerable.Range(0, DoubleSize).Where(i => bytes[i] == 0x3F).Single();
            DoubleExponentByteB = Enumerable.Range(0, DoubleSize).Where(i => bytes[i] == 0xF0).Single();
        }

        public HashPseudoRandom(HashAlgorithm hashAlgorithm, byte[] seed = null)
        {
            this.hashAlgorithm = hashAlgorithm ?? throw new ArgumentNullException(nameof(hashAlgorithm));
            seed = seed ?? Array.Empty<byte>();

            this.key = this.hashAlgorithm.ComputeHash(seed);
            this.blockWidth = this.key.Length;
            if (this.blockWidth % DoubleSize != 0)
            {
                throw new ArgumentOutOfRangeException(nameof(hashAlgorithm));
            }

            this.currentBlock = this.key;
            this.currentOffset = 0;
        }

        public double NextDouble()
        {
            this.EnsureAvailable();

            // Read the double's data from the array.
            var doubleData = new byte[DoubleSize];
            Array.Copy(this.currentBlock, this.currentOffset, doubleData, 0, DoubleSize);

            // Constrain the double to be in the range [1, 2).
            doubleData[DoubleExponentByteA] = 0x3f;
            doubleData[DoubleExponentByteB] = (byte)((doubleData[DoubleExponentByteB] & 0xF) | 0xF0);

            // Convert the bits!
            var value = BitConverter.ToDouble(doubleData, 0);
            this.currentOffset += DoubleSize;

            // Subtract one, to get the range [0, 1).
            return value - 1;
        }

        private void EnsureAvailable()
        {
            if (this.currentOffset >= this.blockWidth)
            {
                var chunk = new byte[this.blockWidth * 2];
                this.key.CopyTo(chunk, 0);
                this.currentBlock.CopyTo(chunk, this.blockWidth);

                this.currentBlock = this.hashAlgorithm.ComputeHash(chunk);
                this.currentOffset = 0;
            }
        }
    }
}
