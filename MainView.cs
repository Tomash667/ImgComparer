using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;

namespace ImgComparer
{
    public partial class MainView : Form
    {
        private Db db = new Db();
        private List<Image> images;

        public MainView()
        {
            InitializeComponent();
            dataGridView1.AutoGenerateColumns = false;

            db.LoadNew();
            RefreshImages();
        }

        private void RefreshImages()
        {
            images = db.images.Values.ToList();
            dataGridView1.DataSource = null;
            dataGridView1.DataSource = images;
        }

        private void dataGridView1_CellDoubleClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex == -1)
                return;
            Image image = images[e.RowIndex];
            Preview preview = new Preview(image);
            if (preview.Ok)
                preview.ShowDialog(this);
        }

        private void sortToolStripMenuItem_Click(object sender, System.EventArgs e)
        {
            (int level, int max) = db.GetLevelToSort();

            while (true)
            {
                if (level == -1)
                {
                    MessageBox.Show("All sorted!");
                    RefreshImages();
                    return;
                }

                List<Image> toSort = db.GetImagesToSort(level);
                if (level > max && toSort.Count == 0)
                {
                    MessageBox.Show("All sorted!");
                    RefreshImages();
                    return;
                }

                while (toSort.Count > 0)
                {
                    if (toSort.Count == 1)
                    {
                        Image image = toSort[0];
                        image.level++;
                        image.score *= 2;
                        image.baseScore *= 2;
                        toSort.Clear();
                    }
                    else
                    {
                        int index = Utility.Rand % toSort.Count;
                        Image image1 = toSort[index];
                        toSort.RemoveAt(index);
                        Image[] pick = toSort.Where(x => x.score == image1.score).ToArray();
                        index = Utility.Rand % pick.Length;
                        Image image2 = pick[index];
                        toSort.Remove(image2);

                        CompareView compare = new CompareView(image1, image2);
                        DialogResult result = compare.ShowDialog(this);
                        if (result == DialogResult.OK)
                        {
                            int value;
                            if (image1.baseScore == 0)
                            {
                                value = 1;
                                image1.baseScore = 1;
                                image2.baseScore = 1;
                            }
                            else
                            {
                                value = image1.baseScore;
                                image1.baseScore *= 2;
                                image2.baseScore *= 2;
                            }
                            if (compare.CompareResult)
                            {
                                // image2 is better
                                image2.score += value;
                            }
                            else
                            {
                                // image1 is better
                                image1.score += value;
                            }
                            image1.ScoreValue = (float)image1.score / image1.baseScore;
                            image1.level++;
                            image2.ScoreValue = (float)image2.score / image2.baseScore;
                            image2.level++;
                        }
                        else
                        {
                            RefreshImages();
                            return;
                        }
                    }
                }

                ++level;
            }
        }
    }
}
