using System;
using System.Windows.Forms;

namespace ImgComparer
{
    public partial class CompareView : Form
    {
        public bool CompareResult { get; private set; }

        public CompareView(Image image1, Image image2)
        {
            InitializeComponent();
            pictureBox1.Image = System.Drawing.Image.FromFile(image1.Path);
            pictureBox2.Image = System.Drawing.Image.FromFile(image2.Path);
            DialogResult = DialogResult.Cancel;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            CompareResult = false;
            DialogResult = DialogResult.OK;
            Close();
        }

        private void button2_Click(object sender, EventArgs e)
        {
            CompareResult = true;
            DialogResult = DialogResult.OK;
            Close();
        }
    }
}
