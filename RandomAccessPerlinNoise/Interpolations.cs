// Copyright © John Gietzen. All Rights Reserved. This source is subject to the MIT license. Please see license.md for more information.

namespace RandomAccessPerlinNoise
{
    using System;

    public static class Interpolations
    {
        public static Interpolation Cosine { get; } = (a, b, t) =>
        {
            var ft = t * Math.PI;
            var f = (1 - Math.Cos(ft)) * 0.5;

            return a * (1 - f) + b * f;
        };

        public static Interpolation Linear { get; } = (a, b, t) => a * (1 - t) + b * t;

        public static Interpolation NearestNeighbor { get; } = (a, b, t) => t < 0.5 ? a : b;
    }
}
