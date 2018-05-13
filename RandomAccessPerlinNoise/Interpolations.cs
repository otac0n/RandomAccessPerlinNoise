// Copyright © John Gietzen. All Rights Reserved. This source is subject to the MIT license. Please see license.md for more information.

namespace RandomAccessPerlinNoise
{
    using System;

    public static class Interpolations
    {
        public static Interpolation Cosine { get; } = new Interpolation((a, b, t) =>
        {
            var ft = t * Math.PI;
            var f = (1 - Math.Cos(ft)) * 0.5;

            return a * (1 - f) + b * f;
        });

        public static Interpolation Linear { get; } = new Interpolation((a, b, t) => a * (1 - t) + b * t);
    }
}
