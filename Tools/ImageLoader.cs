using ImageMagick;
using System;
using System.Drawing;
using System.IO;

namespace ImgComparer.Tools
{
    public static class ImageLoader
    {
        public static Bitmap Load(string path)
        {
            try
            {
                string ext = Path.GetExtension(path);
                if (ext == ".mp4")
                {
                    return Ffmpeg.GetBitmap(path);
                }
                else if (ext == ".webp")
                {
                    using (var magickImages = new MagickImageCollection(path))
                    {
                        var ms = new MemoryStream();
                        magickImages.Write(ms, MagickFormat.Png);
                        Bitmap bitmap = new Bitmap(ms)
                        {
                            // keep MemoryStream from being garbage collected while Bitmap is in use
                            Tag = ms
                        };
                        return bitmap;
                    }
                }
                else
                {
                    return new Bitmap(path);
                }
            }
            catch (Exception)
            {
                return null;
            }
        }
    }
}
