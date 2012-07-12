using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using GraphicsMgrLib;


namespace Crop
{
    public class Crop2 : Form
    {
        public Image Screenshot { get; private set; }
        public AreaManager Manager { get; private set; }

        public Crop2(Image screenshot)
        {
            InitializeComponent();
            Screenshot = screenshot;
            Manager = new AreaManager(this);
            Timer drawTimer = new Timer();
            drawTimer.Interval = 10;
            drawTimer.Tick += new EventHandler(drawTimer_Tick);
            drawTimer.Start();
        }

        protected override void Dispose(bool disposing)
        {
            if (Screenshot != null) Screenshot.Dispose();

            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this.SuspendLayout();
            this.Bounds = GraphicsMgr.GetScreenBounds();
            this.CausesValidation = false;
            this.ControlBox = true;
            this.Cursor = Cursors.Cross;
            this.DoubleBuffered = true;
            this.FormBorderStyle = FormBorderStyle.None;
            this.KeyPreview = true;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "Crop";
            this.SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint, true);
            this.ShowIcon = false;
#if !DEBUG
            this.TopMost = true;
#endif
            this.ShowInTaskbar = false;
            this.ResumeLayout(false);

            this.KeyDown += new KeyEventHandler(Crop2_KeyDown);
        }

        private void Crop2_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Escape)
            {
                Close(false);
            }
            else if (e.KeyCode == Keys.Enter || e.KeyCode == Keys.Space)
            {
                Close(true);
            }
        }

        public void Close(bool result)
        {
            if (result)
            {
                saveImage();
                DialogResult = DialogResult.OK;
            }
            else
            {
                DialogResult = DialogResult.Abort;
            }

            this.Close();
        }

        private void drawTimer_Tick(object sender, EventArgs e)
        {
            this.Refresh();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.HighSpeed;
            DrawScreenshot(g);
            Manager.Draw(g);
        }

        public Image GetCroppedScreenshot()
        {
            using (Graphics g = Graphics.FromImage(Screenshot))
            {
                Region region = Manager.CombineAreas();

                if (!region.IsEmpty(g))
                {
                    RectangleF rect = region.GetBounds(g);
                    Bitmap bmp = new Bitmap((int)rect.Width, (int)rect.Height);

                    using (Graphics g2 = Graphics.FromImage(bmp))
                    {
                        g2.Clear(Color.Transparent);

                        using (Matrix translateMatrix = new Matrix())
                        {
                            translateMatrix.Translate(-rect.X, -rect.Y);
                            region.Transform(translateMatrix);
                        }

                        g2.IntersectClip(region);

                        g2.CompositingQuality = CompositingQuality.HighQuality;
                        g2.InterpolationMode = InterpolationMode.HighQualityBicubic;
                        g2.DrawImage(Screenshot, new Rectangle(0, 0, bmp.Width, bmp.Height), rect, GraphicsUnit.Pixel);

                        return bmp;
                    }
                }
            }

            return null;
        }

        public void saveImage()
        {
            Image bmp = GetCroppedScreenshot();
            String savePath = getNewFilename();
            bmp.Save(savePath, System.Drawing.Imaging.ImageFormat.Jpeg);
        }

        // With full path
        private String getNewFilename()
        {
            String basePath = "C:\\emacsd\\cygwin\\home\\ptmono\\.emacs.d\\imgs_xp";

            // Create new filename
            int lastest_num = getLastestNumber(basePath);
            int new_num = lastest_num + 1;
            String new_num_str = new_num.ToString();
            Console.WriteLine(new_num_str);
            String new_filename = basePath + "\\image" + new_num_str + ".jpg";

            return new_filename;
        }

        private int getLastestNumber(String dir)
        {
            var accuracy = 20;
            var start = 1;
            List<int> result_list = new List<int>();

            // Get lastest file
            var directory = new DirectoryInfo(dir);
            var files = directory.GetFiles()
                .OrderByDescending(f => f.LastWriteTime);

            // To increase speed we use some region
            // FIXME: conside more useful way
            foreach (var a in files)
            {
                if (start <= accuracy)
                {
                    var num = (int)getNum(a.Name);
                    if (num > 0)
                    {
                        result_list.Add(num);
                    }
                    start++;
                }
            }

            result_list.Sort();
            result_list.Reverse();
            return result_list[0];
        }

        private uint getNum(String file_name)
        {
            String regexp_num = "image([0-9]+).jpg";
            String filename = Path.GetFileName(file_name);
            Match match = Regex.Match(filename, regexp_num);

            if (match.Success)
            {
                String num_str = match.Groups[1].Value;
                uint result = Convert.ToUInt32(num_str);
                return result;
            }
            else
            {
                return 0;
            }
        }

        private void DrawScreenshot(Graphics g)
        {
            g.DrawImage(Screenshot, 0, 0, Screenshot.Width, Screenshot.Height);
        }
    }
}