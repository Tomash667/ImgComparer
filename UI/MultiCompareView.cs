using ImgComparer.Model;
using ImgComparer.Tools;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows.Forms;

namespace ImgComparer.UI
{
    public partial class MultiCompareView : Form
    {
        private Db db;
        private List<DuplicateItem> items;
        private BindingList<DuplicateItem> bindingList;
        public int replaced, removed;
        private bool inInit;

        public MultiCompareView(Db db, List<DuplicateItem> items)
        {
            this.db = db;
            this.items = items;

            InitializeComponent();

            bindingList = new BindingList<DuplicateItem>(items);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                pictureBox1.Image?.Dispose();
                components?.Dispose();
            }
            base.Dispose(disposing);
        }

        private void MultiCompareView_Load(object sender, EventArgs e)
        {
            List<Model.Action> actions = new List<Model.Action>
            {
                new Model.Action{ Text = "Keep", Value = "keep" },
                new Model.Action{ Text = "Remove", Value = "remove" }
            };
            for (int i = 0; i < items.Count; ++i)
            {
                items[i].action = "keep";
                actions.Add(new Model.Action { Text = $"Replace with {items[i].image.Filename}", Value = $"replace{i}" });
            }
            var column = dataGridView1.Columns["Action"] as DataGridViewComboBoxColumn;
            column.DisplayMember = "Text";
            column.ValueMember = "Value";
            column.DataSource = actions;

            inInit = true;
            dataGridView1.AutoGenerateColumns = false;
            dataGridView1.DataSource = bindingList;
            inInit = false;
        }

        private void dataGridView1_SelectionChanged(object sender, EventArgs e)
        {
            if (dataGridView1.SelectedRows.Count == 0)
                return;

            int index = dataGridView1.SelectedRows[0].Index, pos = 0;
            DuplicateItem item = items[index];
            foreach (DuplicateItem item2 in items)
            {
                if (item == item2)
                    item2.Similarity = "---";
                else
                {
                    int dist = DHash.Distance(item.image.hash, item2.image.hash);
                    item2.Similarity = $"{DHash.ToSimilarity(dist)}%";
                }

                if (!inInit)
                {
                    bindingList.ResetItem(pos);
                    ++pos;
                }
            }

            if (pictureBox1.Image != null)
                pictureBox1.Image.Dispose();
            try
            {
                pictureBox1.Image = ImageLoader.Load(item.image.path);
            }
            catch (Exception)
            {
                pictureBox1.Image = null;
            }
        }

        private void btSwitch_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.Abort;
            Close();
        }

        private void btApply_Click(object sender, EventArgs e)
        {
            try
            {
                if (items.All(x => x.action == "remove"))
                    throw new Exception("All images set to remove!");
                List<string> replaces = new List<string>();
                for (int i = 0; i < items.Count; ++i)
                {
                    DuplicateItem item = items[i];
                    if (item.action.StartsWith("replace"))
                    {
                        int index = int.Parse(item.action.Substring("replace".Length));
                        if (replaces.Contains(item.action))
                            throw new Exception($"{item.Filename} - Replace with '{items[index]}' already used!");
                        if (index == i)
                            throw new Exception($"{item.Filename} - Can't replace with itself!");
                        if (items[index].action != "keep")
                            throw new Exception($"{item.Filename} - Can't replace with '{items[index]}', image not kept!");
                        if (item.image.score == 0)
                            throw new Exception($"{item.Filename} - Can't replace with '{items[index]}', image not scored - use remove!");
                        replaces.Add(item.action);
                    }
                    else if (item.action == "remove" && item.image.score != 0)
                        throw new Exception($"{item.Filename} - Can't remove scored image, must replace.");
                }

                pictureBox1.Image?.Dispose();
                pictureBox1.Image = null;

                for (int i = 0; i < items.Count; ++i)
                {
                    DuplicateItem item = items[i];
                    if (item.action.StartsWith("replace"))
                    {
                        int index = int.Parse(item.action.Substring("replace".Length));
                        int imgIndex = db.sortedImages.IndexOf(item.image);
                        Image replacement = items[index].image;
                        replacement.score = item.image.score;
                        db.sortedImages[imgIndex] = replacement;
                        db.imagesDict.Remove(item.image.Filename);
                        db.duplicates.RemoveAll(x => x.image1 == item.image || x.image2 == item.image);
                        Utility.MarkToDelete(item.image.path);
                        ++replaced;
                    }
                    else if (item.action == "remove")
                    {
                        db.newImages.Remove(item.image);
                        db.imagesDict.Remove(item.Filename);
                        db.duplicates.RemoveAll(x => x.image1 == item.image || x.image2 == item.image);
                        Utility.MarkToDelete(item.image.path);
                        ++removed;
                    }
                }

                DialogResult = DialogResult.OK;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
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
            if (contextMenuStrip1.SourceControl == dataGridView1)
            {
                if (dataGridView1.CurrentRow == null || dataGridView1.CurrentRow.Index == -1)
                    e.Cancel = true;
                var point = dataGridView1.PointToClient(Cursor.Position);
                var hit = dataGridView1.HitTest(point.X, point.Y);
                if (hit.Type != DataGridViewHitTestType.Cell && hit.Type != DataGridViewHitTestType.RowHeader)
                    e.Cancel = true;
            }
        }

        private void openInExplorerToolStripMenuItem_Click(object sender, EventArgs e)
        {
            int index = dataGridView1.CurrentRow.Index;
            Image image = items[index].image;
            Utility.OpenInExplorer(image.path);
        }

        private void dataGridView1_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            var editingControl = dataGridView1.EditingControl as DataGridViewComboBoxEditingControl;
            if (editingControl != null)
                editingControl.DroppedDown = true;
        }

        private void MultiCompareView_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Escape)
                Close();
        }

        private void previewToolStripMenuItem_Click(object sender, EventArgs e)
        {
            List<Image> images = items.Select(x => x.image).ToList();
            System.Drawing.Image[] realImages = new System.Drawing.Image[items.Count];
            int index = dataGridView1.SelectedRows[0].Index;
            realImages[index] = pictureBox1.Image;
            using (Preview preview = new Preview(images[index], images, realImages))
            {
                if (preview.Ok)
                    preview.ShowDialog(this);
            }
        }

        private void MultiCompareView_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (DialogResult == DialogResult.None)
                DialogResult = DialogResult.Cancel;
        }
    }
}
