using System;
using System.Collections.Generic;
using System.ComponentModel;
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
            int level = 0;
            while (level < db.levels.Count)
            {
                List<Image> toSort = db.GetImagesToSort(level);
                if (toSort.Count == 0 && level == db.levels.Count - 1)
                    break;
                while (toSort.Count != 0)
                {
                    if (toSort.Count == 1)
                    {
                        Image image = toSort[0];
                        db.MoveImageToNextLevel(image, false);
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
                            db.MoveImageToNextLevel(image1, !compare.CompareResult);
                            db.MoveImageToNextLevel(image2, compare.CompareResult);
                        }
                        else
                        {
                            RefreshImages();
                            return;
                        }
                    }
                }

                db.MoveImagesToNextLevel(level);

                ++level;
            }

            db.Deflate();

            RefreshImages();
            MessageBox.Show("All sorted!");
        }

        private void dataGridView1_ColumnHeaderMouseClick(object sender, DataGridViewCellMouseEventArgs e)
        {
            /*var column = dataGridView1.Columns[e.ColumnIndex];
            SortOrder order;
            if (dataGridView1.SortedColumn == column)
            {
                order = dataGridView1.SortOrder;
                if (order == SortOrder.Descending)
                    order = SortOrder.Ascending;
                else
                    order = SortOrder.Ascending;
            }
            else
                order = SortOrder.Ascending;
            dataGridView1.Sort(column, order == SortOrder.Ascending ? ListSortDirection.Ascending : ListSortDirection.Descending);*/
        }
    }
}
