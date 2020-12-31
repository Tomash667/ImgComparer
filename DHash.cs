using System.Drawing;
using System.Drawing.Imaging;

namespace ImgComparer
{
    public static class DHash
    {
        public static long Calculate(System.Drawing.Image image)
        {
            Bitmap bmp = ToGrayscale((Bitmap)image);
            bmp = new Bitmap(bmp, 9, 8);
            long hash = 0;
            long bit = 1;
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

        private static int Distance(long hash1, long hash2)
        {
            int res = 0;
            int shift = 0;
            long mask = 0xF;
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
