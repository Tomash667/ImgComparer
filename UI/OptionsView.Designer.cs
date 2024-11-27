namespace ImgComparer.UI
{
    partial class OptionsView
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(OptionsView));
            this.cbAutoOpen = new System.Windows.Forms.CheckBox();
            this.btOk = new System.Windows.Forms.Button();
            this.tbExcludedExts = new System.Windows.Forms.TextBox();
            this.label1 = new System.Windows.Forms.Label();
            this.label3 = new System.Windows.Forms.Label();
            this.tbFfmpegPath = new System.Windows.Forms.TextBox();
            this.SuspendLayout();
            // 
            // cbAutoOpen
            // 
            this.cbAutoOpen.AutoSize = true;
            this.cbAutoOpen.Location = new System.Drawing.Point(12, 12);
            this.cbAutoOpen.Name = "cbAutoOpen";
            this.cbAutoOpen.Size = new System.Drawing.Size(129, 17);
            this.cbAutoOpen.TabIndex = 1;
            this.cbAutoOpen.Text = "Auto open last project";
            this.cbAutoOpen.UseVisualStyleBackColor = true;
            // 
            // btOk
            // 
            this.btOk.Location = new System.Drawing.Point(186, 119);
            this.btOk.Name = "btOk";
            this.btOk.Size = new System.Drawing.Size(75, 23);
            this.btOk.TabIndex = 6;
            this.btOk.Text = "Ok";
            this.btOk.UseVisualStyleBackColor = true;
            this.btOk.Click += new System.EventHandler(this.btOk_Click);
            // 
            // tbExcludedExts
            // 
            this.tbExcludedExts.Location = new System.Drawing.Point(12, 55);
            this.tbExcludedExts.Name = "tbExcludedExts";
            this.tbExcludedExts.Size = new System.Drawing.Size(249, 20);
            this.tbExcludedExts.TabIndex = 4;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(12, 39);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(200, 13);
            this.label1.TabIndex = 7;
            this.label1.Text = "Excluded extensions (comma separated):";
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(12, 77);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(69, 13);
            this.label3.TabIndex = 9;
            this.label3.Text = "Ffmpeg path:";
            // 
            // tbFfmpegPath
            // 
            this.tbFfmpegPath.Location = new System.Drawing.Point(12, 93);
            this.tbFfmpegPath.Name = "tbFfmpegPath";
            this.tbFfmpegPath.Size = new System.Drawing.Size(249, 20);
            this.tbFfmpegPath.TabIndex = 5;
            // 
            // OptionsView
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(273, 152);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.tbFfmpegPath);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.tbExcludedExts);
            this.Controls.Add(this.btOk);
            this.Controls.Add(this.cbAutoOpen);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "OptionsView";
            this.ShowInTaskbar = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Options";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion
        private System.Windows.Forms.CheckBox cbAutoOpen;
        private System.Windows.Forms.Button btOk;
        private System.Windows.Forms.TextBox tbExcludedExts;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.TextBox tbFfmpegPath;
    }
}