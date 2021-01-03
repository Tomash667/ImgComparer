using ImgComparer.Model;
using System;
using System.IO;
using System.Windows.Forms;

namespace ImgComparer.UI
{
    public partial class CompareView : Form
    {
        private System.Drawing.Font normalFont, boldFont;

        public enum Result
        {
            Left,
            Right,
            Both
        }

        public Result CompareResult { get; private set; }

        public CompareView(bool conflict)
        {
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

        public DialogResult Show(Image image1, Image image2, int? dist)
        {
            pictureBox1.Image = System.Drawing.Image.FromFile(image1.path);
            pictureBox2.Image = System.Drawing.Image.FromFile(image2.path);
            DialogResult = DialogResult.None;
            textBox1.Text = $"File:​\u200B{image1.Filename} Size:\u200B{Utility.BytesToString(image1.size)} Resolution:\u200B{image1.Resolution}";
            textBox2.Text = $"File:\u200B{image2.Filename} Size:\u200B{Utility.BytesToString(image2.size)} Resolution:\u200B{image2.Resolution}";
            if (dist.HasValue)
            {
                btBoth.Text = $"Keep both\n({dist.Value} distance)";
                if (image2.size >= image1.size && image2.ResolutionValue >= image1.ResolutionValue)
                {
                    btRight.Font = boldFont;
                    btLeft.Font = normalFont;
                }
                else if (image1.size >= image2.size && image1.ResolutionValue >= image2.ResolutionValue)
                {
                    btLeft.Font = boldFont;
                    btRight.Font = normalFont;
                }
                else
                {
                    btLeft.Font = normalFont;
                    btRight.Font = normalFont;
                }
            }

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

        private void CompareView_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Escape)
                Close();
        }

        private void CompareView_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (e.CloseReason == CloseReason.UserClosing)
                DialogResult = DialogResult.Cancel;
        }
    }
}
