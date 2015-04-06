using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using FFmpeg.Wrapper;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

namespace VideoPlayer
{
    public partial class VideoPlayerControl : UserControl
    {
        private byte[] _frame;
        private VideoWrapper _wrapper;
        Timer _timer;
        private DateTime _last;
        Bitmap _image;


        public void SetWrapper(VideoWrapper wrapper)
        {
            _wrapper = wrapper;
            if (_wrapper != null)
            {
                _timer = new Timer();
                _timer.Interval = 40;
                _timer.Tick += new EventHandler(timer_Tick);
                _timer.Start();
                _frame = new byte[_wrapper.Width * _wrapper.Height * 3];
               _image = new Bitmap(_wrapper.Width, _wrapper.Height);
               trackBar1.Minimum = 0;
               trackBar1.Maximum = (int)(wrapper.Duration * 1000);
            }
            else
            {
                if (_timer != null)
                    _timer.Stop();
                _timer = null;
            }
            _last = DateTime.Now;
        }

        public VideoPlayerControl()
        {
            InitializeComponent();
            DoubleBuffered = true;
        }

        void timer_Tick(object sender, EventArgs e)
        {            
            if (_wrapper != null)
                Invalidate();
        }

        protected override void OnPaint(PaintEventArgs pe)
        {
            base.OnPaint(pe);

           

            if (_wrapper != null)
            {
                var cur = DateTime.Now;
                double diff = (cur - _last).TotalSeconds;
                trackBar1.Value = (int)(diff * 1000);

                _wrapper.ReadFrame(diff, _frame);
                

                BitmapData data = _image.LockBits(new Rectangle(0, 0, _image.Width, _image.Height),
                        ImageLockMode.WriteOnly, PixelFormat.Format24bppRgb);

                Marshal.Copy(_frame, 0, data.Scan0, _frame.Length);

                _image.UnlockBits(data);

                pe.Graphics.DrawImage(_image, ClientRectangle);
                

            }
        }

        private void trackBar1_Scroll(object sender, EventArgs e)
        {

        }
    }
}
