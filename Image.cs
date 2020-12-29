namespace ImgComparer
{
    public class Image
    {
        public string Filename { get; set; }
        public string Path => $"images/{Filename}";
        public int level, score, baseScore;
        public float ScoreValue { get; set; }
        public bool found;
    }
}
