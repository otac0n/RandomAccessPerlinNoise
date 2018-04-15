// Copyright © John Gietzen. All Rights Reserved. This source is subject to the MIT license. Please see license.md for more information.

namespace RandomAccessPerlinNoise
{
    using System;
    using System.Diagnostics;
    using System.Linq;
    using System.Security.Cryptography;

    public class CryptoPseudoRandom
    {
        private static readonly int BlockWidth;
        private static readonly int DoubleExponentByteA;
        private static readonly int DoubleExponentByteB;
        private static readonly int DoubleSize;

        private readonly byte[] key;
        private readonly MD5CryptoServiceProvider md5;
        private byte[] currentBlock;
        private int currentOffset;

        static CryptoPseudoRandom()
        {
            var bytes = BitConverter.GetBytes(1.0D);
            DoubleSize = bytes.Length;
            DoubleExponentByteA = Enumerable.Range(0, DoubleSize).Where(i => bytes[i] == 0x3F).Single();
            DoubleExponentByteB = Enumerable.Range(0, DoubleSize).Where(i => bytes[i] == 0xF0).Single();

            BlockWidth = new MD5CryptoServiceProvider().ComputeHash(bytes).Length;

            Debug.Assert(BlockWidth % DoubleSize == 0);
        }

        public CryptoPseudoRandom(byte[] seed)
        {
            if (seed == null)
            {
                throw new ArgumentNullException(nameof(seed));
            }

            this.md5 = new MD5CryptoServiceProvider();
            this.key = this.md5.ComputeHash(seed);

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
            if (this.currentOffset >= BlockWidth)
            {
                var chunk = new byte[BlockWidth * 2];
                this.key.CopyTo(chunk, 0);
                this.currentBlock.CopyTo(chunk, BlockWidth);

                this.currentBlock = this.md5.ComputeHash(chunk);
                this.currentOffset = 0;
            }
        }
    }
}
