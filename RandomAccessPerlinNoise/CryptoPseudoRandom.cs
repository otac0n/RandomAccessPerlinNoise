using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RandomAccessPerlinNoise
{
    public class CryptoPseudoRandom
    {
        private readonly byte[] seed;
        private byte[] block;

        public CryptoPseudoRandom(byte[] seed)
        {
            if (seed == null)
            {
                throw new ArgumentNullException("seed");
            }

            this.seed = seed;
        }

        public double NextDouble()
        {
            return 1.0;
        }
    }
}
