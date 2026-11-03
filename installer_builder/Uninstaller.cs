using System;
using System.Diagnostics;
using System.IO;
using System.Windows.Forms;
using Microsoft.Win32;

namespace SaeFeeder
{
    public class Uninstaller
    {
        [STAThread]
        public static void Main(string[] args)
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            DialogResult confirm = MessageBox.Show(
                "Apakah Anda yakin ingin menghapus aplikasi SAE Feeder dari komputer Anda?",
                "Konfirmasi Uninstall SAE Feeder",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question
            );

            if (confirm != DialogResult.Yes)
            {
                return;
            }

            string appDir = AppDomain.CurrentDomain.BaseDirectory.TrimEnd('\\');

            // 1. Terminate running processes
            try
            {
                Process[] procs = Process.GetProcesses();
                foreach (Process p in procs)
                {
                    try
                    {
                        if (p.ProcessName.Equals("SAE-Feeder", StringComparison.OrdinalIgnoreCase) ||
                            p.ProcessName.Equals("php-win", StringComparison.OrdinalIgnoreCase) ||
                            p.ProcessName.Equals("php", StringComparison.OrdinalIgnoreCase))
                        {
                            string procPath = p.MainModule.FileName;
                            if (procPath.StartsWith(appDir, StringComparison.OrdinalIgnoreCase))
                            {
                                p.Kill();
                                p.WaitForExit(1000);
                            }
                        }
                    }
                    catch { }
                }
            }
            catch { }

            // 2. Remove Shortcuts
            try
            {
                string desktopShortcut = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory), "SAE Feeder.lnk");
                if (File.Exists(desktopShortcut)) File.Delete(desktopShortcut);

                string startMenuShortcut = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.StartMenu), "Programs\\SAE Feeder.lnk");
                if (File.Exists(startMenuShortcut)) File.Delete(startMenuShortcut);

                string commonDesktop = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonDesktopDirectory), "SAE Feeder.lnk");
                if (File.Exists(commonDesktop)) File.Delete(commonDesktop);

                string commonStartMenu = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonStartMenu), "Programs\\SAE Feeder.lnk");
                if (File.Exists(commonStartMenu)) File.Delete(commonStartMenu);
            }
            catch { }

            // 3. Remove Registry Uninstaller Key
            try
            {
                Registry.CurrentUser.DeleteSubKeyTree(@"Software\Microsoft\Windows\CurrentVersion\Uninstall\SAE-Feeder", false);
            }
            catch { }

            // 4. Self-delete folder using cmd in background
            try
            {
                ProcessStartInfo psi = new ProcessStartInfo();
                psi.FileName = "cmd.exe";
                psi.Arguments = string.Format("/c timeout /t 2 /nobreak > NUL & rmdir /s /q \"{0}\"", appDir);
                psi.WindowStyle = ProcessWindowStyle.Hidden;
                psi.CreateNoWindow = true;
                psi.UseShellExecute = false;
                Process.Start(psi);
            }
            catch { }

            MessageBox.Show(
                "SAE Feeder telah berhasil dihapus dari komputer Anda.",
                "Uninstall Selesai",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information
            );
        }
    }
}
