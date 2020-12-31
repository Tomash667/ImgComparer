using System.IO;

namespace ImgComparer.Model
{
    public class Image
    {
        public string Filename { get; set; }
        public string path;
        public int ScoreValue { get; set; }
        public ulong hash;
        public bool found;

        public void Write(BinaryWriter writer)
        {
            writer.Write(Filename);
            writer.Write(ScoreValue);
            writer.Write(hash);
        }

        public void Read(BinaryReader reader)
        {
            Filename = reader.ReadString();
            ScoreValue = reader.ReadInt32();
            hash = reader.ReadUInt64();
        }
    }
}
