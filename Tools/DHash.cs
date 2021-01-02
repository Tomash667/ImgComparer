using System.Drawing;
using System.Drawing.Imaging;

namespace ImgComparer.Tools
{
    public static class DHash
    {
        public static ulong Calculate(Bitmap image)
        {
            Bitmap grayscale = ToGrayscale(image);
            Bitmap bmp = new Bitmap(grayscale, 9, 8);
            grayscale.Dispose();
            ulong hash = 0;
            ulong bit = 1;
            for (int y = 0; y < 8; ++y)
            {
                int previous = bmp.GetPixel(0, y).R;
                for (int x = 1; x < 9; ++x)
                {
                    int current = bmp.GetPixel(x, y).R;
                    if (previous > current)
                        hash |= bit;
                    bit = bit << 1;
                    previous = current;
                }
            }
            bmp.Dispose();
            return hash;
        }

        private static Bitmap ToGrayscale(Bitmap original)
        {
            //create a blank bitmap the same size as original
            Bitmap newBitmap = new Bitmap(original.Width, original.Height);

            //get a graphics object from the new image
            Graphics g = Graphics.FromImage(newBitmap);

            //create the grayscale ColorMatrix
            ColorMatrix colorMatrix = new ColorMatrix(
                new float[][]
                {
                    new float[] {.3f, .3f, .3f, 0, 0},
                    new float[] {.59f, .59f, .59f, 0, 0},
                    new float[] {.11f, .11f, .11f, 0, 0},
                    new float[] {0, 0, 0, 1, 0},
                    new float[] {0, 0, 0, 0, 1}
                });

            //create some image attributes
            ImageAttributes attributes = new ImageAttributes();

            //set the color matrix attribute
            attributes.SetColorMatrix(colorMatrix);

            //draw the original image on the new image
            //using the grayscale color matrix
            g.DrawImage(original, new Rectangle(0, 0, original.Width, original.Height),
               0, 0, original.Width, original.Height, GraphicsUnit.Pixel, attributes);

            //dispose the Graphics object
            g.Dispose();
            return newBitmap;
        }

        private static readonly int[] counts = new int[] { 0, 1, 1, 2, 1, 2, 2, 3, 1, 2, 2, 3, 2, 3, 3, 4 };

        public static int Distance(ulong hash1, ulong hash2)
        {
            int res = 0;
            int shift = 0;
            ulong mask = 0xF;
            for (int i = 0; i < 16; i++)
            {
                int h1 = (int)((hash1 & mask) >> shift);
                int h2 = (int)((hash2 & mask) >> shift);
                if (h1 != h2)
                    res += counts[h1 ^ h2];
                mask <<= 4;
                shift += 4;
            }
            return res;
        }
    }
}
