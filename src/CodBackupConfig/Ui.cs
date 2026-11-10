using System;
using System.Drawing;

namespace CodBackupConfig
{
    // palette lifted from rehab.ac (their css variables, verbatim)
    static class Ui
    {
        // uniform manual dpi scaling: every hardcoded pixel in the ui goes
        // through X so a non-96 context scales geometry as a whole; fonts
        // stay in points and scale with the device context on their own
        public static float F = 1f;
        public static int X(int v) => (int)Math.Round(v * F);
        public static readonly Color Bg        = Color.FromArgb(10, 11, 12);
        public static readonly Color Header    = Color.FromArgb(13, 15, 18);
        public static readonly Color Panel     = Color.FromArgb(19, 20, 23);
        public static readonly Color PanelHi   = Color.FromArgb(27, 31, 37);
        public static readonly Color Line      = Color.FromArgb(33, 35, 40);
        public static readonly Color Text      = Color.FromArgb(233, 230, 223);
        public static readonly Color Dim       = Color.FromArgb(138, 141, 148);
        public static readonly Color Faint     = Color.FromArgb(75, 78, 86);
        public static readonly Color Accent    = Color.FromArgb(1, 89, 179);
        public static readonly Color AccentDim = Color.FromArgb(15, 36, 60);
        public static readonly Color SelRow    = Color.FromArgb(14, 25, 40);
        public static readonly Color Error     = Color.FromArgb(200, 16, 46);

        // consolas stays reserved for numeric player ids and dates
        public static Font Mono(float size, FontStyle style = FontStyle.Regular) =>
            new Font("Consolas", size, style, GraphicsUnit.Point);

        // hitmarker faces live in process memory via FontHost; a lookup
        // miss or a gdi+ substitution silently falls back to consolas
        static Font Face(string family, float size, FontStyle style)
        {
            try
            {
                var fam = FontHost.Get(family);
                if (fam != null)
                {
                    var f = new Font(fam, size, style, GraphicsUnit.Point);
                    if (f.Name.StartsWith("Hitmarker")) return f;
                    f.Dispose();
                }
            }
            catch { }
            return Mono(size, style);
        }

        // condensed cut: titles, section headers and buttons
        public static Font Head(float size) =>
            Face("Hitmarker Condensed", size, FontStyle.Bold);

        public static readonly Font Title    = Head(12.5f);
        public static readonly Font CardName = Face("Hitmarker Normal", 10f, FontStyle.Bold);
        public static readonly Font Btn      = Face("Hitmarker Condensed", 9f, FontStyle.Bold);
        public static readonly Font Label    = Face("Hitmarker Text", 8.5f, FontStyle.Regular);
        public static readonly Font Body     = Face("Hitmarker Text", 9.5f, FontStyle.Regular);
        public static readonly Font Small    = Face("Hitmarker Text", 8.5f, FontStyle.Regular);
    }
}
