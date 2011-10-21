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
            var w = 256;
            var h = 256;

            var perlin = new NoiseGenerator(0, 0.5, 5, new[] { w / 8, h / 8 }, false, Interpolator.Cosine);

            double[,] noise = new double[w, h];
            perlin.Fill(noise, new[] { 0L, 0L });

            var b = new Bitmap(w, h, PixelFormat.Format32bppArgb);
            var g = Graphics.FromImage(b);

            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    var c = (int)Math.Round(noise[x, y] * 255);
                    b.SetPixel(x, y, Color.FromArgb(c, c, c));
                }
            }

            b.Save("image.png", ImageFormat.Png);
        }
    }
}
