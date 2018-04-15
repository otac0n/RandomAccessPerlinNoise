// Copyright © John Gietzen. All Rights Reserved. This source is subject to the MIT license. Please see license.md for more information.

namespace RandomAccessPerlinNoise
{
    using System;

    public class Interpolator : IInterpolator
    {
        private readonly Interpolation interpolation;

        public Interpolator(Interpolation interpolation)
        {
            if (interpolation == null)
            {
                throw new ArgumentNullException(nameof(interpolation));
            }

            this.interpolation = interpolation;
        }

        public static Interpolator Cosine { get; } = new Interpolator((a, b, t) =>
        {
            var ft = t * Math.PI;
            var f = (1 - Math.Cos(ft)) * 0.5;

            return a * (1 - f) + b * f;
        });

        public static Interpolator Linear { get; } = new Interpolator((a, b, t) => a * (1 - t) + b * t);

        public double Interpolate(double a, double b, double t)
        {
            return this.interpolation(a, b, t);
        }
    }
}
