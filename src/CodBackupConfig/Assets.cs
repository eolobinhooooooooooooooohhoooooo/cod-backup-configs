using System.Drawing;
using System.Drawing.Drawing2D;
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

    // aspect-fit drawing helper shared by cards and the platform legend
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
            float ar = (float)img.Width / img.Height;
            float rr = (float)r.Width / r.Height;
            Rectangle fr;
            if (ar > rr)
            {
                int h = r.Height;
                int w = (int)(h * ar);
                fr = new Rectangle(r.X + (r.Width - w) / 2, r.Y, w, h);
            }
            else
            {
                int w = r.Width;
                int h = (int)(w / ar);
                fr = new Rectangle(r.X, r.Y + (r.Height - h) / 2, w, h);
            }
            g.DrawImage(img, fr);
        }

        static void Prep(Graphics g)
        {
            g.SmoothingMode = SmoothingMode.HighQuality;
            g.InterpolationMode = InterpolationMode.HighQualityBicubic;
            g.PixelOffsetMode = PixelOffsetMode.HighQuality;
        }
    }
}
