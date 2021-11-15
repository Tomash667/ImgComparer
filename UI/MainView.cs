using ImgComparer.Model;
using ImgComparer.Tools;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
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
        private Properties.Settings settings;
        private List<string> recentProjects, invalidFiles;

        public MainView()
        {
            InitializeComponent();
            dataGridView1.AutoGenerateColumns = false;
            imagesToolStripMenuItem.Enabled = false;
            saveToolStripMenuItem.Enabled = false;
            exploreFolderToolStripMenuItem.Enabled = false;
            sortToolStripMenuItem1.Enabled = false;
            resolveDuplicatesToolStripMenuItem.Enabled = false;
            toolStripStatusLabel1.Text = "";
            typeof(DataGridView).InvokeMember(
               "DoubleBuffered",
               BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.SetProperty,
               null,
               dataGridView1,
               new object[] { true });
            settings = Properties.Settings.Default;
            recentProjects = settings.Recent.Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries).ToList();
            PopulateRecentProjects();
            images = new SortableList<Image>();
            dataGridView1.DataSource = images;
            dataGridView1.Sort(dataGridView1.Columns[0], ListSortDirection.Ascending);
        }

        private void MainView_Load(object sender, EventArgs e)
        {
            if (settings.AutoOpen && recentProjects.Count > 0)
            {
                db.Open(recentProjects[0]);
                imagesToolStripMenuItem.Enabled = true;
                saveToolStripMenuItem.Enabled = true;
                exploreFolderToolStripMenuItem.Enabled = true;
                UpdateStatus(changed: false, calculateScore: false);
                dataGridView1.Sort(dataGridView1.Columns[0], ListSortDirection.Ascending);
            }
        }

        private void PopulateRecentProjects()
        {
            var items = projectToolStripMenuItem.DropDownItems;
            while (true)
            {
                var item = items[items.Count - 1];
                if (item == toolStripSeparator1)
                    break;
                items.Remove(item);
            }

            int index = 0;
            foreach (string recentProject in recentProjects)
            {
                ToolStripMenuItem item = new ToolStripMenuItem($"{index + 1}: {recentProject}");
                item.Name = $"recent{index}";
                item.Click += OpenRecentProject_Click;
                projectToolStripMenuItem.DropDownItems.Add(item);
                ++index;
            }
        }

        private void RefreshImages()
        {
            Image selected = null;
            if (dataGridView1.SelectedRows.Count == 1)
            {
                int index = dataGridView1.SelectedRows[0].Index;
                selected = images[index];
            }
            var column = dataGridView1.SortedColumn;
            var order = dataGridView1.SortOrder;
            images.ResetItems(db.imagesDict.Values);
            if (selected != null)
            {
                int index = images.IndexOf(selected);
                if (index != -1)
                    dataGridView1.Rows[index].Selected = true;
            }
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

            CompareView compare = new CompareView(db, false);
            Enabled = false;
            compare.Owner = this;
            bool anythingDone = false, cancel = false;
            while (db.newImages.Count != 0 && !cancel)
            {
                int index = Utility.Rand % db.newImages.Count;
                Image image = db.newImages[index];
                if (db.sortedImages.Count == 0)
                {
                    image.score = 1;
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
                    DialogResult result = compare.Show(image, image2);
                    if (result == DialogResult.OK)
                    {
                        if (L == R || L == mid)
                        {
                            image.score = 1;
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
            if (!ConfirmOpen())
                return;

            DialogResult result = vistaFolderBrowserDialog1.ShowDialog(this);
            if (result == DialogResult.OK)
            {
                string path = vistaFolderBrowserDialog1.SelectedPath;
                db.Open(path);
                imagesToolStripMenuItem.Enabled = true;
                saveToolStripMenuItem.Enabled = true;
                exploreFolderToolStripMenuItem.Enabled = true;
                tFilter.Clear();
                images.ResetItems();
                dataGridView1.Sort(dataGridView1.Columns[0], ListSortDirection.Ascending);
                UpdateStatus(changed: false, calculateScore: false);
                UpdateRecent(path);
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
            toolStripStatusLabel1.Text = $"Sorted images:{db.sortedImages.Count}/{db.imagesDict.Count} Duplicates:{db.GetUniqueDuplicates()}";
            sortToolStripMenuItem1.Enabled = (db.newImages.Count != 0);
            resolveDuplicatesToolStripMenuItem.Enabled = (db.duplicates.Count != 0);
        }

        private void UpdateRecent(string path)
        {
            if (recentProjects.Contains(path))
            {
                if (path == recentProjects[0])
                    return;
                recentProjects.Remove(path);
                recentProjects.Insert(0, path);
            }
            else
            {
                recentProjects.Insert(0, path);
                if (recentProjects.Count > 5)
                    recentProjects.RemoveAt(5);
            }
            settings.Recent = string.Join(";", recentProjects);
            settings.Save();
            PopulateRecentProjects();
        }

        private void saveToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Utility.DeleteFiles(this);
            db.Save();
            UpdateStatus(calculateScore: false, changed: false, refreshImages: false);
        }

        private void scanDirectoryToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Enabled = false;
            imageCount = db.newImages.Count;
            invalidFiles = new List<string>(db.invalidFiles);

            Ookii.Dialogs.WinForms.ProgressDialog dialog = new Ookii.Dialogs.WinForms.ProgressDialog();
            dialog.Text = "Scanning directory...";
            dialog.DoWork += (k, v) => db.Scan(p => dialog.ReportProgress(p));
            dialog.RunWorkerCompleted += Dialog_RunWorkerCompleted;
            dialog.Show(this);
        }

        private void Dialog_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if (e.Error != null)
                MessageBox.Show(this, $"Error during scan!\n{e.Error}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);

            string[] newInvalidFiles = db.invalidFiles.Except(invalidFiles).ToArray();
            if (newInvalidFiles.Length != 0)
                MessageBox.Show(this, $"Failed to load some files:\n{string.Join("\n", newInvalidFiles)}", "Invalid files", MessageBoxButtons.OK, MessageBoxIcon.Warning);

            int newImages = db.newImages.Count - imageCount;
            Enabled = true;

            foreach (string path in db.exactDuplicates)
                Utility.MarkToDelete(path);

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

            if (newImages > 0 || replaced > 0 || removed > 0 || db.missing.Count > 0 || db.exactDuplicates.Count > 0)
                UpdateStatus();
            MessageBox.Show(this, $"Scanning complete!\n" +
                $"New images: {newImages}\n" +
                $"Replaced images: {replaced}\n" +
                $"Possible duplicates: {db.GetUniqueDuplicates()}\n" +
                $"Removed duplicates: {db.exactDuplicates.Count}\n" +
                $"Missing images: {db.missing.Count}");
        }

        private (int, int) ResolveDuplicates()
        {
            if (db.duplicates.Count == 0)
                return (0, 0);

            int replaced = 0, removed = 0, indexOf = 1, totalCount = db.duplicates.Count;
            CompareView compare = new CompareView(db, true);
            compare.Owner = this;
            Enabled = false;

            bool cancel = false;
            while (db.duplicates.Count > 0 && !cancel)
            {
                Duplicate duplicate = db.duplicates[0];
                int? complex = null;
                if (db.duplicates.Any(x => x != duplicate && x.IsSameImage(duplicate)))
                    complex = db.duplicates.Count(x => x != duplicate && x.IsSameImage(duplicate));
                DialogResult result = compare.Show(duplicate.image1, duplicate.image2, duplicate.dist, complex, indexOf, totalCount);
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
                        Utility.MarkToDelete(duplicate.image2.path);
                        indexOf += db.duplicates.RemoveAll(x => x.image1 == duplicate.image2 || x.image2 == duplicate.image2);
                        ++replaced;
                    }
                    else
                    {
                        db.newImages.Remove(duplicate.image2);
                        db.imagesDict.Remove(duplicate.image2.Filename);
                        Utility.MarkToDelete(duplicate.image2.path);
                        indexOf += db.duplicates.RemoveAll(x => x.image1 == duplicate.image2 || x.image2 == duplicate.image2);
                        ++removed;
                    }
                    break;
                case CompareView.Result.Right:
                    // keep existing
                    db.newImages.Remove(duplicate.image1);
                    db.imagesDict.Remove(duplicate.image1.Filename);
                    Utility.MarkToDelete(duplicate.image1.path);
                    indexOf += db.duplicates.RemoveAll(x => x.image1 == duplicate.image1 || x.image2 == duplicate.image1);
                    ++removed;
                    break;
                case CompareView.Result.Both:
                    // keep both
                    db.duplicates.RemoveAt(0);
                    ++indexOf;
                    break;
                case CompareView.Result.Complex:
                    {
                        List<Duplicate> dups = db.duplicates.Where(x => x.IsSameImage(duplicate)).ToList();
                        List<Image> dupImages = new List<Image>();
                        foreach (Duplicate dup in dups)
                        {
                            if (!dupImages.Contains(dup.image1))
                                dupImages.Add(dup.image1);
                            if (!dupImages.Contains(dup.image2))
                                dupImages.Add(dup.image2);
                        }
                        List<DuplicateItem> dupItems = dupImages
                            .Select(x => new DuplicateItem { image = x })
                            .ToList();
                        compare.Hide();
                        using (MultiCompareView view = new MultiCompareView(db, dupItems))
                        {
                            DialogResult result2 = view.ShowDialog(this);
                            if (result2 == DialogResult.Cancel)
                                cancel = true;
                            else if (result2 == DialogResult.OK)
                            {
                                indexOf += db.duplicates.RemoveAll(x => dups.Contains(x));
                                replaced += view.replaced;
                                removed += view.removed;
                            }
                        }
                    }
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

        private bool ConfirmOpen()
        {
            if (!changes)
                return true;

            DialogResult result = MessageBox.Show(this, "Open project without saving?", "Open", MessageBoxButtons.YesNo);
            return result == DialogResult.Yes;
        }

        private void resolveDuplicatesToolStripMenuItem_Click(object sender, EventArgs e)
        {
            (int replaced, int removed) = ResolveDuplicates();
            if (replaced > 0 || removed > 0)
                UpdateStatus();
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
            else
            {
                int index = dataGridView1.CurrentRow.Index;
                Image image = images[index];
                resetScoreToolStripMenuItem.Enabled = image.score > 0;
            }
        }

        private void openInExplorerToolStripMenuItem_Click(object sender, EventArgs e)
        {
            int index = dataGridView1.CurrentRow.Index;
            Image image = images[index];
            Utility.OpenInExplorer(image.path);
        }

        private void reverseSearchToolStripMenuItem_Click(object sender, EventArgs e)
        {
            int index = dataGridView1.CurrentRow.Index;
            Image image = images[index];
            string[] urls = ReverseImageSearch.GetSearchUrl(image);
            if (urls == null)
                MessageBox.Show(this, "Reverse image search require image blob connection string set in options.");
            else
            {
                foreach (string url in urls)
                    System.Diagnostics.Process.Start(url);
            }
        }

        private void recalculateHashesToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show("Are sure you want to recalculate hashes?\nIt's a slow operation that will check all duplicates again.",
                "Recalculate hashes", MessageBoxButtons.YesNo) == DialogResult.No)
                return;

            Enabled = false;

            Ookii.Dialogs.WinForms.ProgressDialog dialog = new Ookii.Dialogs.WinForms.ProgressDialog();
            dialog.Text = "Recalculating hashes...";
            dialog.DoWork += (k, v) => db.RecalculateHashes(p => dialog.ReportProgress(p));
            dialog.RunWorkerCompleted += Dialog_RunWorkerCompleted1;
            dialog.Show(this);
        }

        private void Dialog_RunWorkerCompleted1(object sender, RunWorkerCompletedEventArgs e)
        {
            if (e.Error != null)
                MessageBox.Show(this, $"Error during recalculating hashes!\n{e.Error}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            Enabled = true;
            ResolveDuplicates();
            UpdateStatus();
        }

        private void OpenRecentProject_Click(object sender, EventArgs e)
        {
            if (!ConfirmOpen())
                return;

            var toolstrip = sender as ToolStripMenuItem;
            int index = int.Parse(toolstrip.Name.Substring("recent".Length));
            string path = recentProjects[index];
            db.Open(path);
            imagesToolStripMenuItem.Enabled = true;
            saveToolStripMenuItem.Enabled = true;
            exploreFolderToolStripMenuItem.Enabled = true;
            tFilter.Clear();
            images.ResetItems();
            dataGridView1.Sort(dataGridView1.Columns[0], ListSortDirection.Ascending);
            UpdateStatus(changed: false, calculateScore: false);
            UpdateRecent(path);
        }

        private void resetScoreToolStripMenuItem_Click(object sender, EventArgs e)
        {
            int index = dataGridView1.CurrentRow.Index;
            Image image = images[index];
            if (MessageBox.Show(this, "Are you sure?", "Reset score", MessageBoxButtons.YesNo) == DialogResult.Yes)
            {
                image.score = 0;
                db.newImages.Add(image);
                db.sortedImages.Remove(image);
                UpdateStatus();
            }
        }

        private void tFilter_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                e.Handled = true;
                e.SuppressKeyPress = true;
                btApply_Click(sender, new EventArgs());
            }
        }

        private void btApply_Click(object sender, EventArgs e)
        {
            var column = dataGridView1.SortedColumn;
            SortOrder order = dataGridView1.SortOrder;
            StringFilter filter = new StringFilter(tFilter.Text);
            if (filter.Required)
                images.ApplyFilter(image => filter.Filter(image.Filename));
            else
            {
                tFilter.Clear();
                images.ClearFilter();
            }
        }

        private void btClear_Click(object sender, EventArgs e)
        {
            tFilter.Clear();
            images.ClearFilter();
        }

        private void removeToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (dataGridView1.CurrentRow == null)
                return;

            int index = dataGridView1.CurrentRow.Index;
            Image image = images[index];
            if (MessageBox.Show(this, "Are you sure?", "Remove", MessageBoxButtons.YesNo) == DialogResult.Yes)
            {
                if (image.score == 0)
                    db.newImages.Remove(image);
                else
                    db.sortedImages.Remove(image);
                db.imagesDict.Remove(image.Filename);
                Utility.MarkToDelete(image.path);
                UpdateStatus(calculateScore: image.score != 0);
            }
        }

        private void optionsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            OptionsView options = new OptionsView();
            options.ShowDialog(this);
        }

        private void exploreFolderToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Utility.OpenInExplorer(db.path);
        }
    }
}
