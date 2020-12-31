﻿using ImgComparer.Model;
using System;
using System.ComponentModel;
using System.IO;
using System.Windows.Forms;

namespace ImgComparer
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
            using (Preview preview = new Preview(image))
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

            bool anythingDone = false;
            while (db.newImages.Count != 0)
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
                    using (CompareView compare = new CompareView(image, image2, null))
                    {
                        DialogResult result = compare.ShowDialog(this);
                        if (result == DialogResult.OK)
                        {
                            if (L == R || mid == 0)
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
                            if (anythingDone)
                                UpdateStatus();
                            return;
                        }
                    }
                }
            }

            UpdateStatus();
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

            foreach (Image image in db.missing)
            {
                if (image.ScoreValue != 0)
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
                $"Removed images: {db.missing.Count}");
        }

        private (int, int) ResolveDuplicates()
        {
            int replaced = 0, removed = 0;

            while (db.duplicates.Count > 0)
            {
                Duplicate duplicate = db.duplicates[0];
                CompareView.Result compareResult;
                using (CompareView compare = new CompareView(duplicate.image1, duplicate.image2, duplicate.dist))
                {
                    DialogResult result = compare.ShowDialog(this);
                    if (result != DialogResult.OK)
                        break;
                    compareResult = compare.CompareResult;
                }

                switch (compareResult)
                {
                case CompareView.Result.Left:
                    // replace existing
                    if (duplicate.image2.ScoreValue != 0)
                    {
                        duplicate.image1.ScoreValue = duplicate.image2.ScoreValue;
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
    }
}
