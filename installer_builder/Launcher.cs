using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Windows.Forms;

namespace SaeFeeder
{
    public class LauncherApplication : ApplicationContext
    {
        private const int PORT = 4215;
        private const string MUTEX_ID = "Global\\SAE_Feeder_Launcher_Mutex_v1";
        private static Mutex appMutex;

        private NotifyIcon trayIcon;
        private ContextMenuStrip trayMenu;
        private Process serverProcess;
        private string appDir;
        private string phpExe;
        private string logsDir;

        [STAThread]
        public static void Main(string[] args)
        {
            bool createdNew;
            appMutex = new Mutex(true, MUTEX_ID, out createdNew);

            if (!createdNew)
            {
                // Instance already active, just open browser and exit
                try
                {
                    Process.Start("http://localhost:" + PORT);
                }
                catch { }
                return;
            }

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new LauncherApplication());
        }

        public LauncherApplication()
        {
            appDir = AppDomain.CurrentDomain.BaseDirectory.TrimEnd('\\');
            logsDir = Path.Combine(appDir, "logs");
            if (!Directory.Exists(logsDir))
            {
                try { Directory.CreateDirectory(logsDir); } catch { }
            }

            LocatePhp();
            CleanupStaleProcesses();
            InitializeTray();
            StartServer();
            OpenBrowser();
        }

        private void LocatePhp()
        {
            // 1. Check embedded php-win.exe
            string localPhpWin = Path.Combine(appDir, @"php\php-win.exe");
            if (File.Exists(localPhpWin))
            {
                phpExe = localPhpWin;
                return;
            }

            // 2. Check embedded php.exe
            string localPhp = Path.Combine(appDir, @"php\php.exe");
            if (File.Exists(localPhp))
            {
                phpExe = localPhp;
                return;
            }

            // 3. Fallback to system php
            phpExe = "php";
        }

        private void CleanupStaleProcesses()
        {
            try
            {
                Process currentProc = Process.GetCurrentProcess();
                Process[] procs = Process.GetProcesses();
                foreach (Process p in procs)
                {
                    try
                    {
                        if (p.Id == currentProc.Id) continue;

                        if (p.ProcessName.Equals("php-win", StringComparison.OrdinalIgnoreCase) ||
                            p.ProcessName.Equals("php", StringComparison.OrdinalIgnoreCase))
                        {
                            string procPath = p.MainModule.FileName;
                            if (procPath.StartsWith(appDir, StringComparison.OrdinalIgnoreCase))
                            {
                                p.Kill();
                                p.WaitForExit(500);
                            }
                        }
                    }
                    catch { }
                }
            }
            catch { }
        }

        private void InitializeTray()
        {
            trayMenu = new ContextMenuStrip();

            ToolStripMenuItem menuTitle = new ToolStripMenuItem("SAE Feeder v" + GetAppVersion());
            menuTitle.Font = new Font(trayMenu.Font, FontStyle.Bold);
            menuTitle.Enabled = false;
            trayMenu.Items.Add(menuTitle);
            trayMenu.Items.Add(new ToolStripSeparator());

            ToolStripMenuItem openBrowserItem = new ToolStripMenuItem("🌐 Buka di Browser (Localhost)", null, (s, e) => OpenBrowser());
            openBrowserItem.Font = new Font(trayMenu.Font, FontStyle.Bold);
            trayMenu.Items.Add(openBrowserItem);

            string localIp = GetLocalIPAddress();
            if (!string.IsNullOrEmpty(localIp) && localIp != "127.0.0.1")
            {
                ToolStripMenuItem openLanItem = new ToolStripMenuItem("🌐 Buka di Browser (" + localIp + ")", null, (s, e) =>
                {
                    try { Process.Start("http://" + localIp + ":" + PORT); } catch { }
                });
                trayMenu.Items.Add(openLanItem);
            }

            ToolStripMenuItem restartItem = new ToolStripMenuItem("🔄 Restart Server", null, (s, e) => RestartServer());
            trayMenu.Items.Add(restartItem);

            ToolStripMenuItem logItem = new ToolStripMenuItem("📋 Lihat Log Server", null, (s, e) => OpenLogFile());
            trayMenu.Items.Add(logItem);

            ToolStripMenuItem openFolderItem = new ToolStripMenuItem("📁 Buka Folder Aplikasi", null, (s, e) => OpenFolder());
            trayMenu.Items.Add(openFolderItem);

            trayMenu.Items.Add(new ToolStripSeparator());

            ToolStripMenuItem exitItem = new ToolStripMenuItem("❌ Keluar", null, (s, e) => ExitApplication());
            trayMenu.Items.Add(exitItem);

            trayIcon = new NotifyIcon();
            trayIcon.Text = "SAE Feeder (Port " + PORT + ")";
            trayIcon.ContextMenuStrip = trayMenu;

            // Load Icon
            string iconPath = Path.Combine(appDir, "sae-feeder.ico");
            if (File.Exists(iconPath))
            {
                try { trayIcon.Icon = new Icon(iconPath); }
                catch { trayIcon.Icon = SystemIcons.Application; }
            }
            else
            {
                trayIcon.Icon = SystemIcons.Application;
            }

            trayIcon.Visible = true;
            trayIcon.DoubleClick += (s, e) => OpenBrowser();
        }

        private string GetLocalIPAddress()
        {
            try
            {
                var host = Dns.GetHostEntry(Dns.GetHostName());
                foreach (var ip in host.AddressList)
                {
                    if (ip.AddressFamily == AddressFamily.InterNetwork && !IPAddress.IsLoopback(ip))
                    {
                        return ip.ToString();
                    }
                }
            }
            catch { }
            return "";
        }

        private string GetAppVersion()
        {
            try
            {
                string versionFile = Path.Combine(appDir, "VERSION");
                if (File.Exists(versionFile))
                {
                    string text = File.ReadAllText(versionFile).Trim();
                    if (!string.IsNullOrEmpty(text)) return text;
                }
            }
            catch { }
            return "1.0";
        }

        private bool IsPortInUse(int port)
        {
            try
            {
                using (TcpClient client = new TcpClient())
                {
                    IAsyncResult result = client.BeginConnect("127.0.0.1", port, null, null);
                    bool success = result.AsyncWaitHandle.WaitOne(400);
                    if (success && client.Connected)
                    {
                        client.EndConnect(result);
                        return true;
                    }
                }
            }
            catch { }
            return false;
        }

        private void StartServer()
        {
            if (IsPortInUse(PORT))
            {
                // Already running
                return;
            }

            string logPath = Path.Combine(logsDir, "server_output.log");

            try
            {
                ProcessStartInfo psi = new ProcessStartInfo();
                psi.FileName = phpExe;
                // Listen on 0.0.0.0 so both localhost and LAN IP are supported
                psi.Arguments = string.Format("-S 0.0.0.0:{0} -t \"{1}\" \"{1}\\index.php\"", PORT, appDir);
                psi.WorkingDirectory = appDir;
                psi.CreateNoWindow = true;
                psi.UseShellExecute = false;
                psi.WindowStyle = ProcessWindowStyle.Hidden;

                // Set environment variables for VC++ and PHP
                string phpDir = Path.GetDirectoryName(phpExe);
                if (Directory.Exists(phpDir))
                {
                    string currentPath = Environment.GetEnvironmentVariable("PATH") ?? "";
                    if (!currentPath.Contains(phpDir))
                    {
                        psi.EnvironmentVariables["PATH"] = phpDir + ";" + currentPath;
                    }
                }

                serverProcess = Process.Start(psi);

                // Wait up to 3 seconds for port to open
                bool portReady = false;
                for (int i = 0; i < 15; i++)
                {
                    if (serverProcess.HasExited)
                    {
                        break;
                    }
                    if (IsPortInUse(PORT))
                    {
                        portReady = true;
                        break;
                    }
                    Thread.Sleep(200);
                }

                if (!portReady && serverProcess.HasExited)
                {
                    // If php-win failed, try php.exe fallback
                    string fallbackPhp = Path.Combine(appDir, @"php\php.exe");
                    if (phpExe != fallbackPhp && File.Exists(fallbackPhp))
                    {
                        phpExe = fallbackPhp;
                        psi.FileName = fallbackPhp;
                        serverProcess = Process.Start(psi);
                        for (int i = 0; i < 15; i++)
                        {
                            if (IsPortInUse(PORT))
                            {
                                portReady = true;
                                break;
                            }
                            Thread.Sleep(200);
                        }
                    }
                }

                if (portReady)
                {
                    trayIcon.ShowBalloonTip(3000, "SAE Feeder Aktif", "Feeder berjalan di port " + PORT + ". Klik ikon tray untuk opsi.", ToolTipIcon.Info);
                }
                else
                {
                    MessageBox.Show(
                        "Peringatan: Server SAE Feeder tidak dapat mendengarkan di port " + PORT + ".\n" +
                        "Silakan periksa apakah port " + PORT + " digunakan oleh aplikasi lain atau periksa log.",
                        "SAE Feeder",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning
                    );
                }
            }
            catch (Exception ex)
            {
                File.AppendAllText(logPath, "[" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "] ERROR: " + ex.ToString() + Environment.NewLine);
                MessageBox.Show("Gagal memulai server PHP Feeder: " + ex.Message, "SAE Feeder Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void StopServer()
        {
            if (serverProcess != null && !serverProcess.HasExited)
            {
                try
                {
                    serverProcess.Kill();
                    serverProcess.WaitForExit(1000);
                }
                catch { }
                serverProcess = null;
            }
            CleanupStaleProcesses();
        }

        private void RestartServer()
        {
            StopServer();
            Thread.Sleep(500);
            StartServer();
            trayIcon.ShowBalloonTip(2000, "SAE Feeder", "Server berhasil di-restart di port " + PORT, ToolTipIcon.Info);
        }

        private void OpenBrowser()
        {
            try
            {
                Process.Start("http://localhost:" + PORT);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Gagal membuka browser: " + ex.Message, "SAE Feeder", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void OpenLogFile()
        {
            try
            {
                string logPath = Path.Combine(logsDir, "server_output.log");
                if (!File.Exists(logPath))
                {
                    File.WriteAllText(logPath, "Log Server SAE Feeder - Tidak ada error tercatat." + Environment.NewLine);
                }
                Process.Start("notepad.exe", logPath);
            }
            catch { }
        }

        private void OpenFolder()
        {
            try
            {
                Process.Start("explorer.exe", appDir);
            }
            catch { }
        }

        private void ExitApplication()
        {
            StopServer();
            if (trayIcon != null)
            {
                trayIcon.Visible = false;
                trayIcon.Dispose();
            }
            if (appMutex != null)
            {
                appMutex.ReleaseMutex();
                appMutex.Dispose();
            }
            Application.Exit();
        }
    }
}
