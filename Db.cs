using ImgComparer.Model;
using ImgComparer.Tools;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace ImgComparer
{
    public class Db
    {
        private readonly byte[] Sign = new byte[] { (byte)'I', (byte)'M', (byte)'G', 0xDB };
        private readonly string[] excludedExt = new string[] { ".db" };

        public Dictionary<string, Image> imagesDict = new Dictionary<string, Image>();
        public List<Image> newImages = new List<Image>();
        public List<Image> sortedImages = new List<Image>();
        public List<Image> missing = new List<Image>();
        public List<string> exactDuplicates = new List<string>();
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
            exactDuplicates.Clear();
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
                    System.Drawing.Bitmap bmp = new System.Drawing.Bitmap(image.path);
                    image.hash = DHash.Calculate(bmp);
                    image.width = bmp.Width;
                    image.height = bmp.Height;
                    image.size = new FileInfo(image.path).Length;
                    bmp.Dispose();

                    foreach (Image image2 in imagesDict.Select(x => x.Value).Where(x => x.found))
                    {
                        int dist = DHash.Distance(image.hash, image2.hash);
                        if (dist == 0 && image.size == image2.size && image.ResolutionValue == image2.ResolutionValue)
                        {
                            // exact duplicate, autodelete
                            exactDuplicates.Add(image.path);
                            image = null;
                            break;
                        }
                        else if (dist < DHash.Margin && !duplicates.Any(x => x.image1 == image2 && x.image2 == image))
                        {
                            duplicates.Add(new Duplicate
                            {
                                image1 = image,
                                image2 = image2,
                                dist = dist
                            });
                        }
                    }

                    if (image != null)
                    {
                        imagesDict[newFile] = image;
                        newImages.Add(image);
                    }
                    ++index;
                    progress(100 * index / newFiles.Count);
                }
            }

            missing.AddRange(imagesDict.Select(x => x.Value).Where(x => !x.found));
        }

        public void CalculateScore()
        {
            if (sortedImages.Count <= 1)
            {
                if (sortedImages.Count == 1)
                    sortedImages[0].score = 100;
                return;
            }
            decimal mod = 99.0M / (sortedImages.Count - 1);
            decimal sum = 1;
            foreach (Image image in sortedImages)
            {
                image.score = sum;
                sum += mod;
            }
        }

        public string GetScore(Image image)
        {
            decimal score;
            if (sortedImages.Count == 1)
                score = 100;
            else
            {
                int index = sortedImages.IndexOf(image);
                decimal mod = 99.0M / (sortedImages.Count - 1);
                score = 1 + mod * index;
            }
            return score.ToString("0.##");
        }

        public int GetUniqueDuplicates()
        {
            if (duplicates.Count < 2)
                return duplicates.Count * 2;
            HashSet<Image> dupImages = new HashSet<Image>();
            foreach (Duplicate dup in duplicates)
            {
                dupImages.Add(dup.image1);
                dupImages.Add(dup.image2);
            }
            return dupImages.Count;
        }

        public void RecalculateHashes(Action<int> progress)
        {
            int count = imagesDict.Count * 2;
            int index = 0;
            foreach (Image image in imagesDict.Select(x => x.Value))
            {
                System.Drawing.Bitmap bmp = new System.Drawing.Bitmap(image.path);
                image.hash = DHash.Calculate(bmp);
                bmp.Dispose();
                ++index;
                progress((int)(100.0f * index / count));
            }
            foreach (Image image in imagesDict.Select(x => x.Value))
            {
                foreach (Image image2 in imagesDict.Select(x => x.Value))
                {
                    if (image != image2)
                    {
                        int dist = DHash.Distance(image.hash, image2.hash);
                        if (dist < DHash.Margin && !duplicates.Any(x => x.image1 == image2 && x.image2 == image))
                        {
                            duplicates.Add(new Duplicate
                            {
                                image1 = image,
                                image2 = image2,
                                dist = dist
                            });
                        }
                    }
                }
                ++index;
                progress((int)(100.0f * index / count));
            }
        }
    }
}
