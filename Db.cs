using System.Collections.Generic;
using System.IO;

namespace ImgComparer
{
    class Db
    {
        public Dictionary<string, Image> images = new Dictionary<string, Image>();

        public void LoadNew()
        {
            foreach (KeyValuePair<string, Image> kvp in images)
                kvp.Value.found = false;

            IEnumerable<string> files = Directory.EnumerateFiles("images/");
            foreach (string file in files)
            {
                string filename = Path.GetFileName(file);
                if (images.TryGetValue(filename, out Image image))
                    image.found = true;
                else
                {
                    image = new Image
                    {
                        Filename = filename,
                        found = true
                    };
                    images[filename] = image;
                }
            }
        }

        public (int, int) GetLevelToSort()
        {
            int min = 999, max = -1;
            foreach (KeyValuePair<string, Image> kvp in images)
            {
                Image image = kvp.Value;
                if (image.level < min)
                {
                    foreach (KeyValuePair<string, Image> kvp2 in images)
                    {
                        Image image2 = kvp2.Value;
                        if (image != image2 && image.level == image2.level && image.score == image2.score)
                        {
                            min = image.level;
                            break;
                        }
                    }
                }
                if (image.level > max)
                    max = image.level;
            }
            return (min == 999 ? -1 : min, max);
        }

        public List<Image> GetImagesToSort(int level)
        {
            List<Image> imagesWithLevel = new List<Image>();
            foreach (KeyValuePair<string, Image> kvp in images)
            {
                Image image = kvp.Value;
                if (image.level == level)
                    imagesWithLevel.Add(image);
            }
            List<Image> toSort = new List<Image>();
            foreach (Image image1 in imagesWithLevel)
            {
                foreach (Image image2 in imagesWithLevel)
                {
                    if (image1 != image2 && image1.score == image2.score)
                    {
                        toSort.Add(image1);
                        break;
                    }
                }
            }
            return toSort;
        }
    }
}
