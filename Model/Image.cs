using System.IO;

namespace ImgComparer.Model
{
    public class Image
    {
        public string Filename { get; set; }
        public decimal score;
        public ulong hash;
        public long size;
        public int width, height;
        //---------------------
        // not saved (calculated or temporary)
        public string path;
        [OrderBy(nameof(ScoreValue))]
        public string Score => score.ToString("0.##");
        [OrderBy(nameof(SizeValue))]
        public string SizeText => Utility.BytesToString(size);
        [OrderBy(nameof(ResolutionValue))]
        public string Resolution => $"{width}x{height}";
        public decimal ScoreValue => score;
        public long SizeValue => size;
        public int ResolutionValue => width * height;
        public bool found;

        public void Write(BinaryWriter writer)
        {
            writer.Write(Filename);
            writer.Write(score);
            writer.Write(hash);
            writer.Write(size);
            writer.Write(width);
            writer.Write(height);
        }

        public void Read(BinaryReader reader)
        {
            Filename = reader.ReadString();
            score = reader.ReadDecimal();
            hash = reader.ReadUInt64();
            size = reader.ReadInt64();
            width = reader.ReadInt32();
            height = reader.ReadInt32();
        }
    }
}
