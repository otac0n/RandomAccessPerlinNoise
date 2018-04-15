// Copyright © John Gietzen. All Rights Reserved. This source is subject to the MIT license. Please see license.md for more information.

namespace RandomAccessPerlinNoise
{
    using System;
    using System.Drawing;
    using System.Drawing.Imaging;

    public class Program
    {
        public static void Main(string[] args)
        {
            var w = 1024;
            var h = 1024;
            var W = 2;
            var H = 2;

            var landGenerator = new NoiseGenerator(0, 0.5, 6, new[] { 4, 4 }, false, Interpolator.Cosine);

            var b = new Bitmap(W * w, H * h, PixelFormat.Format32bppArgb);
            var g = Graphics.FromImage(b);

            for (long Y = 0; Y < H; Y++)
            {
                for (long X = 0; X < W; X++)
                {
                    var noise = new double[w, h];
                    landGenerator.Fill(noise, new[] { X, Y });

                    for (var y = 0; y < h; y++)
                    {
                        for (var x = 0; x < w; x++)
                        {
                            var c = (int)Math.Round(noise[x, y] * 255);
                            b.SetPixel((int)(x + X * w), (int)(y + Y * h), Color.FromArgb(c > 190 ? c : 0, c >= 160 ? c : 0, c < 160 ? c : (c > 190 ? c : 0)));
                        }
                    }
                }
            }

            b.Save("image.png", ImageFormat.Png);
        }
    }
}
