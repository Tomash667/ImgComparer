using ImgComparer.Model;
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
        public int replaced, removed;

        public MultiCompareView(Db db, List<DuplicateItem> items)
        {
            this.db = db;
            this.items = items;

            InitializeComponent();
            dataGridView1.AutoGenerateColumns = false;
            dataGridView1.DataSource = new BindingList<DuplicateItem>(items);
            dataGridView1.Rows[0].Selected = true;
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

        private void dataGridView1_SelectionChanged(object sender, EventArgs e)
        {
            if (dataGridView1.SelectedRows.Count == 0)
                return;

            int index = dataGridView1.SelectedRows[0].Index;
            Image image = items[index].image;
            if (pictureBox1.Image != null)
                pictureBox1.Image.Dispose();
            try
            {
                pictureBox1.Image = System.Drawing.Image.FromFile(image.path);
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
                        if (items[index].image.score == 0)
                            throw new Exception($"{item.Filename} - Can't replace with '{items[index]}', image not scored - use remove!");
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
                        Utility.SafeDelete(this, item.image.path);
                        ++replaced;
                    }
                    else if (item.action == "remove")
                    {
                        db.newImages.Remove(item.image);
                        db.imagesDict.Remove(item.Filename);
                        db.duplicates.RemoveAll(x => x.image1 == item.image || x.image2 == item.image);
                        Utility.SafeDelete(this, item.image.path);
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

        private void MultiCompareView_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (DialogResult == DialogResult.None)
                DialogResult = DialogResult.Cancel;
        }
    }
}
