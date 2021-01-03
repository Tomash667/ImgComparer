using ImgComparer.Model;
using System;
using System.ComponentModel;
using System.IO;
using System.Reflection;
using System.Windows.Forms;

namespace ImgComparer.UI
{
    public partial class MainView : Form
    {
        private Db db = new Db();
        private SortableList<Image> images;
        private int imageCount;
        private bool changes;

        public MainView()
        {
            InitializeComponent();
            dataGridView1.AutoGenerateColumns = false;
            imagesToolStripMenuItem.Enabled = false;
            saveToolStripMenuItem.Enabled = false;
            sortToolStripMenuItem1.Enabled = false;
            resolveDuplicatesToolStripMenuItem.Enabled = false;
            toolStripStatusLabel1.Text = "";
            typeof(DataGridView).InvokeMember(
               "DoubleBuffered",
               BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.SetProperty,
               null,
               dataGridView1,
               new object[] { true });
        }

        private void RefreshImages()
        {
            images = new SortableList<Image>(db.imagesDict.Values);
            dataGridView1.DataSource = images;
        }

        private void dataGridView1_CellDoubleClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex == -1)
                return;
            Image image = images[e.RowIndex];
            using (Preview preview = new Preview(image, images))
            {
                if (preview.Ok)
                    preview.ShowDialog(this);
            }
        }

        private void sortToolStripMenuItem_Click(object sender, System.EventArgs e)
        {
            if (db.newImages.Count == 0)
            {
                MessageBox.Show("All sorted!");
                return;
            }

            CompareView compare = new CompareView(false);
            Enabled = false;
            compare.Owner = this;
            bool anythingDone = false, cancel = false;
            while (db.newImages.Count != 0 && !cancel)
            {
                int index = Utility.Rand % db.newImages.Count;
                Image image = db.newImages[index];
                if (db.sortedImages.Count == 0)
                {
                    db.sortedImages.Add(image);
                    db.newImages.RemoveAt(index);
                    anythingDone = true;
                    continue;
                }

                int L = 0, R = db.sortedImages.Count - 1;
                while (true)
                {
                    int mid = (L + R) / 2;
                    Image image2 = db.sortedImages[mid];
                    DialogResult result = compare.Show(image, image2, null);
                    if (result == DialogResult.OK)
                    {
                        if (L == R || L == mid)
                        {
                            db.sortedImages.Insert(compare.CompareResult == CompareView.Result.Right ? L : L + 1, image);
                            db.newImages.RemoveAt(index);
                            anythingDone = true;
                            break;
                        }

                        if (compare.CompareResult == CompareView.Result.Left)
                            L = mid + 1;
                        else
                            R = mid - 1;
                    }
                    else
                    {
                        cancel = true;
                        break;
                    }
                }
            }

            compare.Close();
            Enabled = true;
            Activate();
            if (anythingDone)
                UpdateStatus();
            if (!cancel)
                MessageBox.Show("All sorted!");
        }

        private void openToolStripMenuItem_Click(object sender, EventArgs e)
        {
            DialogResult result = vistaFolderBrowserDialog1.ShowDialog(this);
            if (result == DialogResult.OK)
            {
                db.Open(vistaFolderBrowserDialog1.SelectedPath);
                imagesToolStripMenuItem.Enabled = true;
                saveToolStripMenuItem.Enabled = true;
                UpdateStatus(changed: false, calculateScore: false);
            }
        }

        private void UpdateStatus(bool calculateScore = true, bool changed = true, bool refreshImages = true)
        {
            if (calculateScore)
                db.CalculateScore();
            if (refreshImages)
                RefreshImages();
            changes = changed;
            Text = $"ImgComparer - {db.path}{(changes ? "*" : "")}";
            toolStripStatusLabel1.Text = $"Sorted images:{db.sortedImages.Count}/{db.imagesDict.Count} Duplicates:{db.duplicates.Count}";
            sortToolStripMenuItem1.Enabled = (db.newImages.Count != 0);
            resolveDuplicatesToolStripMenuItem.Enabled = (db.duplicates.Count != 0);
        }

        private void saveToolStripMenuItem_Click(object sender, EventArgs e)
        {
            db.Save();
            UpdateStatus(calculateScore: false, changed: false, refreshImages: false);
        }

        private void scanDirectoryToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Enabled = false;
            imageCount = db.newImages.Count;

            Ookii.Dialogs.WinForms.ProgressDialog dialog = new Ookii.Dialogs.WinForms.ProgressDialog();
            dialog.Text = "Scanning directory...";
            dialog.DoWork += (k, v) => db.Scan(p => dialog.ReportProgress(p));
            dialog.RunWorkerCompleted += Dialog_RunWorkerCompleted;
            dialog.Show(this);
        }

        private void Dialog_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            int newImages = db.newImages.Count - imageCount;
            Enabled = true;

            foreach (string path in db.exactDuplicates)
                DeleteSafe(path);

            foreach (Image image in db.missing)
            {
                if (image.score != 0)
                    db.sortedImages.Remove(image);
                else
                    db.newImages.Remove(image);
                db.imagesDict.Remove(image.Filename);
            }
            db.duplicates.RemoveAll(x => !x.image1.found || !x.image2.found);

            (int replaced, int removed) = ResolveDuplicates();
            newImages -= removed;

            if (newImages > 0 || replaced > 0 || removed > 0 || db.missing.Count > 0)
                UpdateStatus();
            MessageBox.Show(this, $"Scanning complete!\n" +
                $"New images: {newImages}\n" +
                $"Replaced images: {replaced}\n" +
                $"Possible duplicates: {db.duplicates.Count}\n" +
                $"Removed duplicates: {db.exactDuplicates.Count}\n" +
                $"Missing images: {db.missing.Count}");
        }

        private (int, int) ResolveDuplicates()
        {
            if (db.duplicates.Count == 0)
                return (0, 0);

            int replaced = 0, removed = 0;
            CompareView compare = new CompareView(true);
            Enabled = false;

            while (db.duplicates.Count > 0)
            {
                Duplicate duplicate = db.duplicates[0];
                DialogResult result = compare.Show(duplicate.image1, duplicate.image2, duplicate.dist);
                if (result != DialogResult.OK)
                    break;

                switch (compare.CompareResult)
                {
                case CompareView.Result.Left:
                    // replace existing
                    if (duplicate.image2.score != 0)
                    {
                        duplicate.image1.score = duplicate.image2.score;
                        int index = db.sortedImages.IndexOf(duplicate.image2);
                        db.sortedImages[index] = duplicate.image1;
                        db.newImages.Remove(duplicate.image1);
                        db.imagesDict.Remove(duplicate.image2.Filename);
                        DeleteSafe(duplicate.image2.path);
                        db.duplicates.RemoveAll(x => x.image1 == duplicate.image2 || x.image2 == duplicate.image2);
                        ++replaced;
                    }
                    else
                    {
                        db.newImages.Remove(duplicate.image2);
                        db.imagesDict.Remove(duplicate.image2.Filename);
                        DeleteSafe(duplicate.image2.path);
                        db.duplicates.RemoveAll(x => x.image1 == duplicate.image2 || x.image2 == duplicate.image2);
                        ++removed;
                    }
                    break;
                case CompareView.Result.Right:
                    // keep existing
                    db.newImages.Remove(duplicate.image1);
                    db.imagesDict.Remove(duplicate.image1.Filename);
                    DeleteSafe(duplicate.image1.path);
                    db.duplicates.RemoveAll(x => x.image1 == duplicate.image1 || x.image2 == duplicate.image1);
                    ++removed;
                    break;
                case CompareView.Result.Both:
                    // keep both
                    db.duplicates.RemoveAt(0);
                    break;
                }
            }

            compare.Close();
            Enabled = true;
            Activate();
            return (replaced, removed);
        }

        private void MainView_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (changes)
            {
                DialogResult result = MessageBox.Show(this, "Quit without saving?", "Quit", MessageBoxButtons.YesNo);
                if (result == DialogResult.No)
                    e.Cancel = true;
            }
        }

        private void resolveDuplicatesToolStripMenuItem_Click(object sender, EventArgs e)
        {
            (int replaced, int removed) = ResolveDuplicates();
            if (replaced > 0 || removed > 0)
                UpdateStatus();
        }

        private void DeleteSafe(string path)
        {
            while (true)
            {
                try
                {
                    File.Delete(path);
                    return;
                }
                catch (Exception ex)
                {
                    DialogResult result = MessageBox.Show(this, $"Failed to delete file {path}.\n{ex.Message}", "Retry?", MessageBoxButtons.RetryCancel);
                    if (result == DialogResult.Cancel)
                        return;
                }
            }
        }

        private void previewToolStripMenuItem_Click(object sender, EventArgs e)
        {
            int index = dataGridView1.CurrentRow.Index;
            Image image = images[index];
            using (Preview preview = new Preview(image, images))
            {
                if (preview.Ok)
                    preview.ShowDialog(this);
            }
        }

        private void dataGridView1_CellMouseDown(object sender, DataGridViewCellMouseEventArgs e)
        {
            if (e.ColumnIndex != -1 && e.RowIndex != -1 && e.Button == MouseButtons.Right)
            {
                DataGridViewCell c = (sender as DataGridView)[e.ColumnIndex, e.RowIndex];
                if (!c.Selected)
                {
                    c.DataGridView.ClearSelection();
                    c.DataGridView.CurrentCell = c;
                    c.Selected = true;
                }
            }
        }

        private void contextMenuStrip1_Opening(object sender, CancelEventArgs e)
        {
            if (dataGridView1.CurrentRow == null || dataGridView1.CurrentRow.Index == -1)
                e.Cancel = true;
            var point = dataGridView1.PointToClient(Cursor.Position);
            var hit = dataGridView1.HitTest(point.X, point.Y);
            if (hit.Type != DataGridViewHitTestType.Cell && hit.Type != DataGridViewHitTestType.RowHeader)
                e.Cancel = true;
        }

        private void openInExplorerToolStripMenuItem_Click(object sender, EventArgs e)
        {
            int index = dataGridView1.CurrentRow.Index;
            Image image = images[index];
            System.Diagnostics.Process.Start("explorer.exe", $"/select, \"{image.path}\"");
        }

        private void reverseSearchToolStripMenuItem_Click(object sender, EventArgs e)
        {
            int index = dataGridView1.CurrentRow.Index;
            Image image = images[index];
            string[] urls = Tools.ReverseImageSearch.GetSearchUrl(image);
            foreach (string url in urls)
                System.Diagnostics.Process.Start(url);
        }
    }
}
