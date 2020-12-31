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
        private SortableList<Image> images;

        public MainView()
        {
            InitializeComponent();
            dataGridView1.AutoGenerateColumns = false;

            db.LoadNew();
            RefreshImages();
        }

        private void RefreshImages()
        {
            images = new SortableList<Image>(db.images.Values);
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
            if (db.newImages.Count == 0)
            {
                MessageBox.Show("All sorted!");
                return;
            }

            while (db.newImages.Count != 0)
            {
                int index = Utility.Rand % db.newImages.Count;
                Image image = db.newImages[index];
                if (db.sortedImages.Count == 0)
                {
                    db.sortedImages.Add(image);
                    db.newImages.RemoveAt(index);
                    continue;
                }

                int L = 0, R = db.sortedImages.Count - 1;
                while (true)
                {
                    int mid = (L + R) / 2;
                    Image image2 = db.sortedImages[mid];
                    CompareView compare = new CompareView(image, image2);
                    DialogResult result = compare.ShowDialog(this);
                    if (result == DialogResult.OK)
                    {
                        if (L == R || mid == 0)
                        {
                            db.sortedImages.Insert(compare.CompareResult ? L : L + 1, image);
                            db.newImages.RemoveAt(index);
                            break;
                        }

                        if (!compare.CompareResult)
                            L = mid + 1;
                        else
                            R = mid - 1;
                    }
                    else
                        return;
                }
            }

            db.CalculateScore();
            RefreshImages();
            MessageBox.Show("All sorted!");
        }

        private void dataGridView1_ColumnHeaderMouseClick(object sender, DataGridViewCellMouseEventArgs e)
        {

        }

        private void Sort(string column, SortOrder order)
        {
            if(column == "Name")
            {

            }
            else
            {

            }
        }
    }
}
