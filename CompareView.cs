using ImgComparer.Model;
using System;
using System.IO;
using System.Windows.Forms;

namespace ImgComparer
{
    public partial class CompareView : Form
    {
        public enum Result
        {
            Left,
            Right,
            Both
        }

        public Result CompareResult { get; private set; }

        public CompareView(Image image1, Image image2, int? conflict)
        {
            InitializeComponent();
            pictureBox1.Image = System.Drawing.Image.FromFile(image1.path);
            pictureBox2.Image = System.Drawing.Image.FromFile(image2.path);
            DialogResult = DialogResult.Cancel;
            if (conflict.HasValue)
            {
                btLeft.Text = "Replace with this";
                btRight.Text = "Keep original";
                btBoth.Text = $"Keep both \n({conflict.Value} distance)";
                Text = "Possible duplicate found";
            }
            else
            {
                btBoth.Hide();
                Text = "Which image is better?";
            }
            long size = new FileInfo(image1.path).Length;
            label1.Text = $"File:{image1.Filename}, Size:{Utility.BytesToString(size)}, Resolution:{pictureBox1.Width}x{pictureBox1.Height}";
            size = new FileInfo(image2.path).Length;
            label2.Text = $"File:{image2.Filename}, Size:{Utility.BytesToString(size)}, Resolution:{pictureBox2.Width}x{pictureBox2.Height}";
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                pictureBox1.Image.Dispose();
                pictureBox2.Image.Dispose();
                components?.Dispose();
            }
            base.Dispose(disposing);
        }

        private void btLeft_Click(object sender, EventArgs e)
        {
            CompareResult = Result.Left;
            DialogResult = DialogResult.OK;
            Close();
        }

        private void btRight_Click(object sender, EventArgs e)
        {
            CompareResult = Result.Right;
            DialogResult = DialogResult.OK;
            Close();
        }

        private void btBoth_Click(object sender, EventArgs e)
        {
            CompareResult = Result.Both;
            DialogResult = DialogResult.OK;
            Close();
        }
    }
}
