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
            tbExcludedExts.Text = settings.ExcludedExt;
            tbFfmpegPath.Text = settings.FfmpegPath;
        }

        private void btOk_Click(object sender, EventArgs e)
        {
            settings.AutoOpen = cbAutoOpen.Checked;
            settings.ExcludedExt = tbExcludedExts.Text.Trim();
            settings.FfmpegPath = tbFfmpegPath.Text.Trim();
            Ffmpeg.SetPath(settings.FfmpegPath);
            settings.Save();
            Close();
        }
    }
}
