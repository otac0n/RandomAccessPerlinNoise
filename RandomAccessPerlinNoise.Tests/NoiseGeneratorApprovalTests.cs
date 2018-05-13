// Copyright © John Gietzen. All Rights Reserved. This source is subject to the MIT license. Please see license.md for more information.

namespace RandomAccessPerlinNoise.Tests
{
    using System;
    using System.Drawing;
    using System.Drawing.Imaging;
    using System.IO;
    using System.Reflection;
    using System.Text.RegularExpressions;
    using Xunit;
    using Xunit.Abstractions;

    public class NoiseGeneratorApprovalTests
    {
        private ITest test;

        public NoiseGeneratorApprovalTests(ITestOutputHelper output)
        {
            var type = output.GetType();
            var testMember = type.GetField("test", BindingFlags.Instance | BindingFlags.NonPublic);
            this.test = (ITest)testMember.GetValue(output);
        }

        [Fact]
        public void NoiseGenerator_Always_ReturnsTheSameDataForTheSameSeed()
        {
            var w = 1024;
            var h = 1024;
            var W = 2;
            var H = 2;

            var landGenerator = new NoiseGenerator(0, 0.5, 6, new[] { 4, 4 }, false, Interpolations.Cosine);

            using (var bitmap = new Bitmap(W * w, H * h, PixelFormat.Format32bppArgb))
            using (var graphics = Graphics.FromImage(bitmap))
            {
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
                                bitmap.SetPixel((int)(x + X * w), (int)(y + Y * h), Color.FromArgb(c > 190 ? c : 0, c >= 160 ? c : 0, c < 160 ? c : (c > 190 ? c : 0)));
                            }
                        }
                    }
                }

                this.WriteAndAssertResult(bitmap);
            }
        }

        private static string SanitizeName(string testName)
        {
            return Regex.Replace(testName.Replace('"', '\''), "[" + Regex.Escape(new string(Path.GetInvalidFileNameChars())) + "]", "_");
        }

        private void WriteAndAssertResult(Bitmap bitmap)
        {
            var className = this.test.TestCase.TestMethod.TestClass.Class.Name;
            var testPath = Path.Combine(className, SanitizeName(this.test.TestCase.DisplayName.Substring(className.Length + 1)) + ".png");
            var actualPath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "ActualResults", testPath);
            var expectedPath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "ExpectedResults", testPath);
            Directory.CreateDirectory(Path.GetDirectoryName(actualPath));
            bitmap.Save(actualPath);

            Assert.True(File.Exists(expectedPath));
            using (var expected = Image.FromFile(expectedPath))
            {
                Assert.Equal(expected.Width, bitmap.Width);
                Assert.Equal(expected.Height, bitmap.Height);
                var actualColors = bitmap.GetColors();
                var expectedColors = expected.GetColors();
                Assert.Equal(expectedColors, actualColors);
            }
        }
    }
}
