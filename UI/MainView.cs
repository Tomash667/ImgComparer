﻿using ImgComparer.Model;
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
        private ImgComparer.Properties.Settings settings;
        private List<string> recentProjects;

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
            settings = ImgComparer.Properties.Settings.Default;
            autoOpenLastToolStripMenuItem.Checked = settings.AutoOpen;
            recentProjects = settings.Recent.Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries).ToList();
            PopulateRecentProjects();
            if (settings.AutoOpen && recentProjects.Count > 0)
            {
                db.Open(recentProjects[0]);
                imagesToolStripMenuItem.Enabled = true;
                saveToolStripMenuItem.Enabled = true;
                UpdateStatus(changed: false, calculateScore: false);
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
                    DialogResult result = compare.Show(image, image2);
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
            if (!ConfirmOpen())
                return;

            DialogResult result = vistaFolderBrowserDialog1.ShowDialog(this);
            if (result == DialogResult.OK)
            {
                string path = vistaFolderBrowserDialog1.SelectedPath;
                db.Open(path);
                imagesToolStripMenuItem.Enabled = true;
                saveToolStripMenuItem.Enabled = true;
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
            toolStripStatusLabel1.Text = $"Sorted images:{db.sortedImages.Count}/{db.imagesDict.Count} Duplicates:{db.duplicates.Count}";
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
                Utility.SafeDelete(this, path);

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
            compare.Owner = this;
            Enabled = false;

            bool cancel = false;
            while (db.duplicates.Count > 0 && !cancel)
            {
                Duplicate duplicate = db.duplicates[0];
                bool complex = db.duplicates.Any(x => x != duplicate && x.IsSameImage(duplicate));
                DialogResult result = compare.Show(duplicate.image1, duplicate.image2, duplicate.dist, complex);
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
                        Utility.SafeDelete(this, duplicate.image2.path);
                        db.duplicates.RemoveAll(x => x.image1 == duplicate.image2 || x.image2 == duplicate.image2);
                        ++replaced;
                    }
                    else
                    {
                        db.newImages.Remove(duplicate.image2);
                        db.imagesDict.Remove(duplicate.image2.Filename);
                        Utility.SafeDelete(this, duplicate.image2.path);
                        db.duplicates.RemoveAll(x => x.image1 == duplicate.image2 || x.image2 == duplicate.image2);
                        ++removed;
                    }
                    break;
                case CompareView.Result.Right:
                    // keep existing
                    db.newImages.Remove(duplicate.image1);
                    db.imagesDict.Remove(duplicate.image1.Filename);
                    Utility.SafeDelete(this, duplicate.image1.path);
                    db.duplicates.RemoveAll(x => x.image1 == duplicate.image1 || x.image2 == duplicate.image1);
                    ++removed;
                    break;
                case CompareView.Result.Both:
                    // keep both
                    db.duplicates.RemoveAt(0);
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
                                db.duplicates.RemoveAll(x => dups.Contains(x));
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
            string[] urls = Tools.ReverseImageSearch.GetSearchUrl(image);
            foreach (string url in urls)
                System.Diagnostics.Process.Start(url);
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
            Enabled = true;
            ResolveDuplicates();
            UpdateStatus();
        }

        private void autoOpenLastToolStripMenuItem_Click(object sender, EventArgs e)
        {
            settings.AutoOpen = !settings.AutoOpen;
            settings.Save();
            autoOpenLastToolStripMenuItem.Checked = settings.AutoOpen;
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
            UpdateStatus(changed: false, calculateScore: false);
            UpdateRecent(path);
        }
    }
}
