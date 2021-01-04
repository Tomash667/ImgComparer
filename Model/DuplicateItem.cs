namespace ImgComparer.Model
{
    public class DuplicateItem
    {
        public Image image;
        public string action;

        public string Filename => image.Filename;
        public string Score => image.Score;
        public string SizeText => image.SizeText;
        public string Resolution => image.Resolution;
        public string Action
        {
            get { return action; }
            set { action = value; }
        }
    }
}
