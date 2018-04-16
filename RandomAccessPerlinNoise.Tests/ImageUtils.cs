namespace RandomAccessPerlinNoise.Tests
{
    using System.Drawing;
    using System.Drawing.Imaging;
    using System.Runtime.InteropServices;

    internal static class ImageUtils
    {
        public static Bitmap Copy(this Image image)
        {
            Bitmap copy = null;
            try
            {
                copy = new Bitmap(image.Width, image.Height);
                using (var g = Graphics.FromImage(copy))
                {
                    g.DrawImage(image, 0, 0, image.Width, image.Height);
                }

                var result = copy;
                copy = null;
                return result;
            }
            finally
            {
                if (copy != null)
                {
                    copy.Dispose();
                }
            }
        }

        public static Color[,] GetColors(this Image image)
        {
            var bitmap = image as Bitmap;
            if (bitmap != null)
            {
                return bitmap.GetColors();
            }

            using (var copy = image.Copy())
            {
                return copy.GetColors();
            }
        }

        public static Color[,] GetColors(this Bitmap bitmap)
        {
            var size = bitmap.Size;
            var colors = new Color[size.Width, size.Height];
            var data = bitmap.LockBits(new Rectangle(Point.Empty, size), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);

            for (var y = 0; y < data.Height; y++)
            {
                var row = new int[data.Width];
                Marshal.Copy(data.Scan0 + y * data.Stride, row, 0, data.Width);
                for (var x = 0; x < data.Width; x++)
                {
                    colors[x, y] = Color.FromArgb(row[x]);
                }
            }

            bitmap.UnlockBits(data);
            return colors;
        }
    }
}
