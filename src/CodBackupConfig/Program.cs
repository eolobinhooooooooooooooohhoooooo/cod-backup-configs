using System;
using System.Drawing.Text;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;

namespace CodBackupConfig
{
    internal static class Program
    {
        [STAThread]
        static void Main()
        {
            LoadHitmarkerFont();
            Application.SetHighDpiMode(HighDpiMode.PerMonitorV2);
            Application.EnableVisualStyles();
            try
            {
                Application.Run(new MainForm());
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Cod Backup Configs",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // pinned bytes + the collection must stay alive for the whole run,
        // gdi+ drops lazily-registered families otherwise
        static GCHandle _fontPin;
        static PrivateFontCollection _fontCol;

        // register the embedded hitmarker face from memory only; on any
        // failure the ui silently falls back to consolas
        static void LoadHitmarkerFont()
        {
            try
            {
                using Stream s = Assembly.GetExecutingAssembly()
                    .GetManifestResourceStream("font/HitmarkerCondensed-Bold.ttf");
                if (s == null) return;
                byte[] bytes = new byte[s.Length];
                s.ReadExactly(bytes);

                _fontPin = GCHandle.Alloc(bytes, GCHandleType.Pinned);
                IntPtr ptr = _fontPin.AddrOfPinnedObject();
                uint len = (uint)bytes.Length;

                _fontCol = new PrivateFontCollection();
                _fontCol.AddMemoryFont(ptr, (int)len);
                uint count = 1;
                AddFontMemResourceEx(ptr, len, IntPtr.Zero, ref count);
            }
            catch { }
        }

        [DllImport("gdi32.dll", SetLastError = true)]
        static extern int AddFontMemResourceEx(IntPtr data, uint len, IntPtr faces, ref uint numFaces);
    }
}
