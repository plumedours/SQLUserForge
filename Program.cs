using System;
using System.Diagnostics;
using System.Security.Principal;
using System.Windows.Forms;
using SQLUserForge.Forms;
using SQLUserForge.Services;

namespace SQLUserForge
{
    internal static class Program
    {
        [STAThread]
        static void Main()
        {
            // Auto-élévation admin (tu l’avais déjà)
            bool isAdmin = new WindowsPrincipal(WindowsIdentity.GetCurrent())
                           .IsInRole(WindowsBuiltInRole.Administrator);
            if (!isAdmin)
            {
                try
                {
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = Application.ExecutablePath,
                        UseShellExecute = true,
                        Verb = "runas"
                    });
                }
                catch { /* UAC refusé */ }
                return;
            }

            // Initialisation UI et traductions
            ApplicationConfiguration.Initialize();
            TranslationProvider.Initialize();   // charge config.json + lang courant

            Application.Run(new MainForm());
        }
    }
}
