using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Reflection;

namespace CodBackupConfig
{
    // embedded images (covers, platform logos, flags)
    static class Assets
    {
        public static Bitmap Img(string name)
        {
            Stream s = Assembly.GetExecutingAssembly().GetManifestResourceStream("img/" + name);
            if (s == null) return null;
            using (s)
            {
                try { return new Bitmap(s); }
                catch { return null; }
            }
        }
    }

    // aspect-fit drawing helper shared by cards, flags and list rows
    static class Img
    {
        public static Bitmap Scale(Bitmap src, int w, int h)
        {
            var bmp = new Bitmap(w, h);
            using var g = Graphics.FromImage(bmp);
            Prep(g);
            Fit(g, src, new Rectangle(0, 0, w, h));
            return bmp;
        }

        public static void Fit(Graphics g, Bitmap img, Rectangle r)
        {
            if (img == null)
            {
                g.FillRectangle(new SolidBrush(Ui.Bg), r);
                using var p = new Pen(Ui.Line);
                g.DrawRectangle(p, r.X, r.Y, r.Width, r.Height);
                return;
            }
            Prep(g);
            // contain: the image never overflows the rect (css object-fit)
            float ar = (float)img.Width / img.Height;
            float rr = (float)r.Width / r.Height;
            Rectangle fr;
            if (ar > rr) // image relatively wide -> bound by width
            {
                int w = r.Width;
                int h = Math.Max(1, (int)(w / ar));
                fr = new Rectangle(r.X + (r.Width - w) / 2, r.Y + (r.Height - h) / 2, w, h);
            }
            else         // image relatively tall -> bound by height
            {
                int h = r.Height;
                int w = Math.Max(1, (int)(h * ar));
                fr = new Rectangle(r.X + (r.Width - w) / 2, r.Y + (r.Height - h) / 2, w, h);
            }
            g.DrawImage(img, fr);
        }

        // contain-fit for multi-frame sources (the gif intro): draws the
        // given frame centered at its natural aspect
        public static void Fit(Graphics g, Image img, int frame, Rectangle r)
        {
            if (img == null) return;
            Prep(g);
            float ar = (float)img.Width / img.Height;
            float rr = (float)r.Width / r.Height;
            int w, h;
            if (ar > rr) { h = r.Height; w = Math.Max(1, (int)(h * ar)); }
            else { w = r.Width; h = Math.Max(1, (int)(w / ar)); }
            try
            {
                if (frame >= 0 && img.FrameDimensionsList.Length > 0)
                    img.SelectActiveFrame(new FrameDimension(img.FrameDimensionsList[0]), frame);
            }
            catch { }
            g.DrawImage(img, new Rectangle(r.X + (r.Width - w) / 2, r.Y + (r.Height - h) / 2, w, h));
        }

        static void Prep(Graphics g)
        {
            g.SmoothingMode = SmoothingMode.HighQuality;
            g.InterpolationMode = InterpolationMode.HighQualityBicubic;
            g.PixelOffsetMode = PixelOffsetMode.HighQuality;
        }
    }
}
