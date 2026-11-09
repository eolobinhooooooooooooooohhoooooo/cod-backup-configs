using System;
using System.Windows.Forms;

namespace CodBackupConfig
{
    internal static class Program
    {
        [STAThread]
        static void Main()
        {
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
    }
}
