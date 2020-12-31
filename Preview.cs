using ImgComparer.Model;
using System;
using System.Windows.Forms;

namespace ImgComparer
{
    public partial class Preview : Form
    {
        public bool Ok => pictureBox1.Image != null;

        public Preview(Image image)
        {
            InitializeComponent();
            try
            {
                pictureBox1.Image = System.Drawing.Image.FromFile(image.path);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                pictureBox1.Image.Dispose();
                components?.Dispose();
            }
            base.Dispose(disposing);
        }

        private void Preview_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Escape)
                Close();
        }
    }
}
