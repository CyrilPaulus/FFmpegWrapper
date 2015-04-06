using FFmpeg.Wrapper;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace VideoPlayer
{
    public partial class VideoPlayer : Form
    {
        public VideoPlayer()
        {
            InitializeComponent();
        }

        private void _btBrowse_Click(object sender, EventArgs e)
        {
            var d = _ofFile.ShowDialog();

            if (d == DialogResult.OK)
                _tbBrowse.Text = _ofFile.FileName;
        }

        private void _btPlay_Click(object sender, EventArgs e)
        {
            var wrapper = new VideoWrapper();
            wrapper.Open(_tbBrowse.Text);
            _videoPlayer.SetWrapper(wrapper);
        }
    }
}
