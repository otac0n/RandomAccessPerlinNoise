// Copyright © John Gietzen. All Rights Reserved. This source is subject to the MIT license. Please see license.md for more information.
namespace RandomAccessPerlinNoise.Tests
{
    using Xunit;

    public class NoiseGeneratorTests
    {
        [Fact]
        public void GetValue_CalledIndependently_ReturnsTheSameValueAsFill()
        {
            const int W = 4;
            const int H = 4;

            var noiseGen = new NoiseGenerator(0, 0.5, 3, new[] { W, H }, false, Interpolations.Linear);
            var expected = new double[W, H];
            noiseGen.Fill(expected, new long[] { 0, 0 });

            var actual = new double[W, H];
            for (var y = 0; y < W; y++)
            {
                for (var x = 0; x < H; x++)
                {
                    actual[x, y] = noiseGen.GetValue(new[] { (double)x / W, (double)y / H });
                }
            }

            Assert.Equal(expected, actual);
        }
    }
}
