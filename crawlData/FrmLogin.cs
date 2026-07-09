using Microsoft.Win32;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Management;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace crawlData
{
    public partial class FrmLogin : Form
    {
        public FrmLogin()
        {
            InitializeComponent();
            ///
            //string aes = "N4GGHUqCnbUm0Cu6WXzPG3xag7+oDaZfydmuIm9S06TpG7FIkKGbogANfBLTeOrMes5u/YDsRyg18k+Y2h/bDvgJdoQ028RXcrn+4riP7TS605HLn514VCfXUTLtITLfSJA+J86l6OHAJH3EF2iN/Rn21eta8E0DmnG8pixtLs8=";
            //string data = "vYo6j+W0+m59rYlBvzAWr0NKj9Hsnkds4l9NXnuN/HgxHHqVY89khPvO/1YmV+qT2A4sC30vIxCeLfxf62vxz94QOAkfMhRAzrey83FIe8PsrMGdmk4eFMkNYCjucDX7Ll1870lVxlnOnZalMxtSuA04P8ClgQ==";
            //string path = "C:\\Users\\lam duc\\Downloads\\private_key.pem";
            //Decrypt(aes, data, path);
            ///

            txtKey.Text = GetUniqueWindowsID();
            DatabaseHelper.InitializeDatabase();
        }

        public static string Decrypt(string base64AesKey, string base64Data, string pemPath)
        {
            // ===============================
            // 1. LOAD RSA PRIVATE KEY (.pem)
            // ===============================
            string privateKeyPem = File.ReadAllText(pemPath);

            using RSA rsa = RSA.Create();
            rsa.ImportFromPem(privateKeyPem);

            // =====================================
            // 2. GIAI MA AES KEY BANG RSA
            // =====================================
            byte[] encryptedAesKey = Convert.FromBase64String(base64AesKey);

            byte[] aesKey = rsa.Decrypt(
                encryptedAesKey,
                RSAEncryptionPadding.OaepSHA256
            );

            // =====================================
            // 3. GIAI MA DATA BANG AES
            // =====================================
            byte[] encryptedData = Convert.FromBase64String(base64Data);

            // IV = 16 byte dau
            byte[] iv = encryptedData.Take(16).ToArray();
            byte[] cipherText = encryptedData.Skip(16).ToArray();

            using Aes aes = Aes.Create();
            aes.Key = aesKey;
            aes.IV = iv;
            aes.Mode = CipherMode.CBC;
            aes.Padding = PaddingMode.PKCS7;

            using ICryptoTransform decryptor = aes.CreateDecryptor();
            byte[] plainBytes = decryptor.TransformFinalBlock(cipherText, 0, cipherText.Length);

            return Encoding.UTF8.GetString(plainBytes);
        }

        public string GetUniqueWindowsID()
        {
            try
            {
                using (var baseKey = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64))
                {
                    using (var key = baseKey.OpenSubKey(@"SOFTWARE\Microsoft\Cryptography"))
                    {
                        if (key != null)
                        {
                            object guid = key.GetValue("MachineGuid");
                            if (guid != null)
                            {
                                return guid.ToString().ToUpper();
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                return "ERROR-CANNOT-GET-ID " + ex.ToString();
            }
            return "ID-NOT-FOUND";
        }

        private void btnCopy_Click(object sender, EventArgs e)
        {
            if (!string.IsNullOrEmpty(txtKey.Text))
            {
                Clipboard.SetText(txtKey.Text);
                MessageBox.Show("Da sao chep ma may vao bo nho tam!", "Thong bao", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private async void btnLogin_Click(object sender, EventArgs e)
        {
            await Login();
        }

        private async Task Login()
        {
            if (btnLogin.Enabled == false) return;

            btnLogin.Enabled = false;
            string pcKeyInput = txtKey.Text.Trim();

            if (string.IsNullOrEmpty(pcKeyInput))
            {
                MessageBox.Show("Vui long nhap Key!");
                btnLogin.Enabled = true;
                return;
            }

            try
            {
                bool isSuccess = await CheckLicenseKey(pcKeyInput);

                if (isSuccess)
                {
                    FrmMain mainForm = new FrmMain(pcKeyInput);
                    mainForm.Show();
                    this.Hide();
                }
                else
                {
                    MessageBox.Show("Key khong chinh xac hoac da het han!", "Loi kich hoat",
                                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                    btnLogin.Enabled = true;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Loi he thong: " + ex.Message);
                btnLogin.Enabled = true;
            }
        }

        private void btnClose_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        public async Task<bool> CheckLicenseKey(string pcKey)
        {
            string url = "https://n8n.thile.ai/webhook/94f5f2a2-c54f-4320-a446-1dac3ea2c904/Check";

            using (HttpClient client = new HttpClient())
            {
                try
                {
                    var payload = new { pckey = pcKey };
                    string jsonPayload = JsonConvert.SerializeObject(payload);
                    var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

                    var response = await client.PostAsync(url, content);

                    if (response.IsSuccessStatusCode)
                    {
                        string jsonResponse = await response.Content.ReadAsStringAsync();
                        var data = JsonConvert.DeserializeObject<List<LicenseResponse>>(jsonResponse);

                        if (data != null && data.Count > 0)
                        {
                            var info = data[0];
                            bool isStatusTrue = info.status.ToLower() == "true";
                            bool isNotExpired = info.expired > DateTime.Now;

                            if (isStatusTrue && isNotExpired)
                                return true;
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Loi Check Key: " + ex.Message);
                }
            }
            return false;
        }

        public class LicenseResponse
        {
            public string pckey { get; set; }
            public DateTime expired { get; set; }
            public string status { get; set; }
        }

        // =============================================
        // AUTO UPDATE
        // =============================================

        /// <summary>
        /// Phien ban hien tai cua ung dung. Cap nhat moi lan release.
        /// Dinh dang: Major.Minor.Patch  (vi du: "1.0.0")
        /// </summary>
        public const string CURRENT_VERSION = "2.6.9.3";

        /// <summary>
        /// URL tra ve JSON chua thong tin phien ban moi nhat.
        /// JSON mau: { "version": "1.1.0", "url": "https://..../Setup.exe", "note": "Cap nhat loi X" }
        /// </summary>
        private const string UPDATE_CHECK_URL = "https://n8n.thile.ai/webhook/2603239b-c26c-4888-8d8c-95ae776b5c10/crawldata";

        private async void btn_update_Click(object sender, EventArgs e)
        {
            btn_update.Enabled = false;
            btn_update.Text = "Checking...";

            try
            {
                UpdateInfo info = await FetchUpdateInfo();

                if (info == null)
                {
                    MessageBox.Show("Không thể kết nối máy chủ.\nVui lòng thử lại sau.",
                                    "Kiểm tra cập nhật", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                if (!IsNewerVersion(info.Version, CURRENT_VERSION))
                {
                    MessageBox.Show($"Bạn đang dùng phiên bản mới nhất ({CURRENT_VERSION}).",
                                    "Kiểm tra cập nhật", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                // Co ban moi -> hoi nguoi dung
                string message =
                    $"Có phiên bản mới: {info.Version}\n" +
                    $"Phiên bản hiện tại: {CURRENT_VERSION}\n\n" +
                    $"Ghi chú: {info.Note}\n\n" +
                    "Bạn có muốn cập nhật không?";

                var result = MessageBox.Show(message, "Cập nhật mới",
                                             MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                if (result != DialogResult.Yes) return;

                await DownloadAndInstall(info);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi khi kiểm tra cập nhật: " + ex.Message,
                                "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                btn_update.Enabled = true;
                btn_update.Text = "UPDATE";
            }
        }

        /// <summary>
        /// Lay thong tin phien ban moi nhat tu server.
        /// </summary>
        private async Task<UpdateInfo> FetchUpdateInfo()
        {
            using var client = new HttpClient();
            client.Timeout = TimeSpan.FromSeconds(15);

            try
            {
                var urlWithCacheBuster = UPDATE_CHECK_URL + "?v=" + DateTime.Now.Ticks;
                var response = await client.GetAsync(urlWithCacheBuster);
                if (!response.IsSuccessStatusCode) return null;

                string json = await response.Content.ReadAsStringAsync();
                return JsonConvert.DeserializeObject<UpdateInfo>(json);
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Tai file cai dat va chay updater tu dong.
        /// </summary>
        private async Task DownloadAndInstall(UpdateInfo info)
        {
            string tempDir = Path.GetTempPath();
            string setupPath = Path.Combine(tempDir, "W5GExtractor_Update.exe");
            string batchPath = Path.Combine(tempDir, "w5g_updater.bat");

            // --- Kill cac tien trinh khac cung duong dan truoc khi download de tranh lock file ---
            string exePath = Application.ExecutablePath;
            int currentPid = System.Diagnostics.Process.GetCurrentProcess().Id;
            
            try
            {
                var processes = System.Diagnostics.Process.GetProcessesByName(Path.GetFileNameWithoutExtension(exePath));
                foreach (var p in processes)
                {
                    if (p.Id != currentPid)
                    {
                        try
                        {
                            if (p.MainModule?.FileName.Equals(exePath, StringComparison.OrdinalIgnoreCase) == true)
                            {
                                p.Kill();
                                p.WaitForExit(3000); // Chờ tối đa 3s
                            }
                        }
                        catch { /* Bo qua loi neu khong the truy cap module hoac da thoat */ }
                    }
                }
            }
            catch { /* Bo qua loi truy xuat tien trinh */ }

            // --- Tai file setup ---
            using (var client = new HttpClient())
            {
                client.Timeout = TimeSpan.FromMinutes(10);

                using var progress = new ProgressForm("Dang tai ban cap nhat...");
                progress.Show(this);
                Application.DoEvents();

                var urlWithCacheBuster = info.Url;
                if (urlWithCacheBuster.Contains("?"))
                    urlWithCacheBuster += "&v=" + DateTime.Now.Ticks;
                else
                    urlWithCacheBuster += "?v=" + DateTime.Now.Ticks;

                var response = await client.GetAsync(urlWithCacheBuster, HttpCompletionOption.ResponseHeadersRead);
                response.EnsureSuccessStatusCode();

                long? totalBytes = response.Content.Headers.ContentLength;
                using var stream = await response.Content.ReadAsStreamAsync();
                
                // Khong ghi de truc tiep neu dang bi lock (mac du da kill o tren, de phong batch file chay truoc do chua xong)
                try
                {
                    if (File.Exists(setupPath)) File.Delete(setupPath);
                }
                catch { }

                using var fileStream = new FileStream(setupPath, FileMode.Create, FileAccess.Write, FileShare.None);

                byte[] buffer = new byte[81920];
                long downloaded = 0;
                int read;

                while ((read = await stream.ReadAsync(buffer, 0, buffer.Length)) > 0)
                {
                    await fileStream.WriteAsync(buffer, 0, read);
                    downloaded += read;
                    if (totalBytes.HasValue)
                    {
                        int percent = (int)(downloaded * 100 / totalBytes.Value);
                        progress.SetProgress(percent);
                        Application.DoEvents();
                    }
                }

                progress.Close();
            }

            // --- Tao batch script: cho app dong roi chay setup hoac copy de ---
            string exeName = Path.GetFileName(exePath);

            bool isInstaller = info.Url.IndexOf("setup", StringComparison.OrdinalIgnoreCase) >= 0 ||
                               info.Url.IndexOf("install", StringComparison.OrdinalIgnoreCase) >= 0;

            string batchContent;

            if (isInstaller)
            {
                batchContent = string.Join("\r\n", new[]
                {
                    "@echo off",
                    ":wait",
                    $"tasklist /FI \"PID eq {currentPid}\" 2>NUL | find \"{currentPid}\" >NUL",
                    "if not errorlevel 1 (",
                    "    timeout /t 1 /nobreak >NUL",
                    "    goto wait",
                    ")",
                    $"taskkill /F /IM \"{exeName}\" >NUL 2>&1",
                    $"start \"\" \"{setupPath}\"",
                    "del \"%~f0\""
                });
            }
            else
            {
                batchContent = string.Join("\r\n", new[]
                {
                    "@echo off",
                    ":wait",
                    $"tasklist /FI \"PID eq {currentPid}\" 2>NUL | find \"{currentPid}\" >NUL",
                    "if not errorlevel 1 (",
                    "    timeout /t 1 /nobreak >NUL",
                    "    goto wait",
                    ")",
                    $"taskkill /F /IM \"{exeName}\" >NUL 2>&1",
                    ":retrycopy",
                    $"copy /Y \"{setupPath}\" \"{exePath}\"",
                    "if errorlevel 1 (",
                    "    timeout /t 1 /nobreak >NUL",
                    "    goto retrycopy",
                    ")",
                    $"start \"\" \"{exePath}\"",
                    "del \"%~f0\""
                });
            }

            File.WriteAllText(batchPath, batchContent, Encoding.ASCII);

            // Chay batch roi thoat app
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = "cmd.exe",
                Arguments = $"/c \"{batchPath}\"",
                WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden,
                CreateNoWindow = true
            });

            Application.Exit();
            Environment.Exit(0);
        }

        /// <summary>
        /// So sanh 2 chuoi version dang "Major.Minor.Patch".
        /// Tra ve true neu remoteVersion > localVersion.
        /// </summary>
        private bool IsNewerVersion(string remoteVersion, string localVersion)
        {
            try
            {
                var remote = new Version(remoteVersion);
                var local = new Version(localVersion);
                return remote > local;
            }
            catch
            {
                return false;
            }
        }

        public class UpdateInfo
        {
            [JsonProperty("version")]
            public string Version { get; set; }

            [JsonProperty("url")]
            public string Url { get; set; }

            [JsonProperty("note")]
            public string Note { get; set; }
        }

        // =============================================
        // PROGRESS FORM - hien thi % tai xuong
        // =============================================
        private class ProgressForm : Form
        {
            private Label lblStatus;
            private ProgressBar progressBar;

            public ProgressForm(string message)
            {
                Text = "Cap nhat";
                Width = 380;
                Height = 110;
                FormBorderStyle = FormBorderStyle.FixedDialog;
                StartPosition = FormStartPosition.CenterParent;
                MaximizeBox = false;
                MinimizeBox = false;
                ControlBox = false;

                lblStatus = new Label
                {
                    Text = message,
                    Dock = DockStyle.Top,
                    TextAlign = ContentAlignment.MiddleCenter,
                    Height = 30,
                    Font = new Font("Segoe UI", 10)
                };

                progressBar = new ProgressBar
                {
                    Dock = DockStyle.Bottom,
                    Height = 28,
                    Minimum = 0,
                    Maximum = 100,
                    Value = 0
                };

                Controls.Add(lblStatus);
                Controls.Add(progressBar);
            }

            public void SetProgress(int percent)
            {
                int safeVal = Math.Min(percent, 100);
                if (progressBar.InvokeRequired)
                {
                    progressBar.Invoke(new Action(() => progressBar.Value = safeVal));
                    lblStatus.Invoke(new Action(() => lblStatus.Text = $"Dang tai... {safeVal}%"));
                }
                else
                {
                    progressBar.Value = safeVal;
                    lblStatus.Text = $"Dang tai... {safeVal}%";
                }
            }
        }
    }
}
