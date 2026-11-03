using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.IO.Compression;
using System.Reflection;
using System.Threading;
using System.Windows.Forms;
using Microsoft.Win32;

namespace SaeFeeder
{
    public class SetupForm : Form
    {
        private Panel headerPanel;
        private Label lblTitle;
        private Label lblSubtitle;
        private PictureBox picLogo;

        private Label lblIntro;
        private GroupBox grpPath;
        private TextBox txtInstallPath;
        private Button btnBrowse;

        private GroupBox grpOptions;
        private CheckBox chkDesktop;
        private CheckBox chkStartMenu;
        private CheckBox chkRunAfter;

        private ProgressBar progressBar;
        private Label lblStatus;

        private Panel footerPanel;
        private Button btnCancel;
        private Button btnInstall;

        [STAThread]
        public static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new SetupForm());
        }

        public SetupForm()
        {
            InitializeComponents();
        }

        private void InitializeComponents()
        {
            this.Text = "SAE Feeder Setup";
            this.Size = new Size(540, 470);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.BackColor = Color.FromArgb(248, 249, 250);
            this.Font = new Font("Segoe UI", 9F, FontStyle.Regular);

            // Try load icon
            try
            {
                using (Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("sae-feeder.ico"))
                {
                    if (stream != null) this.Icon = new Icon(stream);
                }
            }
            catch { }

            // Header Panel
            headerPanel = new Panel();
            headerPanel.Dock = DockStyle.Top;
            headerPanel.Height = 85;
            headerPanel.BackColor = Color.FromArgb(15, 23, 42); // Slate dark 900
            headerPanel.Padding = new Padding(20, 12, 20, 12);

            lblTitle = new Label();
            lblTitle.Text = "SAE Feeder — Setup Wizard";
            lblTitle.Font = new Font("Segoe UI", 13F, FontStyle.Bold);
            lblTitle.ForeColor = Color.White;
            lblTitle.Location = new Point(20, 14);
            lblTitle.AutoSize = true;

            lblSubtitle = new Label();
            lblSubtitle.Text = "Sinkronisasi Cepat & Aman Data Dapodik ke SAE";
            lblSubtitle.Font = new Font("Segoe UI", 8.5F, FontStyle.Regular);
            lblSubtitle.ForeColor = Color.FromArgb(148, 163, 184); // Slate 400
            lblSubtitle.Location = new Point(22, 42);
            lblSubtitle.AutoSize = true;

            picLogo = new PictureBox();
            picLogo.Size = new Size(55, 55);
            picLogo.Location = new Point(450, 14);
            picLogo.SizeMode = PictureBoxSizeMode.Zoom;
            try
            {
                using (Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("sae-logo.png"))
                {
                    if (stream != null) picLogo.Image = Image.FromStream(stream);
                }
            }
            catch { }

            headerPanel.Controls.Add(lblTitle);
            headerPanel.Controls.Add(lblSubtitle);
            headerPanel.Controls.Add(picLogo);
            this.Controls.Add(headerPanel);

            // Intro text
            lblIntro = new Label();
            lblIntro.Text = "Wizard ini akan memandu Anda untuk menginstal SAE Feeder pada komputer Anda. Pastikan Dapodik sudah terpasang jika ingin melakukan penarikan data lokal.";
            lblIntro.Location = new Point(22, 100);
            lblIntro.Size = new Size(480, 36);
            lblIntro.ForeColor = Color.FromArgb(51, 65, 85);
            this.Controls.Add(lblIntro);

            // Path Group
            grpPath = new GroupBox();
            grpPath.Text = "Direktori Instalasi";
            grpPath.Location = new Point(22, 145);
            grpPath.Size = new Size(480, 75);
            grpPath.ForeColor = Color.FromArgb(30, 41, 59);

            txtInstallPath = new TextBox();
            string defaultDir = @"C:\SAE-Feeder";
            txtInstallPath.Text = defaultDir;
            txtInstallPath.Location = new Point(15, 28);
            txtInstallPath.Size = new Size(350, 25);
            grpPath.Controls.Add(txtInstallPath);

            btnBrowse = new Button();
            btnBrowse.Text = "Telusuri...";
            btnBrowse.Location = new Point(375, 26);
            btnBrowse.Size = new Size(90, 27);
            btnBrowse.Click += (s, e) =>
            {
                using (FolderBrowserDialog fbd = new FolderBrowserDialog())
                {
                    fbd.Description = "Pilih direktori instalasi SAE Feeder:";
                    fbd.SelectedPath = txtInstallPath.Text;
                    if (fbd.ShowDialog() == DialogResult.OK)
                    {
                        txtInstallPath.Text = Path.Combine(fbd.SelectedPath, "SAE-Feeder");
                    }
                }
            };
            grpPath.Controls.Add(btnBrowse);
            this.Controls.Add(grpPath);

            // Options Group
            grpOptions = new GroupBox();
            grpOptions.Text = "Opsi Tambahan";
            grpOptions.Location = new Point(22, 230);
            grpOptions.Size = new Size(480, 95);
            grpOptions.ForeColor = Color.FromArgb(30, 41, 59);

            chkDesktop = new CheckBox();
            chkDesktop.Text = "Buat Shortcut di Desktop";
            chkDesktop.Checked = true;
            chkDesktop.Location = new Point(15, 22);
            chkDesktop.AutoSize = true;
            grpOptions.Controls.Add(chkDesktop);

            chkStartMenu = new CheckBox();
            chkStartMenu.Text = "Buat Shortcut di Menu Start";
            chkStartMenu.Checked = true;
            chkStartMenu.Location = new Point(15, 45);
            chkStartMenu.AutoSize = true;
            grpOptions.Controls.Add(chkStartMenu);

            chkRunAfter = new CheckBox();
            chkRunAfter.Text = "Jalankan SAE Feeder setelah selesai instalasi";
            chkRunAfter.Checked = true;
            chkRunAfter.Location = new Point(15, 68);
            chkRunAfter.AutoSize = true;
            grpOptions.Controls.Add(chkRunAfter);

            this.Controls.Add(grpOptions);

            // Progress Bar & Status
            progressBar = new ProgressBar();
            progressBar.Location = new Point(22, 335);
            progressBar.Size = new Size(480, 18);
            progressBar.Visible = false;
            this.Controls.Add(progressBar);

            lblStatus = new Label();
            lblStatus.Text = "Siap untuk menginstal.";
            lblStatus.Location = new Point(22, 357);
            lblStatus.Size = new Size(480, 20);
            lblStatus.ForeColor = Color.FromArgb(100, 116, 139);
            this.Controls.Add(lblStatus);

            // Footer Panel
            footerPanel = new Panel();
            footerPanel.Dock = DockStyle.Bottom;
            footerPanel.Height = 55;
            footerPanel.BackColor = Color.FromArgb(241, 245, 249);
            footerPanel.Padding = new Padding(20, 10, 20, 10);

            btnCancel = new Button();
            btnCancel.Text = "Batal";
            btnCancel.Location = new Point(310, 12);
            btnCancel.Size = new Size(85, 32);
            btnCancel.Click += (s, e) => this.Close();

            btnInstall = new Button();
            btnInstall.Text = "Instal Sekarang";
            btnInstall.Location = new Point(405, 12);
            btnInstall.Size = new Size(110, 32);
            btnInstall.BackColor = Color.FromArgb(37, 99, 235); // Blue 600
            btnInstall.ForeColor = Color.White;
            btnInstall.FlatStyle = FlatStyle.Flat;
            btnInstall.FlatAppearance.BorderSize = 0;
            btnInstall.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            btnInstall.Click += BtnInstall_Click;

            footerPanel.Controls.Add(btnCancel);
            footerPanel.Controls.Add(btnInstall);
            this.Controls.Add(footerPanel);
        }

        private void BtnInstall_Click(object sender, EventArgs e)
        {
            string targetDir = txtInstallPath.Text.Trim();
            if (string.IsNullOrEmpty(targetDir))
            {
                MessageBox.Show("Silakan tentukan direktori instalasi!", "Validasi", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            btnInstall.Enabled = false;
            btnBrowse.Enabled = false;
            txtInstallPath.Enabled = false;
            btnCancel.Enabled = false;
            progressBar.Visible = true;
            progressBar.Value = 0;

            bool makeDesktop = chkDesktop.Checked;
            bool makeStartMenu = chkStartMenu.Checked;
            bool runAfter = chkRunAfter.Checked;

            Thread worker = new Thread(() =>
            {
                try
                {
                    PerformInstallation(targetDir, makeDesktop, makeStartMenu, runAfter);
                }
                catch (Exception ex)
                {
                    this.Invoke((MethodInvoker)delegate
                    {
                        MessageBox.Show("Terjadi kesalahan saat instalasi: " + ex.Message, "Instalasi Gagal", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        btnInstall.Enabled = true;
                        btnCancel.Enabled = true;
                    });
                }
            });
            worker.IsBackground = true;
            worker.Start();
        }

        private static void StopRunningApp(string targetDir)
        {
            // Hentikan launcher + server PHP lama di folder ini agar berkas tidak terkunci
            // dan runtime PHP baru langsung dipakai (tanpa restart manual oleh pengguna).
            foreach (string name in new[] { "SAE-Feeder", "php", "php-win" })
            {
                foreach (Process p in Process.GetProcessesByName(name))
                {
                    try
                    {
                        string exePath = p.MainModule.FileName;
                        if (exePath.StartsWith(targetDir, StringComparison.OrdinalIgnoreCase))
                        {
                            p.Kill();
                            p.WaitForExit(3000);
                        }
                    }
                    catch { }
                }
            }
            Thread.Sleep(500);
        }

        private void PerformInstallation(string targetDir, bool makeDesktop, bool makeStartMenu, bool runAfter)
        {
            UpdateStatus("Menutup SAE Feeder yang sedang berjalan...", 3);
            StopRunningApp(targetDir);

            UpdateStatus("Mempersiapkan direktori tujuan...", 5);
            if (!Directory.Exists(targetDir))
            {
                Directory.CreateDirectory(targetDir);
            }

            UpdateStatus("Mengekstrak berkas aplikasi dan runtime PHP...", 15);

            // Extract payload.zip from embedded resource
            using (Stream resourceStream = Assembly.GetExecutingAssembly().GetManifestResourceStream("payload.zip"))
            {
                if (resourceStream == null)
                {
                    throw new Exception("Sumber daya paket instalasi (payload.zip) tidak ditemukan!");
                }

                using (ZipArchive archive = new ZipArchive(resourceStream, ZipArchiveMode.Read))
                {
                    int totalEntries = archive.Entries.Count;
                    int currentEntry = 0;

                    foreach (ZipArchiveEntry entry in archive.Entries)
                    {
                        currentEntry++;
                        string destPath = Path.Combine(targetDir, entry.FullName.Replace('/', '\\'));

                        if (entry.FullName.EndsWith("/") || entry.FullName.EndsWith("\\"))
                        {
                            Directory.CreateDirectory(destPath);
                            continue;
                        }

                        string parentDir = Path.GetDirectoryName(destPath);
                        if (!Directory.Exists(parentDir))
                        {
                            Directory.CreateDirectory(parentDir);
                        }

                        // Protect existing dynamic_config.json
                        if (Path.GetFileName(destPath).Equals("dynamic_config.json", StringComparison.OrdinalIgnoreCase) && File.Exists(destPath))
                        {
                            // Preserve existing configuration
                            continue;
                        }

                        entry.ExtractToFile(destPath, true);

                        int pct = 15 + (int)(((float)currentEntry / totalEntries) * 65);
                        UpdateStatus(string.Format("Mengekstrak ({0}/{1}): {2}", currentEntry, totalEntries, Path.GetFileName(destPath)), pct);
                    }
                }
            }

            UpdateStatus("Membuat shortcut aplikasi...", 85);
            string launcherPath = Path.Combine(targetDir, "SAE-Feeder.exe");
            string iconPath = Path.Combine(targetDir, "sae-feeder.ico");

            if (makeDesktop)
            {
                string desktopLnk = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory), "SAE Feeder.lnk");
                CreateShortcut(desktopLnk, launcherPath, targetDir, iconPath, "SAE Feeder - Sinkronisasi Dapodik");
            }

            if (makeStartMenu)
            {
                string startMenuDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.StartMenu), "Programs");
                string startMenuLnk = Path.Combine(startMenuDir, "SAE Feeder.lnk");
                CreateShortcut(startMenuLnk, launcherPath, targetDir, iconPath, "SAE Feeder - Sinkronisasi Dapodik");
            }

            UpdateStatus("Mendaftarkan aplikasi di Windows...", 95);
            RegisterUninstaller(targetDir);

            UpdateStatus("Instalasi selesai dengan sukses!", 100);

            this.Invoke((MethodInvoker)delegate
            {
                MessageBox.Show(
                    "SAE Feeder telah berhasil diinstal pada komputer Anda!\n\nLokasi: " + targetDir,
                    "Instalasi Selesai",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information
                );

                if (runAfter)
                {
                    try
                    {
                        Process.Start(launcherPath);
                    }
                    catch { }
                }

                this.Close();
            });
        }

        private void UpdateStatus(string message, int progress)
        {
            if (this.IsHandleCreated)
            {
                this.Invoke((MethodInvoker)delegate
                {
                    lblStatus.Text = message;
                    if (progress >= 0 && progress <= 100) progressBar.Value = progress;
                });
            }
        }

        private void CreateShortcut(string shortcutPath, string targetPath, string workingDir, string iconPath, string description)
        {
            try
            {
                Type shellType = Type.GetTypeFromProgID("WScript.Shell");
                if (shellType != null)
                {
                    dynamic shell = Activator.CreateInstance(shellType);
                    dynamic shortcut = shell.CreateShortcut(shortcutPath);
                    shortcut.TargetPath = targetPath;
                    shortcut.WorkingDirectory = workingDir;
                    if (File.Exists(iconPath)) shortcut.IconLocation = iconPath + ",0";
                    shortcut.Description = description;
                    shortcut.Save();
                }
            }
            catch { }
        }

        private void RegisterUninstaller(string installDir)
        {
            try
            {
                string uninstallerPath = Path.Combine(installDir, "Uninstall.exe");
                string iconPath = Path.Combine(installDir, "sae-feeder.ico");

                using (RegistryKey key = Registry.CurrentUser.CreateSubKey(@"Software\Microsoft\Windows\CurrentVersion\Uninstall\SAE-Feeder"))
                {
                    if (key != null)
                    {
                        key.SetValue("DisplayName", "SAE Feeder");
                        key.SetValue("DisplayVersion", "1.0.0");
                        key.SetValue("Publisher", "SAE Education");
                        key.SetValue("InstallLocation", installDir);
                        key.SetValue("UninstallString", "\"" + uninstallerPath + "\"");
                        key.SetValue("DisplayIcon", iconPath);
                        key.SetValue("HelpLink", "https://sae.smakpal.sch.id");
                        key.SetValue("URLInfoAbout", "https://sae.smakpal.sch.id");
                        key.SetValue("NoModify", 1, RegistryValueKind.DWord);
                        key.SetValue("NoRepair", 1, RegistryValueKind.DWord);
                    }
                }
            }
            catch { }
        }
    }
}
