namespace ImgComparer.Model
{
    public class Duplicate
    {
        public Image image1;
        public Image image2;
        public int dist;

        public bool IsSameImage(Duplicate duplicate)
        {
            return image1 == duplicate.image1
                || image1 == duplicate.image2
                || image2 == duplicate.image1
                || image2 == duplicate.image2;
        }
    }
}
