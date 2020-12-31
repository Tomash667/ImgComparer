using System.Collections.Generic;
using System.IO;

namespace ImgComparer
{
    class Db
    {
        public Dictionary<string, Image> images = new Dictionary<string, Image>();
        public List<Image> newImages = new List<Image>();
        public List<Image> sortedImages = new List<Image>();

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
                    newImages.Add(image);
                }
            }
        }

        public void CalculateScore()
        {
            if (sortedImages.Count <= 1)
                return;
            decimal mod = 100.0M / (sortedImages.Count - 1);
            decimal sum = 0;
            foreach (Image image in sortedImages)
            {
                image.ScoreValue = (int)sum;
                sum += mod;
            }
        }
    }
}
