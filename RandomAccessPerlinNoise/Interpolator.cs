namespace RandomAccessPerlinNoise
{
    using System;

    public class Interpolator : IInterpolator
    {
        private static readonly Interpolator linear = new Interpolator(
            (a, b, t) => a * (1 - t) + b * t
        );

        private static readonly Interpolator cosine = new Interpolator(
            (a, b, t) =>
            {
                var ft = t * Math.PI;
                var f = (1 - Math.Cos(ft)) * 0.5;

                return a * (1 - f) + b * f;
            }
        );

        private readonly Interpolation interpolation;

        public Interpolator(Interpolation interpolation)
        {
            if (interpolation == null)
            {
                throw new ArgumentNullException("interpolation");
            }

            this.interpolation = interpolation;
        }

        public static Interpolator Linear
        {
            get { return linear; }
        }

        public static Interpolator Cosine
        {
            get { return cosine; }
        }

        public double Interpolate(double a, double b, double t)
        {
            return this.interpolation(a, b, t);
        }
    }
}
