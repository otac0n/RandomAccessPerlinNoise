// Copyright © John Gietzen. All Rights Reserved. This source is subject to the MIT license. Please see license.md for more information.

namespace RandomAccessPerlinNoise
{
    public interface IInterpolator
    {
        double Interpolate(double a, double b, double t);
    }
}
