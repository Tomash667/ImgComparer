using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace ImgComparer
{
    class Db
    {
        public Dictionary<string, Image> images = new Dictionary<string, Image>();
        public List<List<Image>> levels = new List<List<Image>>();

        public void LoadNew()
        {
            if (levels.Count == 0)
                levels.Add(new List<Image>());

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
                    levels[0].Add(image);
                }
            }
        }

        public List<Image> GetImagesToSort(int level)
        {
            List<Image> imagesWithLevel = levels[level];
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

        public void MoveImageToNextLevel(Image image, bool increase)
        {
            levels[image.level].Remove(image);
            if (image.level + 1 == levels.Count)
                levels.Add(new List<Image>());
            levels[image.level + 1].Add(image);
            image.level++;
            image.score *= 2;
            if (increase)
                image.score++;
            if (image.baseScore == 0)
                image.baseScore = 2;
            else
                image.baseScore *= 2;
            image.ScoreValue = (float)image.score / (image.baseScore - 1);
        }

        public void MoveImagesToNextLevel(int level)
        {
            List<Image> imgAtLevel = levels[level];
            List<Image> nextLevel = levels[level + 1];
            foreach (Image image in imgAtLevel)
            {
                nextLevel.Add(image);
                image.score *= 2;
                if (image.baseScore == 0)
                    image.baseScore = 2;
                else
                    image.baseScore *= 2;
                image.ScoreValue = (float)image.score / (image.baseScore - 1);
            }
            imgAtLevel.Clear();
        }

        public void Deflate()
        {
            int reqSize = Utility.NextPow2(images.Count);
            int reqLevel = Utility.Pow2Exponent(reqSize);
            if (reqLevel + 1 < levels.Count)
            {
                List<Image> imgs = levels[levels.Count - 1];
                imgs.Sort((x, y) => x.ScoreValue.CompareTo(y.ScoreValue));
                double increment = (double)(reqSize - 1) / imgs.Count;
                double sum = 0.0;
                foreach (Image image in imgs)
                {
                    sum += increment;
                    image.score = (int)increment;
                    image.level = reqLevel;
                    image.baseScore = reqSize;
                    image.ScoreValue = (float)image.score / (image.baseScore - 1);
                }
                levels[reqLevel] = imgs;
                while (levels.Count > reqLevel - 1)
                    levels.RemoveAt(levels.Count - 1);
            }
        }
    }
}
