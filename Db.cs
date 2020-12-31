using ImgComparer.Model;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace ImgComparer
{
    class Db
    {
        private readonly byte[] Sign = new byte[] { (byte)'I', (byte)'M', (byte)'G', 0xDB };
        private readonly string[] excludedExt = new string[] { ".db" };

        public Dictionary<string, Image> imagesDict = new Dictionary<string, Image>();
        public List<Image> newImages = new List<Image>();
        public List<Image> sortedImages = new List<Image>();
        public List<Image> missing = new List<Image>();
        public string path;
        private string filePath => $"{path}\\images.db";
        public List<Duplicate> duplicates;

        public bool Open(string path)
        {
            this.path = path;
            imagesDict = new Dictionary<string, Image>();
            newImages = new List<Image>();
            sortedImages = new List<Image>();
            duplicates = new List<Duplicate>();
            if (File.Exists(filePath))
            {
                Load();
                return true;
            }
            else
            {
                Save();
                return false;
            }
        }

        public void Save()
        {
            using (FileStream file = new FileStream(filePath, FileMode.Create))
            using (BinaryWriter writer = new BinaryWriter(file))
            {
                writer.Write(Sign);
                writer.Write(0);

                writer.Write(sortedImages.Count);
                foreach (Image image in sortedImages)
                    image.Write(writer);

                writer.Write(newImages.Count);
                foreach (Image image in newImages)
                    image.Write(writer);

                writer.Write(duplicates.Count);
                foreach (Duplicate duplicate in duplicates)
                {
                    writer.Write(duplicate.image1.Filename);
                    writer.Write(duplicate.image2.Filename);
                    writer.Write(duplicate.dist);
                }
            }
        }

        private void Load()
        {
            using (FileStream file = new FileStream(filePath, FileMode.Open))
            using (BinaryReader reader = new BinaryReader(file))
            {
                byte[] sign = reader.ReadBytes(4);
                int version = reader.ReadInt32();
                if (!sign.SequenceEqual(Sign) || version != 0)
                {
                    throw new Exception("Invalid images.db signature or version.");
                }
                LoadImages(ref sortedImages, reader);
                LoadImages(ref newImages, reader);

                int count = reader.ReadInt32();
                duplicates = new List<Duplicate>(count);
                for (int i = 0; i < count; ++i)
                {
                    Duplicate duplicate = new Duplicate
                    {
                        image1 = imagesDict[reader.ReadString()],
                        image2 = imagesDict[reader.ReadString()],
                        dist = reader.ReadInt32()
                    };
                    duplicates.Add(duplicate);
                }
            }
        }

        private void LoadImages(ref List<Image> imgs, BinaryReader reader)
        {
            int count = reader.ReadInt32();
            imgs = new List<Image>(count);
            for (int i = 0; i < count; ++i)
            {
                Image image = new Image();
                image.Read(reader);
                image.path = $"{path}\\{image.Filename}";
                imgs.Add(image);
                imagesDict[image.Filename] = image;
            }
        }

        public void Scan(Action<int> progress)
        {
            missing.Clear();
            foreach (KeyValuePair<string, Image> kvp in imagesDict)
                kvp.Value.found = false;

            // scan for new files, mark existing
            string[] files = Directory.GetFiles(path);
            List<string> newFiles = new List<string>();
            foreach (string file in files)
            {
                string filename = Path.GetFileName(file);
                string ext = Path.GetExtension(file);
                if (!excludedExt.Contains(ext))
                {
                    if (imagesDict.TryGetValue(filename, out Image image))
                        image.found = true;
                    else
                        newFiles.Add(filename);
                }
            }

            // add new files, check for duplicates
            if (newFiles.Count != 0)
            {
                int index = 0;
                foreach (string newFile in newFiles)
                {
                    Image image = new Image
                    {
                        Filename = newFile,
                        path = $"{path}\\{newFile}",
                        found = true
                    };
                    image.hash = DHash.Calculate(image.path);
                    foreach (Image image2 in imagesDict.Select(x => x.Value).Where(x => x.found))
                    {
                        int dist = DHash.Distance(image.hash, image2.hash);
                        if (dist <= 16)
                        {
                            duplicates.Add(new Duplicate
                            {
                                image1 = image,
                                image2 = image2,
                                dist = dist
                            });
                        }
                    }
                    imagesDict[newFile] = image;
                    newImages.Add(image);
                    ++index;
                    progress(100 * index / newFiles.Count);
                }
            }

            missing.AddRange(imagesDict.Select(x => x.Value).Where(x => !x.found));
        }

        public void CalculateScore()
        {
            if (sortedImages.Count <= 1)
                return;
            decimal mod = 99.0M / (sortedImages.Count - 1);
            decimal sum = 1;
            foreach (Image image in sortedImages)
            {
                image.ScoreValue = (int)sum;
                sum += mod;
            }
        }
    }
}
