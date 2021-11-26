using ImgComparer.Tools;
using System;
using System.Windows.Forms;

namespace ImgComparer.UI
{
    public partial class OptionsView : Form
    {
        private Properties.Settings settings;

        public OptionsView()
        {
            InitializeComponent();

            settings = Properties.Settings.Default;
            cbAutoOpen.Checked = settings.AutoOpen;
            tbBlobUrl.Text = settings.ImageBlob;
            tbExcludedExts.Text = settings.ExcludedExt;
        }

        private void btOk_Click(object sender, EventArgs e)
        {
            settings.AutoOpen = cbAutoOpen.Checked;
            settings.ImageBlob = tbBlobUrl.Text.Trim();
            settings.ExcludedExt = tbExcludedExts.Text.Trim();
            settings.Save();
            Close();
        }

        private void btTest_Click(object sender, EventArgs e)
        {
            string connectionString = tbBlobUrl.Text.Trim();
            if (connectionString.Length > 0)
            {
                if (ReverseImageSearch.Test(connectionString))
                    MessageBox.Show(this, "Connection string is correct.", "Connection test", MessageBoxButtons.OK, MessageBoxIcon.Information);
                else
                    MessageBox.Show(this, "Connection string is incorrect.", "Connection test", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }
    }
}
