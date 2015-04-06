namespace VideoPlayer
{
    partial class VideoPlayer
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
            this._tbBrowse = new System.Windows.Forms.TextBox();
            this._btBrowse = new System.Windows.Forms.Button();
            this._btPlay = new System.Windows.Forms.Button();
            this._ofFile = new System.Windows.Forms.OpenFileDialog();
            this._videoPlayer = new VideoPlayerControl();
            this.SuspendLayout();
            // 
            // _tbBrowse
            // 
            this._tbBrowse.Location = new System.Drawing.Point(13, 13);
            this._tbBrowse.Name = "_tbBrowse";
            this._tbBrowse.Size = new System.Drawing.Size(206, 20);
            this._tbBrowse.TabIndex = 0;
            // 
            // _btBrowse
            // 
            this._btBrowse.Location = new System.Drawing.Point(225, 13);
            this._btBrowse.Name = "_btBrowse";
            this._btBrowse.Size = new System.Drawing.Size(46, 23);
            this._btBrowse.TabIndex = 1;
            this._btBrowse.Text = "...";
            this._btBrowse.UseVisualStyleBackColor = true;
            this._btBrowse.Click += new System.EventHandler(this._btBrowse_Click);
            // 
            // _btPlay
            // 
            this._btPlay.Location = new System.Drawing.Point(13, 40);
            this._btPlay.Name = "_btPlay";
            this._btPlay.Size = new System.Drawing.Size(75, 23);
            this._btPlay.TabIndex = 2;
            this._btPlay.Text = "Play";
            this._btPlay.UseVisualStyleBackColor = true;
            this._btPlay.Click += new System.EventHandler(this._btPlay_Click);
            // 
            // _videoPlayer
            // 
            this._videoPlayer.Location = new System.Drawing.Point(13, 70);
            this._videoPlayer.Name = "_videoPlayer";
            this._videoPlayer.Size = new System.Drawing.Size(1165, 476);
            this._videoPlayer.TabIndex = 3;
            // 
            // VideoPlayer
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1190, 558);
            this.Controls.Add(this._videoPlayer);
            this.Controls.Add(this._btPlay);
            this.Controls.Add(this._btBrowse);
            this.Controls.Add(this._tbBrowse);
            this.Name = "VideoPlayer";
            this.Text = "Form1";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.TextBox _tbBrowse;
        private System.Windows.Forms.Button _btBrowse;
        private System.Windows.Forms.Button _btPlay;
        private System.Windows.Forms.OpenFileDialog _ofFile;
        private VideoPlayerControl _videoPlayer;
    }
}

