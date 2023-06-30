using FFMpegCore;
using FFMpegCore.Extensions.System.Drawing.Common;
using System.Drawing;

namespace ImgComparer.Tools
{
    class Ffmpeg
    {
        public static void SetPath(string path)
        {
            GlobalFFOptions.Configure(new FFOptions { BinaryFolder = path });
        }

        public static Bitmap GetBitmap(string path)
        {
            return FFMpegImage.Snapshot(path);
        }
    }
}
