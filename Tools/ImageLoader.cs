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
