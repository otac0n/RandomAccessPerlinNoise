namespace RandomAccessPerlinNoise
{
    using System;
    using System.Diagnostics;
    using System.Linq;
    using System.Security.Cryptography;

    public class CryptoPseudoRandom
    {
        private static readonly int doubleSize;
        private static readonly int doubleExponentByteA;
        private static readonly int doubleExponentByteB;
        private static readonly int blockWidth;

        private readonly byte[] key;
        private readonly MD5CryptoServiceProvider md5;
        private byte[] currentBlock;
        private int currentOffset;

        static CryptoPseudoRandom()
        {
            var bytes = BitConverter.GetBytes(1.0D);
            doubleSize = bytes.Length;
            doubleExponentByteA = Enumerable.Range(0, doubleSize).Where(i => bytes[i] == 0x3F).Single();
            doubleExponentByteB = Enumerable.Range(0, doubleSize).Where(i => bytes[i] == 0xF0).Single();

            blockWidth = new MD5CryptoServiceProvider().ComputeHash(bytes).Length;

            Debug.Assert(blockWidth % doubleSize == 0);
        }

        public CryptoPseudoRandom(byte[] seed)
        {
            if (seed == null)
            {
                throw new ArgumentNullException("seed");
            }

            this.md5 = new MD5CryptoServiceProvider();
            this.key = this.md5.ComputeHash(seed);

            this.currentBlock = this.key;
            this.currentOffset = 0;
        }

        public double NextDouble()
        {
            EnsureAvailable();

            // Read the double's data from the array.
            byte[] doubleData = new byte[doubleSize];
            Array.Copy(this.currentBlock, this.currentOffset, doubleData, 0, doubleSize);

            // Constrain the double to be in the range [1, 2).
            doubleData[doubleExponentByteA] = 0x3f;
            doubleData[doubleExponentByteB] = (byte)((doubleData[doubleExponentByteB] & 0xF) | 0xF0);

            // Convert the bits!
            var value = BitConverter.ToDouble(doubleData, 0);
            this.currentOffset += doubleSize;

            // Subtract one, to get the range [0, 1).
            return value - 1;
        }

        private void EnsureAvailable()
        {
            if (this.currentOffset >= blockWidth)
            {
                byte[] chunk = new byte[blockWidth * 2];
                this.key.CopyTo(chunk, 0);
                this.currentBlock.CopyTo(chunk, blockWidth);

                this.currentBlock = this.md5.ComputeHash(chunk);
                this.currentOffset = 0;
            }
        }
    }
}
