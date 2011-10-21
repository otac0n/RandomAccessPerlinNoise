namespace RandomAccessPerlinNoise
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using System.Text;
    using System.Security.Cryptography;

    public class CryptoPseudoRandom
    {
        private readonly byte[] key;
        private readonly MD5CryptoServiceProvider md5;
        private readonly int blockWidth;
        private byte[] currentBlock;
        private int currentOffset;

        private const int DoubleSize = 8;

        public CryptoPseudoRandom(byte[] seed)
        {
            if (seed == null)
            {
                throw new ArgumentNullException("seed");
            }

            this.md5 = new MD5CryptoServiceProvider();
            this.key = this.md5.ComputeHash(seed);
            this.blockWidth = this.key.Length;

            Debug.Assert(this.blockWidth % 8 == 0);

            this.currentBlock = this.key;
            this.currentOffset = 0;
        }

        public double NextDouble()
        {
            EnsureAvailable();

            // Read the double's data from the array.
            byte[] doubleData = new byte[DoubleSize];
            Array.Copy(this.currentBlock, this.currentOffset, doubleData, 0, DoubleSize);

            // Constrain the double to be in the range [1, 2).
            // INFO: This is little-endian only!
            doubleData[7] = 0x3f;
            doubleData[6] = (byte)((doubleData[6] & 0xF) | 0xF0);

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
                byte[] chunk = new byte[this.blockWidth * 2];
                this.key.CopyTo(chunk, 0);
                this.currentBlock.CopyTo(chunk, this.blockWidth);

                this.currentBlock = this.md5.ComputeHash(chunk);
                this.currentOffset = 0;
            }
        }
    }
}
