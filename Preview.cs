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
                pictureBox1.Image = System.Drawing.Image.FromFile(image.Path);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }
        }

        private void Preview_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Escape)
                Close();
        }
    }
}
