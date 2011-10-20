namespace RandomAccessPerlinNoise
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;

    public class NoiseGenerator
    {
        private readonly long seed;
        private readonly IInterpolator interpolator;
        private readonly double persistence;
        private readonly int levels;
        private readonly bool smooth;

        public NoiseGenerator(long seed, double persistence, int levels, bool smooth, IInterpolator interpolator)
        {
            this.seed = seed;

            if (persistence < 0.0 || persistence > 1.0)
            {
                throw new ArgumentOutOfRangeException("persistence");
            }

            this.persistence = persistence;

            if (levels <= 0)
            {
                throw new ArgumentOutOfRangeException("levels");
            }

            this.levels = levels;

            this.smooth = smooth;

            if (interpolator == null)
            {
                throw new ArgumentNullException("interpolator");
            }

            this.interpolator = interpolator;
        }

        public void Fill(Array array)
        {
            if (array == null)
            {
                throw new ArgumentNullException("array");
            }

        }
    }
}
