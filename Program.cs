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
