﻿using ImgComparer.Model;
using ImgComparer.Tools;
using System;
using System.Collections.Generic;
using System.Windows.Forms;

namespace ImgComparer.UI
{
    public partial class CompareView : Form
    {
        private Db db;
        private System.Drawing.Font normalFont, boldFont;
        private Image image1, image2;

        public enum Result
        {
            Left,
            Right,
            Both,
            Complex
        }

        public Result CompareResult { get; private set; }

        public CompareView(Db db, bool conflict)
        {
            this.db = db;

            InitializeComponent();

            if (conflict)
            {
                btLeft.Text = "Replace with this";
                btRight.Text = "Keep original";
                btBoth.Text = $"Keep both";
                Text = "Possible duplicate found";
                normalFont = btLeft.Font;
                boldFont = new System.Drawing.Font(normalFont, System.Drawing.FontStyle.Bold);
            }
            else
            {
                btBoth.Hide();
                btComplex.Hide();
                tableLayoutPanel2.ColumnStyles[0].Width = 50;
                tableLayoutPanel2.ColumnStyles[1].SizeType = SizeType.Absolute;
                tableLayoutPanel2.ColumnStyles[1].Width = 0;
                tableLayoutPanel2.ColumnStyles[2].Width = 50;
                Text = "Which image is better?";
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing && components != null)
                components.Dispose();
            base.Dispose(disposing);
        }

        public DialogResult Show(Image image1, Image image2, int? dist = null, int? complex = null, int? indexOf = null, int? totalCount = null)
        {
            this.image1 = image1;
            this.image2 = image2;

            pictureBox1.Image = ImageLoader.Load(image1.path);
            pictureBox2.Image = ImageLoader.Load(image2.path);
            DialogResult = DialogResult.None;
            textBox1.Text = $"File:​\u200B{image1.Filename.TrimExt(100)} Size:\u200B{Utility.BytesToString(image1.size)} Resolution:\u200B{image1.Resolution}";
            if (image1.score > 0)
                textBox2.Text += $" Score:\u200B{db.GetScore(image1)}";
            textBox2.Text = $"File:\u200B{image2.Filename.TrimExt(100)} Size:\u200B{Utility.BytesToString(image2.size)} Resolution:\u200B{image2.Resolution}";
            if (image2.score > 0)
                textBox2.Text += $" Score:\u200B{db.GetScore(image2)}";
            if (dist.HasValue)
            {
                btBoth.Text = $"Keep both\n({DHash.ToSimilarity(dist.Value)}% similarity)";

                int cmp = image1.Compare(image2);
                if (cmp == 0)
                {
                    btLeft.Font = normalFont;
                    btRight.Font = normalFont;
                }
                else if (cmp == 1)
                {
                    btLeft.Font = boldFont;
                    btRight.Font = normalFont;
                }
                else
                {
                    btRight.Font = boldFont;
                    btLeft.Font = normalFont;
                }

                if (complex.HasValue)
                {
                    btComplex.Enabled = true;
                    btComplex.Text = $"Multi compare ({complex.Value})";
                    btComplex.Font = boldFont;
                }
                else
                {
                    btComplex.Enabled = false;
                    btComplex.Text = "Multi compare";
                    btComplex.Font = normalFont;
                }
            }

            if (indexOf.HasValue)
                Text = $"Possible duplicate found ({indexOf} of {totalCount})";

            Show();
            while (DialogResult == DialogResult.None)
            {
                System.Threading.Thread.Sleep(10);
                Application.DoEvents();
            }

            pictureBox1.Image.Dispose();
            pictureBox1.Image = null;
            pictureBox2.Image.Dispose();
            pictureBox2.Image = null;

            return DialogResult;
        }

        private void btLeft_Click(object sender, EventArgs e)
        {
            CompareResult = Result.Left;
            DialogResult = DialogResult.OK;
        }

        private void btRight_Click(object sender, EventArgs e)
        {
            CompareResult = Result.Right;
            DialogResult = DialogResult.OK;
        }

        private void btBoth_Click(object sender, EventArgs e)
        {
            CompareResult = Result.Both;
            DialogResult = DialogResult.OK;
        }

        private void btComplex_Click(object sender, EventArgs e)
        {
            CompareResult = Result.Complex;
            DialogResult = DialogResult.OK;
        }

        private void CompareView_KeyDown(object sender, KeyEventArgs e)
        {
            switch (e.KeyCode)
            {
            case Keys.Escape:
                Close();
                break;
            case Keys.D1:
                btLeft_Click(null, null);
                break;
            case Keys.D2:
                btBoth_Click(null, null);
                break;
            case Keys.D3:
                btRight_Click(null, null);
                break;
            }
        }

        private void previewToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var pictureBox = ((ContextMenuStrip)(sender as ToolStripMenuItem).Owner).SourceControl;
            pictureBox1_DoubleClick(pictureBox, e);
        }

        private void pictureBox1_DoubleClick(object sender, EventArgs e)
        {
            var pictureBox = sender as PictureBox;
            Image image = (pictureBox == pictureBox1 ? image1 : image2);
            List<Image> images = new List<Image> { image1, image2 };
            System.Drawing.Image[] realImages = new System.Drawing.Image[] { pictureBox1.Image, pictureBox2.Image };
            using (Preview preview = new Preview(image, images, realImages))
            {
                if (preview.Ok)
                    preview.ShowDialog(this);
            }
        }

        private void openInExplorerToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var pictureBox = ((ContextMenuStrip)(sender as ToolStripMenuItem).Owner).SourceControl;
            Image image = (pictureBox == pictureBox1 ? image1 : image2);
            Utility.OpenInExplorer(image.path);
        }

        private void CompareView_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (e.CloseReason == CloseReason.UserClosing)
                DialogResult = DialogResult.Cancel;
        }
    }
}
