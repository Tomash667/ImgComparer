using System;
using System.Drawing;
using System.IO;

namespace ImgComparer.Tools
{
    public static class ImageLoader
    {
        public static Bitmap Load(string path)
        {
            string ext = Path.GetExtension(path);
            if (ext == ".webp")
            {
                byte[] bytes = File.ReadAllBytes(path);
                Imazen.WebP.SimpleDecoder decoder = new Imazen.WebP.SimpleDecoder();
                return decoder.DecodeFromBytes(bytes, bytes.Length);
            }
            else
            {
                try
                {
                    return new Bitmap(path);
                }
                catch (Exception)
                {
                    return null;
                }
            }
        }
    }
}
