namespace RandomAccessPerlinNoise.Demo
{
    using System;
    using System.Diagnostics;

    internal class Program
    {
        private static void Main(string[] args)
        {
            var landGenerator = new NoiseGenerator(0, 0.5, 6, new[] { 2, 2 }, false, Interpolations.Cosine);
            landGenerator.GetValue(new[] { 0d, 0d });
            landGenerator.GetValue(new[] { 1d, 0d });
            landGenerator.GetValue(new[] { 0d, 1d });
            landGenerator.GetValue(new[] { 1d, 1d });

            var sw = Stopwatch.StartNew();

            var filled = new double[1024, 1024];
            for (var v = 0; v < 2; v++)
            {
                for (var u = 0; u < 2; u++)
                {
                    landGenerator.Fill(filled, new long[] { u, v });
                }
            }

            sw.Stop();
            Console.WriteLine($"Fill Elapsed: {sw.Elapsed}");

            sw.Restart();

            var coords = new double[2];
            for (var y = 0; y < 2 * 1024; y++)
            {
                coords[1] = y / 1024d;
                for (var x = 0; x < 2 * 2024; x++)
                {
                    coords[0] = x / 1024d;
                    landGenerator.GetValue(coords);
                }
            }

            sw.Stop();
            Console.WriteLine($"Random-Access Elapsed: {sw.Elapsed}");
        }
    }
}
