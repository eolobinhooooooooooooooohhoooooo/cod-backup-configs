using System.Drawing;

namespace CodBackupConfig
{
    // palette lifted from rehab.ac (their css variables, verbatim)
    static class Ui
    {
        public static readonly Color Bg        = Color.FromArgb(10, 11, 12);
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

        // Geist Mono is a web font; Consolas is the closest thing Windows ships
        static Font Mono(float size, FontStyle style = FontStyle.Regular) =>
            new Font("Consolas", size, style, GraphicsUnit.Point);

        // hitmarker (activision) for display headers; falls back to consolas
        // when the memory font registration failed in Program.Main
        public static Font Head(float size)
        {
            try
            {
                var f = new Font("Hitmarker Condensed", size, FontStyle.Bold, GraphicsUnit.Point);
                if (f.Name.Contains("Hitmarker")) return f;
                f.Dispose();
            }
            catch { }
            return Mono(size, FontStyle.Bold);
        }

        public static readonly Font Title = Head(12.5f);
        public static readonly Font CardName = Mono(10f, FontStyle.Bold);
        public static readonly Font Label = Mono(8.5f, FontStyle.Bold);
        public static readonly Font Body  = Mono(9.5f);
        public static readonly Font Small = Mono(8.5f);
    }
}
