using ImgComparer.Model;
using ImgComparer.Tools;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;

namespace ImgComparer.UI
{
    public partial class Preview : Form
    {
        private readonly IList<Image> images;
        private readonly System.Drawing.Image[] realImages;
        private int index;
        private bool imageOwner;

        public bool Ok => pictureBox1.Image != null;

        public Preview(Image image, IList<Image> images, System.Drawing.Image[] realImages = null)
        {
            this.images = images;
            this.realImages = realImages;
            InitializeComponent();
            ChangeImage(images.IndexOf(image));
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (imageOwner)
                    pictureBox1.Image.Dispose();
                components?.Dispose();
            }
            base.Dispose(disposing);
        }

        private void ChangeImage(int index)
        {
            this.index = index;
            if (pictureBox1.Image != null && imageOwner)
            {
                pictureBox1.Image.Dispose();
                pictureBox1.Image = null;
                imageOwner = false;
            }

            Image image = images[index];
            try
            {
                if (realImages == null || realImages[index] == null)
                {
                    pictureBox1.Image = ImageLoader.Load(image.path);
                    imageOwner = true;
                }
                else
                    pictureBox1.Image = realImages[index];
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
                if (Application.OpenForms.OfType<Preview>().Count() == 1)
                    Close();
            }
            textBox1.Text = $"File:​\u200B{image.Filename.TrimExt(200)} Size:\u200B{Utility.BytesToString(image.size)} " +
                $"Resolution:\u200B{image.Resolution} Score:\u200B{image.Score} Index:\u200B{index + 1}/{images.Count}";
        }

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if (keyData == Keys.Escape)
            {
                Close();
                return true;
            }
            else if (!textBox1.Focused)
            {
                if (keyData == Keys.Left || keyData == Keys.Up)
                {
                    if (index > 0)
                        ChangeImage(index - 1);
                }
                else if (keyData == Keys.Right || keyData == Keys.Down)
                {
                    if (index < images.Count - 1)
                        ChangeImage(index + 1);
                }
                return true;
            }

            return base.ProcessCmdKey(ref msg, keyData);
        }

        private void Preview_MouseClick(object sender, MouseEventArgs e)
        {
            ActiveControl = null;
        }
    }
}
