using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using System.Drawing.Imaging;

namespace RandomAccessPerlinNoise
{
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
                    double[,] noise = new double[w, h];
                    landGenerator.Fill(noise, new[] { X, Y });

                    for (int y = 0; y < h; y++)
                    {
                        for (int x = 0; x < w; x++)
                        {
                            var c = (int)Math.Round(noise[x, y] * 255);
                            b.SetPixel((int)(x + X * w), (int)(y + Y * h), Color.FromArgb((c > 190 ? c : 0), c >= 160 ? c : 0, c < 160 ? c : (c > 190 ? c : 0)));
                        }
                    }
                }
            }

            b.Save("image.png", ImageFormat.Png);
        }
    }
}
