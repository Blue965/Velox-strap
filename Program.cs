using System;
using System.Windows.Forms;

namespace VeloxStrap
{
    internal static class Program
    {
        [STAThread]
        static void Main()
        {
            try
            {
                MessageBox.Show("VeloxStrap is starting...", "Debug", MessageBoxButtons.OK, MessageBoxIcon.Information);
                ApplicationConfiguration.Initialize();
                Application.Run(new MainForm());
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur au démarrage: {ex.Message}\n\n{ex.StackTrace}", "VeloxStrap - Erreur", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}
