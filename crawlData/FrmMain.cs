using ClosedXML.Excel;
using ExcelDataReader;
using GenerativeAI;
using GenerativeAI.Types;
using Microsoft.Data.Sqlite;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.UI;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Runtime.InteropServices;

namespace crawlData
{
    public partial class FrmMain : Form
    {
        // Khai báo ở đầu Class Form
        private CancellationTokenSource _cts;
        DataTable mydata;
        private CheckBox chkCheckAll;
        private CheckBox chkCheckAllOutput;
        private bool _isUpdatingCheckAll = false;
        private bool _isRerunMode = false;
        private IWebDriver driver; // Legacy single-thread
        private readonly object _gridLock = new object();
        private ConcurrentBag<IWebDriver> _activeDrivers = new ConcurrentBag<IWebDriver>();
        private int _threadCount = 1;
        private int _numHang = 2;  // số hàng (dọc)
        private int _numCot = 5;   // số cột (ngang)

        // === Win32: Dark scrollbar (xám) ===
        [DllImport("uxtheme.dll", CharSet = CharSet.Unicode)]
        private static extern int SetWindowTheme(IntPtr hWnd, string pszSubAppName, string pszSubIdList);
        private void ApplyGrayScrollBar(Control ctrl)
        {
            SetWindowTheme(ctrl.Handle, "DarkMode_Explorer", null);
        }

        // Resize borderless form
        protected override void WndProc(ref Message m)
        {
            const int WM_NCHITTEST = 0x84;
            const int HTCLIENT = 1;
            const int HTLEFT = 10, HTRIGHT = 11, HTTOP = 12;
            const int HTTOPLEFT = 13, HTTOPRIGHT = 14;
            const int HTBOTTOM = 15, HTBOTTOMLEFT = 16, HTBOTTOMRIGHT = 17;

            if (m.Msg == WM_NCHITTEST)
            {
                base.WndProc(ref m);
                if ((int)m.Result == HTCLIENT)
                {
                    Point p = PointToClient(Cursor.Position);
                    int b = 6; // resize border width
                    if (p.X < b)
                        m.Result = (IntPtr)(p.Y < b ? HTTOPLEFT : p.Y > ClientSize.Height - b ? HTBOTTOMLEFT : HTLEFT);
                    else if (p.X > ClientSize.Width - b)
                        m.Result = (IntPtr)(p.Y < b ? HTTOPRIGHT : p.Y > ClientSize.Height - b ? HTBOTTOMRIGHT : HTRIGHT);
                    else if (p.Y < b)
                        m.Result = (IntPtr)HTTOP;
                    else if (p.Y > ClientSize.Height - b)
                        m.Result = (IntPtr)HTBOTTOM;
                }
                return;
            }
            base.WndProc(ref m);
        }
        public FrmMain(string pcKey)
        {
            InitializeComponent();
            lblFormname.Text = $"W5G Extractor V{FrmLogin.CURRENT_VERSION}";
            lblPcKey.Text ="Key: "+ pcKey;

            // Add label "Cột:" vào toolbar
            var lblDoc = new Label { Text = "Cột:", AutoSize = true, Font = new Font("Segoe UI", 9F), ForeColor = Color.FromArgb(166, 173, 200), Location = new Point(745, 12) };
            pnlToolbar.Controls.Add(lblDoc);

            // Add CheckBox "Chọn tất cả" near lblInput
            chkCheckAll = new CheckBox
            {
                Text = "Chọn tất cả",
                ForeColor = Color.FromArgb(205, 214, 244),
                Location = new Point(100, 86),
                AutoSize = true,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold)
            };
            chkCheckAll.CheckedChanged += ChkCheckAll_CheckedChanged;
            this.Controls.Add(chkCheckAll);
            chkCheckAll.BringToFront();

            // Add CheckBox "Chọn tất cả" near lblOutput
            chkCheckAllOutput = new CheckBox
            {
                Text = "Chọn tất cả",
                ForeColor = Color.FromArgb(205, 214, 244),
                Location = new Point(120, 300),
                AutoSize = true,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold)
            };
            chkCheckAllOutput.CheckedChanged += ChkCheckAllOutput_CheckedChanged;
            this.Controls.Add(chkCheckAllOutput);
            chkCheckAllOutput.BringToFront();

            InitGridOutput();
            LoadColumnSettings();
            GenerateColumnFilters();
            LoadGridLayoutSettings();

            // Áp dụng scrollbar màu xám (dark mode) - gọi trực tiếp sau khi Handle đã có
            ApplyGrayScrollBar(dgvInput);
            ApplyGrayScrollBar(dgvOutput);

            dgvInput.CurrentCellDirtyStateChanged += dgvInput_CurrentCellDirtyStateChanged;
            dgvInput.CellValueChanged += dgvInput_CellValueChanged;

            // Save ngay khi nhập
            txt_Thread.TextChanged += (s, ev) => SaveGridLayoutSetting("ColumnThread", txt_Thread.Text.Trim());
            txt_NumHang.TextChanged += (s, ev) => SaveGridLayoutSetting("ColumnNumHang", txt_NumHang.Text.Trim());
            txt_NumCot.TextChanged += (s, ev) => SaveGridLayoutSetting("ColumnNumCot", txt_NumCot.Text.Trim());
        }

        private void LoadGridLayoutSettings()
        {
            string savedThread = Properties.Settings.Default.ColumnThread;
            string savedNumHang = Properties.Settings.Default.ColumnNumHang;
            string savedNumCot = Properties.Settings.Default.ColumnNumCot;
            string savedUseProxy = Properties.Settings.Default.UseProxy;
            string savedUseRecaptcha = Properties.Settings.Default.UseRecaptcha;
            if (!string.IsNullOrEmpty(savedThread)) txt_Thread.Text = savedThread;
            if (!string.IsNullOrEmpty(savedNumHang)) txt_NumHang.Text = savedNumHang;
            if (!string.IsNullOrEmpty(savedNumCot)) txt_NumCot.Text = savedNumCot;
            if (!string.IsNullOrEmpty(savedUseProxy)) chkUseProxy.Checked = savedUseProxy == "True";
            if (!string.IsNullOrEmpty(savedUseRecaptcha)) chkRecaptcha.Checked = savedUseRecaptcha == "True";
        }

        private void SaveGridLayoutSetting(string key, string value)
        {
            switch (key)
            {
                case "ColumnThread": Properties.Settings.Default.ColumnThread = value; break;
                case "ColumnNumHang": Properties.Settings.Default.ColumnNumHang = value; break;
                case "ColumnNumCot": Properties.Settings.Default.ColumnNumCot = value; break;
            }
            Properties.Settings.Default.Save();
        }


        private void btnUploadExcel_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog openFileDialog = new OpenFileDialog())
            {
                openFileDialog.Filter = "Excel Files|*.xlsx;*.xls;*.xlsb";
                openFileDialog.Title = "Chọn file danh sách Shipper";

                if (openFileDialog.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);

                        using (var stream = File.Open(openFileDialog.FileName, FileMode.Open, FileAccess.Read))
                        {
                            using (var reader = ExcelReaderFactory.CreateReader(stream))
                            {
                                var result = reader.AsDataSet(new ExcelDataSetConfiguration()
                                {
                                    ConfigureDataTable = (_) => new ExcelDataTableConfiguration()
                                    {
                                        UseHeaderRow = true
                                    }
                                });

                                DataTable dtOriginal = result.Tables[0];
                                
                                // Tự động phát hiện cấu trúc cột
                                int colCompanyIndex = 0;
                                int colAddressIndex = -1;
                                bool excelHasSTT = false;

                                string firstColName = dtOriginal.Columns[0].ColumnName.Trim().ToUpper();
                                if (firstColName == "STT" || firstColName == "SỐ TT" || firstColName == "NO" || firstColName == "NO.")
                                {
                                    excelHasSTT = true;
                                    colCompanyIndex = 1;
                                    if (dtOriginal.Columns.Count > 2)
                                    {
                                        colAddressIndex = 2;
                                    }
                                }
                                else
                                {
                                    if (dtOriginal.Columns.Count > 1)
                                    {
                                        colAddressIndex = 1;
                                    }
                                }

                                string colCompanyName = dtOriginal.Columns[colCompanyIndex].ColumnName;
                                string colAddressName = colAddressIndex >= 0 ? dtOriginal.Columns[colAddressIndex].ColumnName : "";

                                DataTable dtFiltered;
                                // 4. Lọc lấy các cột chính (bao gồm cột địa chỉ nếu có)
                                if (excelHasSTT)
                                {
                                    if (!string.IsNullOrEmpty(colAddressName))
                                        dtFiltered = dtOriginal.DefaultView.ToTable(false, dtOriginal.Columns[0].ColumnName, colCompanyName, colAddressName);
                                    else
                                        dtFiltered = dtOriginal.DefaultView.ToTable(false, dtOriginal.Columns[0].ColumnName, colCompanyName);
                                }
                                else
                                {
                                    if (!string.IsNullOrEmpty(colAddressName))
                                        dtFiltered = dtOriginal.DefaultView.ToTable(false, colCompanyName, colAddressName);
                                    else
                                        dtFiltered = dtOriginal.DefaultView.ToTable(false, colCompanyName);

                                    // Thêm cột STT vào vị trí đầu tiên (index 0)
                                    DataColumn sttCol = new DataColumn("STT", typeof(int));
                                    dtFiltered.Columns.Add(sttCol);
                                    sttCol.SetOrdinal(0); // Đưa STT lên đầu
                                }

                                // 4.2 Thêm cột Status vào cuối
                                if (!dtFiltered.Columns.Contains("Status"))
                                {
                                    dtFiltered.Columns.Add("Status", typeof(string));
                                }

                                // 4.2b Thêm cột ErrorDetail để hiển thị chi tiết lỗi
                                if (!dtFiltered.Columns.Contains("ErrorDetail"))
                                {
                                    dtFiltered.Columns.Add("ErrorDetail", typeof(string));
                                }

                                // 4.2c Thêm cột Select để chọn dòng chạy
                                if (!dtFiltered.Columns.Contains("Select"))
                                {
                                    DataColumn selectCol = new DataColumn("Select", typeof(bool));
                                    selectCol.DefaultValue = true;
                                    dtFiltered.Columns.Add(selectCol);
                                }

                                // 4.3 Duyệt qua dữ liệu để đánh số thứ tự (nếu Excel chưa có) và gán trạng thái mặc định
                                for (int i = 0; i < dtFiltered.Rows.Count; i++)
                                {
                                    if (!excelHasSTT)
                                    {
                                        dtFiltered.Rows[i]["STT"] = i + 1;
                                    }
                                    dtFiltered.Rows[i]["Select"] = true;
                                    dtFiltered.Rows[i]["Status"] = "Pending";
                                    dtFiltered.Rows[i]["ErrorDetail"] = "";
                                }

                                // 5. Đổ dữ liệu lên Grid
                                dgvInput.DataSource = dtFiltered;
                                if (chkCheckAll != null)
                                {
                                    chkCheckAll.Checked = true;
                                }

                                // Clear old output data before loading new file
                                mydata.Rows.Clear();

                                // === KIỂM TRA NGAY: Công ty nào đã có trong DB → Skipped + load data ===
                                foreach (DataRow r in dtFiltered.Rows)
                                {
                                    string cName = r[colCompanyName]?.ToString() ?? "";
                                    if (!string.IsNullOrEmpty(cName))
                                    {
                                        string dbCompId = GetCompanyIdFromDB(cName);
                                        if (dbCompId != null)
                                        {
                                            r["Status"] = IsCompanyProcessedById(dbCompId) ? "Skipped" : "Pending";
                                            LoadSpecificCompanyToGrid(cName, r["STT"]?.ToString() ?? "", dbCompId);
                                        }
                                    }
                                }

                                FormatGridView(colCompanyName, colAddressName);
                                int InputCount = dtFiltered.Rows.Count;
                                lblInput.Text = $"INPUT: {InputCount}";
                                lblOutput.Text = $"OUTPUT: {mydata.Rows.Count}";
                                // 6. Gọi hàm lưu vào database
                                try
                                {
                                    //SaveDataTableToSQLite(dtFiltered);
                                }
                                catch (Exception ex)
                                {
                                    MessageBox.Show(ex.Message);
                                }
                                //finally
                                //{
                                //    // 2. ẨN VÒNG XOAY KHI XONG
                                //    spinnerLoading.Visible = false;
                                //    btnUploadExcel.Enabled = true;
                                //}
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("Lỗi: " + ex.Message, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }
        
        private async void btnStart_Click(object sender, EventArgs e)
        {
            _isRerunMode = false;
            // 1. KIỂM TRA KEY TRƯỚC KHI CHẠY
            DataTable dtConfig = DatabaseHelper.ExecuteQuery("SELECT aistudio_key FROM Config LIMIT 1");

            if (dtConfig.Rows.Count == 0 || string.IsNullOrEmpty(dtConfig.Rows[0]["aistudio_key"].ToString()))
            {
                MessageBox.Show("Vui lòng vào phần Cấu hình để nhập AI Studio Key trước khi bắt đầu!",
                                "Thiếu cấu hình", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // 2. Đọc số luồng từ txt_Thread
            _threadCount = 1;
            if (int.TryParse(txt_Thread.Text.Trim(), out int tc) && tc > 0)
                _threadCount = Math.Min(tc, 20); // Giới hạn tối đa 20 luồng

            // Đọc số dòng/cột để sắp xếp Chrome
            // txt_NumHang = số dòng (rows), txt_NumCot = số cột (columns)
            _numHang = 2;
            _numCot = 5;
            if (int.TryParse(txt_NumHang.Text.Trim(), out int nh) && nh > 0)
                _numHang = nh;  // số dòng Chrome
            if (int.TryParse(txt_NumCot.Text.Trim(), out int nc) && nc > 0)
                _numCot = nc;   // số cột Chrome

            // Lưu settings cho lần sau
            Properties.Settings.Default.ColumnThread = _threadCount.ToString();
            Properties.Settings.Default.ColumnNumHang = _numHang.ToString();
            Properties.Settings.Default.ColumnNumCot = _numCot.ToString();
            Properties.Settings.Default.UseProxy = chkUseProxy.Checked.ToString();
            Properties.Settings.Default.UseRecaptcha = chkRecaptcha.Checked.ToString();
            Properties.Settings.Default.Save();

            _cts = new CancellationTokenSource();
            _activeDrivers = new ConcurrentBag<IWebDriver>();
            btnStart.Enabled = false;
            btnStop.Enabled = true;
            lblStatus.Text = $"Running ({_threadCount} threads, {_numHang}×{_numCot})";

            DataTable dtInput = (DataTable)dgvInput.DataSource;
            if (dtInput == null || dtInput.Rows.Count == 0)
            {
                btnStart.Enabled = true;
                btnStop.Enabled = false;
                lblStatus.Text = "Ready";
                return;
            }

            try
            {
                // 3. Thu thập các rows được check
                var allRows = new List<DataRow>();
                foreach (DataRow r in dtInput.Rows)
                {
                    bool isSelected = true;
                    if (dtInput.Columns.Contains("Select"))
                    {
                        isSelected = r.Field<bool?>("Select") ?? false;
                    }
                    if (isSelected)
                        allRows.Add(r);
                }

                if (allRows.Count == 0)
                {
                    MessageBox.Show("Vui lòng chọn ít nhất một dòng để chạy!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    btnStart.Enabled = true;
                    btnStop.Enabled = false;
                    lblStatus.Text = "Ready";
                    return;
                }

                // 4. Chia đều rows cho N threads (round-robin)
                var batches = new List<List<DataRow>>();
                for (int t = 0; t < _threadCount; t++)
                    batches.Add(new List<DataRow>());
                for (int i = 0; i < allRows.Count; i++)
                    batches[i % _threadCount].Add(allRows[i]);

                // 5. Khởi chạy N tasks song song
                var tasks = new List<Task>();
                for (int t = 0; t < _threadCount; t++)
                {
                    int threadIdx = t;
                    var batch = batches[t];
                    if (batch.Count == 0) continue;
                    tasks.Add(Task.Run(async () => await ProcessBatch(batch, threadIdx, _cts.Token)));
                }

                await Task.WhenAll(tasks);

                // 6. AUTO RETRY: Chạy lại các công ty "Completed but Empty" hoặc Pending
                if (!_cts.Token.IsCancellationRequested)
                {
                    var emptyRows = new List<DataRow>();
                    foreach (DataRow r in dtInput.Rows)
                    {
                        bool isSelected = true;
                        if (dtInput.Columns.Contains("Select"))
                        {
                            isSelected = r.Field<bool?>("Select") ?? false;
                        }
                        if (!isSelected) continue;

                        string st = r["Status"]?.ToString() ?? "";
                        if (st == "Completed but Empty" || string.IsNullOrEmpty(st) || st == "Pending")
                            emptyRows.Add(r);
                    }

                    if (emptyRows.Count > 0)
                    {
                        lblStatus.Text = $"[Retry] Chạy lại {emptyRows.Count} công ty trống data...";

                        var retryBatches = new List<List<DataRow>>();
                        for (int t = 0; t < _threadCount; t++)
                            retryBatches.Add(new List<DataRow>());
                        for (int i = 0; i < emptyRows.Count; i++)
                            retryBatches[i % _threadCount].Add(emptyRows[i]);

                        var retryTasks = new List<Task>();
                        for (int t = 0; t < _threadCount; t++)
                        {
                            int threadIdx = t;
                            var batch = retryBatches[t];
                            if (batch.Count == 0) continue;
                            retryTasks.Add(Task.Run(async () => await ProcessBatch(batch, threadIdx, _cts.Token)));
                        }
                        await Task.WhenAll(retryTasks);
                    }
                }

                // 7. AUTO RETRY WEBSITE (cũng đa luồng)
                if (!_cts.Token.IsCancellationRequested)
                {
                    await RetryMissingWebsiteMultiThread(dtInput, _cts.Token);
                }
            }
            catch (Exception ex)
            {
                //MessageBox.Show("Lỗi hệ thống: " + ex.Message);
            }
            finally
            {
                SortOutputGridBySTT();
                lblStatus.Text = _cts.Token.IsCancellationRequested ? "Stopped" : "Finished";
                btnStart.Enabled = true;
                btnStop.Enabled = false;
            }
        }
        private async Task ProcessGoogleCrawlBatch(List<DataRow> rows, int threadIndex, CancellationToken token)
        {
            int sttCount = 0;
            lock (_gridLock) { sttCount = mydata.Rows.Count + 1; }
            int proxyIndex = threadIndex;
            int currentRowIndex = 0;
            string tTag = $"[T{threadIndex + 1}]";

            while (currentRowIndex < rows.Count && !token.IsCancellationRequested)
            {
                IWebDriver localDriver = null;
                bool captchaRestart = false;
                try
                {
                    localDriver = await InitChromeDriverAsync(proxyIndex, threadIndex);
                    _activeDrivers.Add(localDriver);
                    PositionChromeWindow(localDriver, threadIndex);

                    for (int i = currentRowIndex; i < rows.Count; i++)
                    {
                        if (token.IsCancellationRequested) break;

                        DataRow row = rows[i];
                        currentRowIndex = i;

                        this.Invoke(new Action(() => lblStatus.Text = $"{tTag} Processing..."));

                        string CompanyName = row[1].ToString();

                        // === Lấy địa chỉ từ file upload (nếu có cột thứ 3) ===
                        string uploadedAddress = "";
                        try
                        {
                            DataTable dtSrc = row.Table;
                            for (int ci = 2; ci < dtSrc.Columns.Count; ci++)
                            {
                                string colName = dtSrc.Columns[ci].ColumnName;
                                if (colName == "STT" || colName == "Status" || colName == "ErrorDetail" || colName == "Select") continue;
                                string val = row[ci]?.ToString()?.Trim() ?? "";
                                if (!string.IsNullOrEmpty(val))
                                {
                                    uploadedAddress = val;
                                    break;
                                }
                            }
                        }
                        catch { }

                        string status = row["Status"]?.ToString();
                        bool alreadyDone = status == "Completed" || status == "Skipped"
                                          || IsCompanyProcessed(CompanyName);
                        bool forceRerun = status == "Completed but Empty" || status == "Pending";

                        if (alreadyDone && !forceRerun)
                        {
                            this.Invoke(new Action(() =>
                            {
                                row["Status"] = "Skipped";
                                row["Select"] = false;
                                LoadSpecificCompanyToGrid(CompanyName, row["STT"]?.ToString() ?? "");
                            }));
                            currentRowIndex++;
                            continue;
                        }

                        try
                        {
                        string searchPrompt = $@"
                                Find public information about the company named '{CompanyName}'.
                                        Return ONLY the following fields in structured text:
                                        
                                        CompanyName (Cleaned Core Name Only. Correct any typos or formatting issues in the input. Strip ALL legal prefixes/suffixes like 'CÔNG TY TNHH', 'CÔNG TY CỔ PHẦN', 'TNHH', 'CTCP', 'Co., Ltd', 'JSC', 'Inc'. Return ONLY the main trade/brand name. Example: If input is 'cong ty tnhh scansia pacific' or 'scansia pacific co', return ONLY 'SCANSIA PACIFIC')                                        
                                        Website (FULL URL)
                                        Address
                                        Business industry
                                        Phone
                                        Email
                                        LinkedIn (FULL URL)

                                        Executives (List if available)                                        
                                        Name
                                        Position
                                        LinkedIn (FULL URL)  

                                        Rules for Email and Phone:
                                        - Extract email and phone ONLY if they are explicitly written on:
                                          - Official website
                                          - Contact page
                                          - About page
                                          - Footer
                                        - Copy email/phone EXACTLY as displayed.
                                        - Do NOT infer, guess, or generate patterns.
                                        - If not explicitly found, return 'N/A'.

                                        Rules for LinkedIn:
                                        - Must be a FULL URL pointing to a specific company or profile.
                                        - If result is only 'https://www.linkedin.com', return 'N/A'.
                                        - If the LinkedIn URL is missing, extract the root domain from the email address (e.g., ctmay10@garco10.com.vn → garco10) and use it as the search keyword.

                                        Rules for Executives:
                                        - Return executives ONLY if their names and roles are explicitly written
                                          on the official website or the company's LinkedIn page.
                                        - For each executive, include:
                                          - Full name
                                          - Role/title
                                          - LinkedIn URL (if explicitly available)
                                        - Do NOT infer executives from news, Wikipedia, or third-party sites.
                                        - If no executives are explicitly found, return 'Executives: N/A'.

                                        IMPORTANT:
                                        - Do NOT fabricate any data.
                                        - Partial information is allowed.
                                        - Accuracy is more important than completeness.
                                        - IMPORTANT: If a number is identified as Fax, prefix it with 'Fax: ' in the phone field.
                                ";
                        string encodedPrompt = Uri.EscapeDataString(searchPrompt);
                        string fullUrl = $"https://www.google.com/search?q={encodedPrompt}&udm=50";
                        CloseExtraTabs(localDriver);
                        localDriver.Navigate().GoToUrl(fullUrl);

                        await Task.Delay(3500, token);
                        bool isCaptcha = true;
                        for (int poll = 0; poll < 10; poll++)
                        {
                            var elements = localDriver.FindElements(By.CssSelector("[data-container-id='main-col']"));
                            if (elements.Count > 0) { isCaptcha = false; break; }
                            await Task.Delay(500, token);
                        }

                        if (isCaptcha) 
                        {
                            bool useRecaptcha = false;
                            this.Invoke(new Action(() => useRecaptcha = chkRecaptcha.Checked));
                            if (useRecaptcha)
                            {
                                bool solved = await SolveCaptchaWith2Captcha(localDriver, tTag, row, token);
                                if (solved)
                                {
                                    await Task.Delay(2000, token);
                                    var checkEls = localDriver.FindElements(By.CssSelector("[data-container-id='main-col']"));
                                    if (checkEls.Count == 0)
                                    {
                                        localDriver.Navigate().GoToUrl(fullUrl);
                                        await Task.Delay(2000, token);
                                    }
                                }
                                else
                                {
                                    this.BeginInvoke(new Action(() => lblStatus.Text = $"{tTag} 2Captcha failed - Changing Proxy"));
                                    string proxyString2 = GetProxyFromDB();
                                    string[] proxyList2 = string.IsNullOrEmpty(proxyString2)
                                                        ? new string[0]
                                                        : proxyString2.Split(new[] { "\n", "\r\n" }, StringSplitOptions.RemoveEmptyEntries);

                                    bool useProxy2 = false;
                                    this.Invoke(new Action(() => useProxy2 = chkUseProxy.Checked));
                                    if (useProxy2 && proxyList2.Length > 0)
                                        proxyIndex = (proxyIndex + _threadCount) % proxyList2.Length;

                                    throw new Exception("CaptchaDetected");
                                }
                            }
                            else
                            {
                                this.Invoke(new Action(() => lblStatus.Text = $"{tTag} Captcha - Changing Proxy"));
                                string proxyString = GetProxyFromDB();
                                string[] proxyList = string.IsNullOrEmpty(proxyString)
                                                    ? new string[0]
                                                    : proxyString.Split(new[] { "\n", "\r\n" }, StringSplitOptions.RemoveEmptyEntries);

                                  bool useProxy = false;
                                this.Invoke(new Action(() => useProxy = chkUseProxy.Checked));
                                if (useProxy && proxyList.Length > 0)
                                    proxyIndex = (proxyIndex + _threadCount) % proxyList.Length;

                                throw new Exception("CaptchaDetected");
                            }
                        }

                        var mainColElement = localDriver.FindElements(By.CssSelector("[data-container-id ='main-col']")).FirstOrDefault();
                        if (mainColElement == null)
                        {
                            if (localDriver.PageSource.Contains("did not match any documents"))
                            {
                                this.Invoke(new Action(() =>
                                {
                                    row["Status"] = "Not Found";
                                    lblStatus.Text = $"{tTag} Not Found";
                                }));
                                currentRowIndex++;
                                continue;
                            }
                            else
                            {
                                throw new Exception("CaptchaDetected");
                            }
                        }

                        string fullPageText = "";
                        {
                            string prevText = "";
                            int stableCount = 0;
                            int maxPollSeconds = 30;
                            for (int sec = 0; sec < maxPollSeconds && !token.IsCancellationRequested; sec++)
                            {
                                try
                                {
                                    mainColElement = localDriver.FindElements(By.CssSelector("[data-container-id ='main-col']")).FirstOrDefault();
                                    string currentText = mainColElement?.Text ?? "";

                                    this.Invoke(new Action(() =>
                                        lblStatus.Text = $"{tTag} Loading... ({currentText.Length} chars, stable={stableCount}/2)"
                                    ));

                                    if (currentText.Length > 200 && currentText == prevText)
                                    {
                                        stableCount++;
                                        if (stableCount >= 2)
                                        {
                                            fullPageText = currentText;
                                            break;
                                        }
                                    }
                                    else
                                    {
                                        stableCount = 0;
                                        prevText = currentText;
                                    }
                                }
                                catch { }
                                await Task.Delay(1000, token);
                            }

                            if (string.IsNullOrEmpty(fullPageText))
                                fullPageText = mainColElement?.Text ?? "";
                        }
                        string Prompt = "Trích xuất thông tin từ text sau: \n*** " + fullPageText + " ***. \n. - DO NOT use markdown\r\n- DO NOT wrap with ```json \r\n- Lấy TẤT CẢ số điện thoại và email tìm thấy. Nếu có nhiều, mỗi số nằm trên 1 dòng (cách nhau bằng \\n)\r\n- QUAN TRỌNG: Nếu là số Fax, hãy thêm tiền tố 'Fax: ' vào trước số đó và để ở dòng bên dưới số điện thoại.\r\n.Trả về JSON theo đúng schema: { 'companies': [{ 'name': '', 'website': '','industry': '', 'address': '', 'phone': 'số1\\nFax: số2', 'email': 'mail1\\nmail2', 'linkedin': '', 'leaders': [{ 'name': '', 'position': '', 'linkedin': '', 'email': '', 'phone': '' }] }] }";

                        this.Invoke(new Action(() => lblStatus.Text = $"{tTag} AI Enrich:"));
                        string result = await CallOpenAI(Prompt); 
                        if (result == "{}") result = await CallGeminiAPI(Prompt);
                        if (result == "{}" || string.IsNullOrWhiteSpace(result))
                        {
                            string apiErrMsg = "AI API không trả về dữ liệu hợp lệ";
                            Console.WriteLine($"{tTag} [SKIP] API fail: {CompanyName}");
                            this.Invoke(new Action(() =>
                            {
                                row["Status"] = "API Error";
                                row["ErrorDetail"] = apiErrMsg;
                                lblStatus.Text = $"{tTag} API Error - Skip: {CompanyName}";
                            }));
                            currentRowIndex++;
                            continue;
                        }
                        if (!string.IsNullOrEmpty(result))
                        {
                            string cleanJson = ExtractJsonSafe(result);
                            dynamic data = JsonConvert.DeserializeObject(cleanJson);
                            if (data?.companies != null)
                            {
                                foreach (var item in data.companies)
                                {
                                    if (!string.IsNullOrEmpty(uploadedAddress))
                                    {
                                        item.address = uploadedAddress;
                                    }
                                }
                                foreach (var item in data.companies)
                                {
                                    string rawWeb = item.website?.ToString() ?? "";
                                    if (!string.IsNullOrEmpty(rawWeb))
                                    {
                                        string web = rawWeb.ToLower().Trim();
                                        if (!web.StartsWith("http")) web = "https://" + web;
                                        try
                                        {
                                            var uri = new Uri(web);
                                            string host = uri.Host;
                                            if (host.StartsWith("www.")) host = host.Substring(4);
                                            item.website = host;
                                        }
                                        catch
                                        {
                                            item.website = rawWeb.Replace("https://", "").Replace("http://", "").Replace("www.", "").TrimEnd('/');
                                        }
                                    }
                                    else
                                    {
                                        item.website = "N/A";
                                    }
                                    string rawLinkedIn = item.linkedin?.ToString() ?? "";
                                    if (rawLinkedIn == "https://www.linkedin.com" || rawLinkedIn == "http://www.linkedin.com" || rawLinkedIn == "www.linkedin.com")
                                    {
                                        item.linkedin = "N/A";
                                    }
                                    else if (!string.IsNullOrEmpty(rawLinkedIn))
                                    {
                                         item.linkedin = CleanLinkedInUrl(rawLinkedIn);
                                    }

                                     if (item.leaders != null)
                                     {
                                         foreach (var leader in item.leaders)
                                         {
                                             string leaderLinkedIn = leader.linkedin?.ToString() ?? "";
                                             if (leaderLinkedIn == "https://www.linkedin.com" || leaderLinkedIn == "http://www.linkedin.com" || leaderLinkedIn == "www.linkedin.com")
                                             {
                                                 leader.linkedin = "N/A";
                                             }
                                             else if (!string.IsNullOrEmpty(leaderLinkedIn))
                                            {
                                                leader.linkedin = CleanLinkedInUrl(leaderLinkedIn);
                                            }
                                        }
                                    }

                                                                         var crawlRes = await SaveCrawlResult((object)item, CompanyName);
                                     string personId = crawlRes.lastPersonId ?? "";
                                     string companyId = crawlRes.companyId;
                                    bool isEmpty = IsResultEmpty(item);

                                    this.Invoke(new Action(() =>
                                      {
                                          lock (_gridLock)
                                          {
                                              UpdateGridOutput(item, CompanyName, personId.ToString(), companyId, row["STT"]?.ToString() ?? "", ref sttCount);
                                          }
                                          row["Status"] = isEmpty ? "Completed but Empty" : "Completed";
                                          row["Select"] = false;
                                      }));
                                }
                            }
                        }
                        this.Invoke(new Action(() => lblStatus.Text = $"{tTag} Processing"));
                        currentRowIndex++;
                        await Task.Delay(2000, token);
                        }
                        catch (Exception rowEx)
                        {
                            if (rowEx.Message == "CaptchaDetected") throw;

                            string errDetail = rowEx.Message;
                            if (rowEx.StackTrace != null)
                            {
                                var firstStack = rowEx.StackTrace.Split('\n').FirstOrDefault()?.Trim();
                                if (!string.IsNullOrEmpty(firstStack))
                                    errDetail += "\n" + firstStack;
                            }
                            Console.WriteLine($"{tTag} [Row Error] {CompanyName}: {rowEx.Message}");
                            this.Invoke(new Action(() =>
                            {
                                row["Status"] = "Error";
                                row["ErrorDetail"] = errDetail;
                                lblStatus.Text = $"{tTag} Error at {CompanyName}: {rowEx.Message}";
                            }));
                            currentRowIndex++;
                        }
                    }
                    break;
                }
                catch (OperationCanceledException)
                {
                    Console.WriteLine($"{tTag} Đã dừng theo yêu cầu.");
                    break;
                }
                catch (Exception ex)
                {
                    if (ex.Message == "CaptchaDetected")
                    {
                        Console.WriteLine($"{tTag} Captcha detected. Restarting Chrome...");
                        captchaRestart = true;
                    }
                    else
                    {
                        Console.WriteLine($"{tTag} System error: " + ex.Message);
                        this.Invoke(new Action(() => lblStatus.Text = $"{tTag} System Error: {ex.Message}"));
                        currentRowIndex++;
                    }

                    if (captchaRestart && !token.IsCancellationRequested)
                        await Task.Delay(3000, token);
                }
                finally
                {
                    if (localDriver != null)
                    {
                        try { localDriver.Quit(); localDriver.Dispose(); } catch { }
                    }
                }
            }

            this.Invoke(new Action(() => lblStatus.Text = $"{tTag} Done!"));
        }

        private class WebsiteCrawlTarget
        {
            public string CompanyName { get; set; }
            public string Website { get; set; }
            public string ExistingPhone { get; set; }
            public string ExistingEmail { get; set; }
            public string ExistingLinkedIn { get; set; }
            public string STT { get; set; }
            public DataRow InputRow { get; set; }
        }

        private class LinkedInCrawlTarget
        {
            public string CompanyName { get; set; }
            public string LinkedInCo { get; set; }
            public string STT { get; set; }
            public DataRow InputRow { get; set; }
        }

        private async Task ProcessWebsiteCrawlBatch(List<WebsiteCrawlTarget> targets, int threadIndex, CancellationToken token)
        {
            int proxyIndex = threadIndex;
            string tTag = $"[T{threadIndex + 1}]";
            IWebDriver localDriver = null;

            try
            {
                localDriver = await InitChromeDriverAsync(proxyIndex, threadIndex);
                _activeDrivers.Add(localDriver);
                PositionChromeWindow(localDriver, threadIndex);

                for (int i = 0; i < targets.Count; i++)
                {
                    if (token.IsCancellationRequested) break;
                    var target = targets[i];

                    this.Invoke(new Action(() => lblStatus.Text = $"{tTag} Crawling Website: {target.CompanyName}..."));

                    try
                    {
                        if (target.InputRow != null)
                        {
                            this.Invoke(new Action(() => target.InputRow["Status"] = "Deep Crawl Home"));
                        }

                        string companyId = "";
                        DataTable dtComp = DatabaseHelper.ExecuteQuery("SELECT ID FROM Company WHERE CompanyName = $name COLLATE NOCASE LIMIT 1", new[] { new SqliteParameter("$name", target.CompanyName) });
                        if (dtComp.Rows.Count > 0)
                        {
                            companyId = dtComp.Rows[0]["ID"].ToString();
                        }

                        var (deepPhone, deepEmail, deepLinkedIn) = await EnrichContactFromWebsite(
                            localDriver, target.Website, tTag, null, token);

                        bool hasPhone = !string.IsNullOrEmpty(deepPhone);
                        bool hasEmail = !string.IsNullOrEmpty(deepEmail);
                        bool hasLinkedIn = !string.IsNullOrEmpty(deepLinkedIn);

                        string finalPhone = target.ExistingPhone;
                        string finalEmail = target.ExistingEmail;
                        string finalLinkedIn = target.ExistingLinkedIn;

                        if (hasPhone)
                        {
                            var allPhones = (target.ExistingPhone + " - " + deepPhone)
                                .Split(new[] { " - ", "\n" }, StringSplitOptions.RemoveEmptyEntries)
                                .Select(s => s.Trim())
                                .Where(s => !string.IsNullOrEmpty(s) && s.ToUpper() != "N/A")
                                .Distinct(StringComparer.OrdinalIgnoreCase);
                            finalPhone = string.Join("\n", allPhones);
                        }
                        if (hasEmail)
                        {
                            var allEmails = (target.ExistingEmail + " - " + deepEmail)
                                .Split(new[] { " - ", "\n" }, StringSplitOptions.RemoveEmptyEntries)
                                .Select(s => s.Trim())
                                .Where(s => !string.IsNullOrEmpty(s) && s.ToUpper() != "N/A")
                                .Distinct(StringComparer.OrdinalIgnoreCase);
                            finalEmail = string.Join("\n", allEmails);
                        }
                        if (hasLinkedIn)
                        {
                            finalLinkedIn = CleanLinkedInUrl(deepLinkedIn);
                        }

                                                if (!hasPhone && !hasEmail && !hasLinkedIn)
                        {
                            string cleanedCurPhone = CleanPhone(target.ExistingPhone);
                            string cleanedCurEmail = CleanPhone(target.ExistingEmail);

                            bool hasAnyContact = (!string.IsNullOrEmpty(cleanedCurPhone) && cleanedCurPhone.ToUpper() != "N/A") ||
                                                 (!string.IsNullOrEmpty(cleanedCurEmail) && cleanedCurEmail.ToUpper() != "N/A");

                            if (!hasAnyContact)
                            {
                                if (string.IsNullOrEmpty(cleanedCurPhone) || cleanedCurPhone.ToUpper() == "N/A")
                                {
                                    finalPhone = "Lỗi trích xuất email, số điện thoại";
                                }
                                else
                                {
                                    finalPhone = cleanedCurPhone + "\nLỗi trích xuất email, số điện thoại";
                                }
                            }
                            else
                            {
                                finalPhone = cleanedCurPhone;
                            }
                        }
                        else
                        {
                            finalPhone = CleanPhone(finalPhone);
                        }

                        if (string.IsNullOrEmpty(companyId))
                        {
                            companyId = Guid.NewGuid().ToString();
                            DatabaseHelper.ExecuteNonQuery(
                                "INSERT INTO Company (ID, CompanyName, Website, Phone, Email, Linkedin, LastUpdate) VALUES ($id, $name, $web, $phone, $email, $link, $date)",
                                new[] {
                                    new SqliteParameter("$id", companyId),
                                    new SqliteParameter("$name", target.CompanyName),
                                    new SqliteParameter("$web", target.Website ?? ""),
                                    new SqliteParameter("$phone", finalPhone),
                                    new SqliteParameter("$email", finalEmail),
                                    new SqliteParameter("$link", finalLinkedIn),
                                    new SqliteParameter("$date", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"))
                                });
                        }
                        else
                        {
                            DatabaseHelper.ExecuteNonQuery(
                                "UPDATE Company SET Phone=$phone, Email=$email, Linkedin=$link, LastUpdate=$date WHERE ID=$id",
                                new[] {
                                    new SqliteParameter("$phone", finalPhone),
                                    new SqliteParameter("$email", finalEmail),
                                    new SqliteParameter("$link", finalLinkedIn),
                                    new SqliteParameter("$date", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")),
                                    new SqliteParameter("$id", companyId)
                                });
                        }

                        this.Invoke(new Action(() => {
                            for (int ri = 0; ri < mydata.Rows.Count; ri++)
                            {
                                string cn = mydata.Rows[ri]["CompanyName"]?.ToString() ?? "";
                                string rt = mydata.Rows[ri]["RowType"]?.ToString() ?? "";
                                if (rt == "Company" && string.Equals(cn.Trim(), target.CompanyName.Trim(), StringComparison.OrdinalIgnoreCase))
                                {
                                    mydata.Rows[ri]["PhoneCo"] = finalPhone;
                                    mydata.Rows[ri]["EmailCo"] = finalEmail;
                                    mydata.Rows[ri]["LinkedInCo"] = finalLinkedIn;
                                    mydata.Rows[ri]["CompanyID"] = companyId;
                                    break;
                                }
                            }
                            if (target.InputRow != null)
                            {
                                target.InputRow["Status"] = "Completed";
                                target.InputRow["Select"] = false;
                            }
                        }));
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"{tTag} Lỗi crawl website cho {target.CompanyName}: {ex.Message}");
                        if (target.InputRow != null)
                        {
                            this.Invoke(new Action(() => {
                                target.InputRow["Status"] = "Error";
                                target.InputRow["ErrorDetail"] = ex.Message;
                            }));
                        }
                    }
                    finally
                    {
                        CloseExtraTabs(localDriver);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"{tTag} Lỗi khởi động Chrome: {ex.Message}");
            }
            finally
            {
                if (localDriver != null)
                {
                    try { localDriver.Quit(); localDriver.Dispose(); } catch { }
                }
            }
        }

        private async Task ProcessLinkedInCrawlBatch(List<LinkedInCrawlTarget> targets, int threadIndex, CancellationToken token)
        {
            int proxyIndex = threadIndex;
            string tTag = $"[T{threadIndex + 1}]";
            IWebDriver localDriver = null;

            try
            {
                localDriver = await InitChromeDriverAsync(proxyIndex, threadIndex);
                _activeDrivers.Add(localDriver);
                PositionChromeWindow(localDriver, threadIndex);

                for (int i = 0; i < targets.Count; i++)
                {
                    if (token.IsCancellationRequested) break;
                    var target = targets[i];

                    this.Invoke(new Action(() => lblStatus.Text = $"{tTag} Crawling LinkedIn: {target.CompanyName}..."));

                    try
                    {
                        if (target.InputRow != null)
                        {
                            this.Invoke(new Action(() => target.InputRow["Status"] = "Crawl LinkedIn"));
                        }

                        await GetPersionFromLinkedInCompany(
                            target.CompanyName,
                            target.CompanyName,
                            target.LinkedInCo,
                            token,
                            localDriver);

                        if (target.InputRow != null)
                        {
                            this.Invoke(new Action(() => {
                                target.InputRow["Status"] = "Completed";
                                target.InputRow["Select"] = false;
                            }));
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"{tTag} Lỗi crawl LinkedIn cho {target.CompanyName}: {ex.Message}");
                        if (target.InputRow != null)
                        {
                            this.Invoke(new Action(() => {
                                target.InputRow["Status"] = "Error";
                                target.InputRow["ErrorDetail"] = ex.Message;
                            }));
                        }
                    }
                    finally
                    {
                        CloseExtraTabs(localDriver);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"{tTag} Lỗi khởi động Chrome: {ex.Message}");
            }
            finally
            {
                if (localDriver != null)
                {
                    try { localDriver.Quit(); localDriver.Dispose(); } catch { }
                }
            }
        }

        private async Task ProcessBatch(List<DataRow> rows, int threadIndex, CancellationToken token)
        {
            int sttCount = 0;
            lock (_gridLock) { sttCount = mydata.Rows.Count + 1; }
            int proxyIndex = threadIndex;
            int currentRowIndex = 0;
            string tTag = $"[T{threadIndex + 1}]";

            while (currentRowIndex < rows.Count && !token.IsCancellationRequested)
            {
                IWebDriver localDriver = null;
                bool captchaRestart = false;
                try
                {
                    localDriver = await InitChromeDriverAsync(proxyIndex, threadIndex);
                    _activeDrivers.Add(localDriver);
                    PositionChromeWindow(localDriver, threadIndex);

                    for (int i = currentRowIndex; i < rows.Count; i++)
                    {
                        if (token.IsCancellationRequested) break;

                        DataRow row = rows[i];
                        currentRowIndex = i;

                        this.Invoke(new Action(() => lblStatus.Text = $"{tTag} Processing..."));

                        string CompanyName = row[1].ToString();

                        // === Lấy địa chỉ từ file upload (nếu có cột thứ 3) ===
                        string uploadedAddress = "";
                        try
                        {
                            // Cột 0=STT, 1=col1(CompanyName), 2=col2, 3+=có thể là Address
                            DataTable dtSrc = row.Table;
                            for (int ci = 2; ci < dtSrc.Columns.Count; ci++)
                            {
                                string colName = dtSrc.Columns[ci].ColumnName;
                                if (colName == "STT" || colName == "Status" || colName == "ErrorDetail" || colName == "Select") continue;
                                string val = row[ci]?.ToString()?.Trim() ?? "";
                                if (!string.IsNullOrEmpty(val))
                                {
                                    uploadedAddress = val;
                                    break; // Lấy giá trị đầu tiên không rỗng sau cột company name
                                }
                            }
                        }
                        catch { }

                        string status = row["Status"]?.ToString();
                        bool alreadyDone = status == "Completed" || status == "Skipped"
                                          || IsCompanyProcessed(CompanyName);
                        bool forceRerun = status == "Completed but Empty" || status == "Pending";

                        if (alreadyDone && !forceRerun)
                        {
                            this.Invoke(new Action(() =>
                            {
                                row["Status"] = "Skipped";
                                row["Select"] = false;
                                LoadSpecificCompanyToGrid(CompanyName, row["STT"]?.ToString() ?? "");
                            }));
                            currentRowIndex++;
                            continue;
                        }

                        try
                        {
                        string searchPrompt = $@"
                                Find public information about the company named '{CompanyName}'.
                                        Return ONLY the following fields in structured text:
                                        
                                        CompanyName (Cleaned Core Name Only. Correct any typos or formatting issues in the input. Strip ALL legal prefixes/suffixes like 'CÔNG TY TNHH', 'CÔNG TY CỔ PHẦN', 'TNHH', 'CTCP', 'Co., Ltd', 'JSC', 'Inc'. Return ONLY the main trade/brand name. Example: If input is 'cong ty tnhh scansia pacific' or 'scansia pacific co', return ONLY 'SCANSIA PACIFIC')                                        
                                        Website (FULL URL)
                                        Address
                                        Business industry
                                        Phone
                                        Email
                                        LinkedIn (FULL URL)

                                        Executives (List if available)                                        
                                        Name
                                        Position
                                        LinkedIn (FULL URL)  

                                        Rules for Email and Phone:
                                        - Extract email and phone ONLY if they are explicitly written on:
                                          - Official website
                                          - Contact page
                                          - About page
                                          - Footer
                                        - Copy email/phone EXACTLY as displayed.
                                        - Do NOT infer, guess, or generate patterns.
                                        - If not explicitly found, return 'N/A'.

                                        Rules for LinkedIn:
                                        - Must be a FULL URL pointing to a specific company or profile.
                                        - If result is only 'https://www.linkedin.com', return 'N/A'.
                                        - If the LinkedIn URL is missing, extract the root domain from the email address (e.g., ctmay10@garco10.com.vn → garco10) and use it as the search keyword.

                                        Rules for Executives:
                                        - Return executives ONLY if their names and roles are explicitly written
                                          on the official website or the company's LinkedIn page.
                                        - For each executive, include:
                                          - Full name
                                          - Role/title
                                          - LinkedIn URL (if explicitly available)
                                        - Do NOT infer executives from news, Wikipedia, or third-party sites.
                                        - If no executives are explicitly found, return 'Executives: N/A'.

                                        IMPORTANT:
                                        - Do NOT fabricate any data.
                                        - Partial information is allowed.
                                        - Accuracy is more important than completeness.
                                        - IMPORTANT: If a number is identified as Fax, prefix it with 'Fax: ' in the phone field.
                                ";
                        string encodedPrompt = Uri.EscapeDataString(searchPrompt);
                        string fullUrl = $"https://www.google.com/search?q={encodedPrompt}&udm=50";
                        // Đóng tab thừa trước khi navigate (tránh tích tụ tab từ website/popup)
                        CloseExtraTabs(localDriver);
                        localDriver.Navigate().GoToUrl(fullUrl);

                        await Task.Delay(3500, token);
                        // Kiểm tra captcha nhanh: poll 500ms x 10 lần = max 5s thay vì chờ 15s timeout
                        bool isCaptcha = true;
                        for (int poll = 0; poll < 10; poll++)
                        {
                            var elements = localDriver.FindElements(By.CssSelector("[data-container-id='main-col']"));
                            if (elements.Count > 0) { isCaptcha = false; break; }
                            await Task.Delay(500, token);
                        }

                        // CAPTCHA
                        if (isCaptcha) 
                        {
                            bool useRecaptcha = false;
                            this.Invoke(new Action(() => useRecaptcha = chkRecaptcha.Checked));
                            if (useRecaptcha)
                            {
                                // Gọi 2Captcha API giải CAPTCHA
                                bool solved = await SolveCaptchaWith2Captcha(localDriver, tTag, row, token);
                                if (solved)
                                {
                                    // Giải thành công → chờ chút rồi thử lại URL tìm kiếm
                                    await Task.Delay(2000, token);
                                    
                                    // Kiểm tra xem đã qua captcha chưa
                                    var checkEls = localDriver.FindElements(By.CssSelector("[data-container-id='main-col']"));
                                    if (checkEls.Count == 0)
                                    {
                                        // Chưa qua → navigate lại URL
                                        localDriver.Navigate().GoToUrl(fullUrl);
                                        await Task.Delay(2000, token);
                                    }
                                }
                                else
                                {
                                    // Giải thất bại → đổi proxy như bình thường
                                    this.BeginInvoke(new Action(() => lblStatus.Text = $"{tTag} 2Captcha failed - Changing Proxy"));
                                    
                                    string proxyString2 = GetProxyFromDB();
                                    string[] proxyList2 = string.IsNullOrEmpty(proxyString2)
                                                        ? new string[0]
                                                        : proxyString2.Split(new[] { "\n", "\r\n" }, StringSplitOptions.RemoveEmptyEntries);

                                    bool useProxy2 = false;
                                    this.Invoke(new Action(() => useProxy2 = chkUseProxy.Checked));
                                    if (useProxy2 && proxyList2.Length > 0)
                                        proxyIndex = (proxyIndex + _threadCount) % proxyList2.Length;

                                    throw new Exception("CaptchaDetected");
                                }
                            }
                            else
                            {
                                this.Invoke(new Action(() => lblStatus.Text = $"{tTag} Captcha - Changing Proxy"));

                                string proxyString = GetProxyFromDB();
                                string[] proxyList = string.IsNullOrEmpty(proxyString)
                                                    ? new string[0]
                                                    : proxyString.Split(new[] { "\n", "\r\n" }, StringSplitOptions.RemoveEmptyEntries);

                                bool useProxy = false;
                                this.Invoke(new Action(() => useProxy = chkUseProxy.Checked));
                                if (useProxy && proxyList.Length > 0)
                                    proxyIndex = (proxyIndex + _threadCount) % proxyList.Length;

                                throw new Exception("CaptchaDetected");
                            }
                        }

                        var mainColElement = localDriver.FindElements(By.CssSelector("[data-container-id ='main-col']")).FirstOrDefault();
                        if (mainColElement == null)
                        {
                            if (localDriver.PageSource.Contains("did not match any documents"))
                            {
                                this.Invoke(new Action(() =>
                                {
                                    row["Status"] = "Not Found";
                                    lblStatus.Text = $"{tTag} Not Found";
                                }));
                                currentRowIndex++;
                                continue;
                            }
                            else
                            {
                                throw new Exception("CaptchaDetected");
                            }
                        }

                        string fullPageText = "";
                        {
                            string prevText = "";
                            int stableCount = 0;
                            int maxPollSeconds = 30;
                            for (int sec = 0; sec < maxPollSeconds && !token.IsCancellationRequested; sec++)
                            {
                                try
                                {
                                    mainColElement = localDriver.FindElements(By.CssSelector("[data-container-id ='main-col']")).FirstOrDefault();
                                    string currentText = mainColElement?.Text ?? "";

                                    this.Invoke(new Action(() =>
                                        lblStatus.Text = $"{tTag} Loading... ({currentText.Length} chars, stable={stableCount}/2)"
                                    ));

                                    if (currentText.Length > 200 && currentText == prevText)
                                    {
                                        stableCount++;
                                        if (stableCount >= 2)
                                        {
                                            fullPageText = currentText;
                                            break;
                                        }
                                    }
                                    else
                                    {
                                        stableCount = 0;
                                        prevText = currentText;
                                    }
                                }
                                catch { }
                                await Task.Delay(1000, token);
                            }

                            if (string.IsNullOrEmpty(fullPageText))
                                fullPageText = mainColElement?.Text ?? "";
                        }
                        string Prompt = "Trích xuất thông tin từ text sau: \n*** " + fullPageText + " ***. \n. - DO NOT use markdown\r\n- DO NOT wrap with ```json \r\n- Lấy TẤT CẢ số điện thoại và email tìm thấy. Nếu có nhiều, mỗi số nằm trên 1 dòng (cách nhau bằng \\n)\r\n- QUAN TRỌNG: Nếu là số Fax, hãy thêm tiền tố 'Fax: ' vào trước số đó và để ở dòng bên dưới số điện thoại.\r\n.Trả về JSON theo đúng schema: { 'companies': [{ 'name': '', 'website': '','industry': '', 'address': '', 'phone': 'số1\\nFax: số2', 'email': 'mail1\\nmail2', 'linkedin': '', 'leaders': [{ 'name': '', 'position': '', 'linkedin': '', 'email': '', 'phone': '' }] }] }";

                        this.Invoke(new Action(() => lblStatus.Text = $"{tTag} AI Enrich:"));
                        string result = await CallOpenAI(Prompt); 
                        if (result == "{}") result = await CallGeminiAPI(Prompt);
                        if (result == "{}" || string.IsNullOrWhiteSpace(result))
                        {
                            string apiErrMsg = "AI API không trả về dữ liệu hợp lệ";
                            Console.WriteLine($"{tTag} [SKIP] API fail: {CompanyName}");
                            this.Invoke(new Action(() =>
                            {
                                row["Status"] = "API Error";
                                row["ErrorDetail"] = apiErrMsg;
                                lblStatus.Text = $"{tTag} API Error - Skip: {CompanyName}";
                            }));
                            currentRowIndex++;
                            continue;
                        }
                        if (!string.IsNullOrEmpty(result))
                        {
                            string cleanJson = ExtractJsonSafe(result);
                            dynamic data = JsonConvert.DeserializeObject(cleanJson);
                            if (data?.companies != null)
                            {
                                foreach (var item in data.companies)
                                {
                                    // === Nếu file upload có địa chỉ → ghi đè, không dùng address crawl ===
                                    if (!string.IsNullOrEmpty(uploadedAddress))
                                    {
                                        item.address = uploadedAddress;
                                    }
                                }
                                foreach (var item in data.companies)
                                {
                                    string rawWeb = item.website?.ToString() ?? "";
                                    if (!string.IsNullOrEmpty(rawWeb))
                                    {
                                        string web = rawWeb.ToLower().Trim();
                                        if (!web.StartsWith("http")) web = "https://" + web;
                                        try
                                        {
                                            var uri = new Uri(web);
                                            string host = uri.Host;
                                            if (host.StartsWith("www.")) host = host.Substring(4);
                                            item.website = host;
                                        }
                                        catch
                                        {
                                            item.website = rawWeb.Replace("https://", "").Replace("http://", "").Replace("www.", "").TrimEnd('/');
                                        }
                                    }
                                    else
                                    {
                                        item.website = "N/A";
                                    }
                                    string rawLinkedIn = item.linkedin?.ToString() ?? "";
                                    if (rawLinkedIn == "https://www.linkedin.com" || rawLinkedIn == "http://www.linkedin.com" || rawLinkedIn == "www.linkedin.com")
                                    {
                                        item.linkedin = "N/A";
                                    }
                                    else if (!string.IsNullOrEmpty(rawLinkedIn))
                                    {
                                         item.linkedin = CleanLinkedInUrl(rawLinkedIn);
                                    }

                                     if (item.leaders != null)
                                     {
                                         foreach (var leader in item.leaders)
                                         {
                                             string leaderLinkedIn = leader.linkedin?.ToString() ?? "";
                                             if (leaderLinkedIn == "https://www.linkedin.com" || leaderLinkedIn == "http://www.linkedin.com" || leaderLinkedIn == "www.linkedin.com")
                                             {
                                                 leader.linkedin = "N/A";
                                             }
                                             else if (!string.IsNullOrEmpty(leaderLinkedIn))
                                             {
                                                 leader.linkedin = CleanLinkedInUrl(leaderLinkedIn);
                                             }
                                         }
                                     }
                                    // === DEEP CRAWL: Lấy phone/email từ website công ty ===
                                    string curPhone = item.phone?.ToString() ?? "";
                                    string curEmail = item.email?.ToString() ?? "";
                                    string curWeb = item.website?.ToString() ?? "";
                                    string curCompanyname= item.name?.ToString() ?? "";

                                    if (!string.IsNullOrEmpty(curWeb) && curWeb != "N/A")
                                    {
                                        bool isSuccess = false;
                                        try
                                        {
                                            var (deepPhone, deepEmail, deepLinkedIn) = await EnrichContactFromWebsite(
                                                localDriver, curWeb, tTag, row, token);

                                            bool hasPhone = !string.IsNullOrEmpty(deepPhone);
                                            bool hasEmail = !string.IsNullOrEmpty(deepEmail);
                                            bool hasLinkedIn = !string.IsNullOrEmpty(deepLinkedIn);

                                            if (hasPhone)
                                            {
                                                var allPhones = (curPhone + " - " + deepPhone)
                                                    .Split(new[] { " - ", "\n" }, StringSplitOptions.RemoveEmptyEntries)
                                                    .Select(s => s.Trim())
                                                    .Where(s => !string.IsNullOrEmpty(s) && s.ToUpper() != "N/A")
                                                    .Distinct(StringComparer.OrdinalIgnoreCase);
                                                item.phone = string.Join("\n", allPhones);
                                            }
                                            if (hasEmail)
                                            {
                                                var allEmails = (curEmail + " - " + deepEmail)
                                                    .Split(new[] { " - ", "\n" }, StringSplitOptions.RemoveEmptyEntries)
                                                    .Select(s => s.Trim())
                                                    .Where(s => !string.IsNullOrEmpty(s) && s.ToUpper() != "N/A")
                                                    .Distinct(StringComparer.OrdinalIgnoreCase);
                                                item.email = string.Join("\n", allEmails);
                                            }
                                            if (hasLinkedIn)
                                            {
                                                item.linkedin = CleanLinkedInUrl(deepLinkedIn);
                                            }

                                            if (hasPhone || hasEmail || hasLinkedIn)
                                            {
                                                isSuccess = true;
                                            }
                                        }
                                        catch (Exception deepEx)
                                        {
                                            Console.WriteLine($"{tTag} [DeepCrawl Skip] {CompanyName}: {deepEx.Message}");
                                        }
                                        finally
                                        {
                                            // Đóng tab thừa sau deep crawl (website có thể mở popup/tab mới)
                                            CloseExtraTabs(localDriver);
                                        }

                                                                                // Nếu không trích xuất được email hay sđt nào từ website, cảnh báo ở ô phone
                                        if (!isSuccess)
                                        {
                                            string cleanedCurPhone = CleanPhone(curPhone);
                                            string cleanedCurEmail = CleanPhone(curEmail);

                                            bool hasAnyContact = (!string.IsNullOrEmpty(cleanedCurPhone) && cleanedCurPhone.ToUpper() != "N/A") ||
                                                                 (!string.IsNullOrEmpty(cleanedCurEmail) && cleanedCurEmail.ToUpper() != "N/A");

                                            if (!hasAnyContact)
                                            {
                                                if (string.IsNullOrEmpty(cleanedCurPhone) || cleanedCurPhone.ToUpper() == "N/A")
                                                {
                                                    item.phone = "Lỗi trích xuất email, số điện thoại";
                                                }
                                                else
                                                {
                                                    item.phone = cleanedCurPhone + "\nLỗi trích xuất email, số điện thoại";
                                                }
                                            }
                                            else
                                            {
                                                item.phone = cleanedCurPhone;
                                            }
                                        }
                                        else
                                        {
                                            item.phone = CleanPhone(item.phone?.ToString() ?? "");
                                        }
                                    }

                                      var crawlRes = await SaveCrawlResult((object)item, CompanyName);
                                     string personId = crawlRes.lastPersonId ?? "";
                                     string companyId = crawlRes.companyId;

                                    // Kiểm tra xem kết quả có thực sự có data hay chỉ có tên
                                    bool isEmpty = IsResultEmpty(item);

                                    this.Invoke(new Action(() =>
                                      {
                                          lock (_gridLock)
                                          {
                                              UpdateGridOutput(item, CompanyName, personId.ToString(), companyId, row["STT"]?.ToString() ?? "", ref sttCount);
                                          }
                                          row["Status"] = isEmpty ? "Completed but Empty" : "Completed";
                                          row["Select"] = false;
                                      }));
                                    
                                    // Luôn tìm manager trên LinkedIn /people/ (kể cả khi đã có leader LinkedIn)
                                    {
                                        try
                                        {
                                        await GetPersionFromLinkedInCompany(
                                            CompanyName,
                                            curCompanyname,
                                            item.linkedin?.ToString() ?? "",
                                            token, localDriver);
                                        }
                                        catch (Exception liEx)
                                        {
                                            Console.WriteLine($"{tTag} [LinkedIn Skip] {CompanyName}: {liEx.Message}");
                                        }
                                        finally
                                        {
                                            // Đóng tab thừa sau LinkedIn crawl
                                            CloseExtraTabs(localDriver);
                                        }
                                    }
                                }
                            }
                        }
                        this.Invoke(new Action(() => lblStatus.Text = $"{tTag} Processing"));
                        currentRowIndex++;
                        await Task.Delay(2000, token);
                        }
                        catch (Exception rowEx)
                        {
                            if (rowEx.Message == "CaptchaDetected") throw;

                            string errDetail = rowEx.Message;
                            if (rowEx.StackTrace != null)
                            {
                                var firstStack = rowEx.StackTrace.Split('\n').FirstOrDefault()?.Trim();
                                if (!string.IsNullOrEmpty(firstStack))
                                    errDetail += "\n" + firstStack;
                            }
                            Console.WriteLine($"{tTag} [Row Error] {CompanyName}: {rowEx.Message}");
                            this.Invoke(new Action(() =>
                            {
                                row["Status"] = "Error";
                                row["ErrorDetail"] = errDetail;
                                lblStatus.Text = $"{tTag} Error at {CompanyName}: {rowEx.Message}";
                            }));
                            currentRowIndex++;
                        }
                    }
                    break;
                }
                catch (OperationCanceledException)
                {
                    Console.WriteLine($"{tTag} Đã dừng theo yêu cầu.");
                    break;
                }
                catch (Exception ex)
                {
                    if (ex.Message == "CaptchaDetected")
                    {
                        Console.WriteLine($"{tTag} Captcha detected. Restarting Chrome...");
                        captchaRestart = true;
                    }
                    else
                    {
                        Console.WriteLine($"{tTag} System error: " + ex.Message);
                        this.Invoke(new Action(() => lblStatus.Text = $"{tTag} System Error: {ex.Message}"));
                        currentRowIndex++;
                    }

                    if (captchaRestart && !token.IsCancellationRequested)
                        await Task.Delay(3000, token);
                }
                finally
                {
                    if (localDriver != null)
                    {
                        try { localDriver.Quit(); localDriver.Dispose(); } catch { }
                    }
                }
            }

            this.Invoke(new Action(() => lblStatus.Text = $"{tTag} Done!"));
        }

        /// <summary>
        /// Deep crawl: Vào trang chủ website công ty, gửi nội dung cho Gemini bóc tách phone/email.
        /// </summary>
        private async Task<(string phone, string email, string linkedin)> EnrichContactFromWebsite(
            IWebDriver driver, string website, string tTag, DataRow row, CancellationToken token)
        {
            string phone = "";
            string email = "";
            string linkedin = "";

            // Chuẩn hóa base URL - Chỉ lấy tới domain, bỏ phần phía sau (ví dụ /english/index.html)
            string baseUrl = website.Trim();
            if (!baseUrl.StartsWith("http")) baseUrl = "https://" + baseUrl;
            try
            {
                var uri = new Uri(baseUrl);
                baseUrl = uri.Scheme + "://" + uri.Host;
            }
            catch { baseUrl = baseUrl.TrimEnd('/'); }

            try
            {
                this.BeginInvoke(new Action(() =>
                {
                    if (row != null) row["Status"] = "Deep Crawl Home";
                    lblStatus.Text = $"{tTag} Deep Crawl: {baseUrl}";
                }));

                driver.Navigate().GoToUrl(baseUrl);
                await Task.Delay(3000, token);

                string bodyText = "";
                try
                {
                    var body = driver.FindElement(By.TagName("body"));
                    bodyText = body?.Text ?? "";
                }
                catch { }

                if (bodyText.Length > 200) // Chỉ gọi AI nếu có nội dung
                {
                    // Cắt bớt để tiết kiệm token (tối đa 8000 ký tự)
                    string snippet = bodyText.Length > 8000 ? bodyText.Substring(0, 8000) : bodyText;

                    string aiPrompt = $@"Extract ALL phone numbers, email addresses and LinkedIn URLs from this website text. Return ONLY JSON, no markdown.
Rules: 
- Copy values EXACTLY as they appear. Do NOT guess or generate.
- If there are multiple phone numbers or emails, list them all separated by newline (\\n).
- IMPORTANT: If a number is identified as Fax, prefix it with 'Fax: ' and put it on a new line (underneath phone).
- Find the company's LinkedIn page URL if available in the text.
- If not found, return empty string.
Schema: {{""phone"": ""0123...\\nFax: 0988..."", ""email"": ""a@gmail.com\\nb@gmail.com"", ""linkedin"": ""https://www.linkedin.com/company/example""}}

Text:
{snippet}";

                    this.BeginInvoke(new Action(() => lblStatus.Text = $"{tTag} AI extract phone/email..."));
                    string aiResult = await CallGeminiAPI(aiPrompt);
                    if (!string.IsNullOrEmpty(aiResult) && aiResult != "{}")
                    {
                        try
                        {
                            string cleanAi = ExtractJsonSafe(aiResult);
                            dynamic aiData = JsonConvert.DeserializeObject(cleanAi);
                            
                            string aiPhone = aiData?.phone?.ToString() ?? "";
                            if (!string.IsNullOrEmpty(aiPhone) && aiPhone.ToUpper() != "N/A")
                            {
                                var phones = aiPhone.Split(new[] { " - ", "\n" }, StringSplitOptions.RemoveEmptyEntries)
                                                    .Select(s => s.Trim())
                                                    .Distinct(StringComparer.OrdinalIgnoreCase);
                                phone = string.Join("\n", phones);
                            }

                            string aiEmail = aiData?.email?.ToString() ?? "";
                            if (!string.IsNullOrEmpty(aiEmail) && aiEmail.ToUpper() != "N/A")
                            {
                                var emails = aiEmail.Split(new[] { " - ", "\n" }, StringSplitOptions.RemoveEmptyEntries)
                                                    .Select(s => s.Trim())
                                                    .Distinct(StringComparer.OrdinalIgnoreCase);
                                email = string.Join("\n", emails);
                            }

                            string aiLinkedIn = aiData?.linkedin?.ToString() ?? "";
                            if (!string.IsNullOrEmpty(aiLinkedIn) && aiLinkedIn.ToUpper() != "N/A")
                            {
                                linkedin = aiLinkedIn.Trim();
                            }
                        }
                        catch { }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"{tTag} [DeepCrawl Error] {baseUrl}: {ex.Message}");
            }

            return (phone, email, linkedin);
        }
        /// <summary>
        /// Giải CAPTCHA Google bằng 2Captcha API.
        /// Trả về true nếu giải thành công, false nếu thất bại.
        /// </summary>
        private static readonly HttpClient _captchaHttp = new HttpClient() { Timeout = TimeSpan.FromSeconds(120) };
        private string CAPTCHA_API_KEY
        {
            get
            {
                try
                {
                    DataTable dt = DatabaseHelper.ExecuteQuery("SELECT captcha_key FROM Config LIMIT 1");
                    if (dt.Rows.Count > 0 && dt.Rows[0]["captcha_key"] != DBNull.Value)
                        return dt.Rows[0]["captcha_key"].ToString() ?? "";
                }
                catch { }
                return "";
            }
        }

        private async Task<bool> SolveCaptchaWith2Captcha(IWebDriver drv, string tTag, DataRow row, CancellationToken token)
        {
            void SetRowStatus(string msg)
            {
                try { this.BeginInvoke(new Action(() => row["Status"] = msg)); } catch { }
            }

            try
            {
                string apiKey = CAPTCHA_API_KEY;
                if (string.IsNullOrEmpty(apiKey))
                {
                    SetRowStatus($"{tTag} 2Captcha: NO API KEY!");
                    return false;
                }

                // === BƯỚC 1: Lấy sitekey + data-s từ PageSource + cookies từ Selenium ===
                SetRowStatus($"{tTag} Extracting data-s...");
                string siteKey = "";
                string dataS = "";
                string cookieStr = "";

                try
                {
                    // Lấy cookies từ Selenium session để gửi cho 2Captcha worker
                    var seleniumCookies = drv.Manage().Cookies.AllCookies;
                    cookieStr = string.Join("; ", seleniumCookies.Select(c => $"{c.Name}={c.Value}"));

                    // Dùng PageSource - HTML đã load sẵn trong browser
                    string rawHtml = drv.PageSource ?? "";

                    var m1 = Regex.Match(rawHtml, @"data-sitekey\s*=\s*[""']([^""']+)[""']", RegexOptions.IgnoreCase);
                    if (m1.Success) siteKey = m1.Groups[1].Value.Trim();

                    var m2 = Regex.Match(rawHtml, @"data-s\s*=\s*[""']([A-Za-z0-9+/=_\-]{20,})[""']", RegexOptions.IgnoreCase);
                    if (m2.Success) dataS = m2.Groups[1].Value.Trim();

                    Console.WriteLine($"{tTag} [2Captcha] siteKey={(string.IsNullOrEmpty(siteKey) ? "MISSING" : siteKey)}");
                    Console.WriteLine($"{tTag} [2Captcha] data-s={(string.IsNullOrEmpty(dataS) ? "MISSING!" : dataS.Substring(0, Math.Min(40, dataS.Length)) + "...")}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"{tTag} [2Captcha] Extract FAILED: {ex.Message}");
                }


                // Fallback sitekey nếu không tìm thấy trong trang
                if (string.IsNullOrEmpty(siteKey))
                    siteKey = "6LfwuyUTAAAAAOAmoS0fdqijC2PbbdH4kjq62Y1b";


                SetRowStatus($"{tTag} Creating task (siteKey:{siteKey.Substring(0,8)}... data-s:{(string.IsNullOrEmpty(dataS) ? "MISSING" : "OK")})...");

                // === BƯỚC 2: Tạo task bằng API mới createTask (https://api.2captcha.com) ===
                var taskObj = new JObject
                {
                    ["type"] = "RecaptchaV2TaskProxyless",
                    ["websiteURL"] = "https://www.google.com",
                    ["websiteKey"] = siteKey,
                    ["isInvisible"] = false,
                    ["userAgent"] = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 Chrome/121.0.0.0 Safari/537.36",
                };
                if (!string.IsNullOrEmpty(dataS))
                    taskObj["recaptchaDataSValue"] = dataS;
                if (!string.IsNullOrEmpty(cookieStr))
                    taskObj["cookies"] = cookieStr;

                var createPayload = new JObject
                {
                    ["clientKey"] = apiKey,
                    ["task"] = taskObj,
                }.ToString();

                Console.WriteLine($"{tTag} [2Captcha createTask] PAYLOAD: {createPayload.Substring(0, Math.Min(300, createPayload.Length))}");

                var createResp = await _captchaHttp.PostAsync(
                    "https://api.2captcha.com/createTask",
                    new StringContent(createPayload, System.Text.Encoding.UTF8, "application/json"));
                string createJson = await createResp.Content.ReadAsStringAsync();
                Console.WriteLine($"{tTag} [2Captcha createTask] RESPONSE: {createJson}");
                var createResult = JObject.Parse(createJson);

                if (createResult["errorId"]?.Value<int>() != 0)
                {
                    string errDesc = createResult["errorDescription"]?.Value<string>() ?? createJson;
                    SetRowStatus($"{tTag} 2Captcha ERR: {errDesc}");
                    await Task.Delay(3000, token);
                    return false;
                }

                long taskId = createResult["taskId"]?.Value<long>() ?? 0;
                SetRowStatus($"{tTag} Task #{taskId} - waiting...");

                // === BƯỚC 3: Poll getTaskResult (mỗi 5s, tối đa 180s) ===
                string captchaToken = null;
                for (int i = 0; i < 36; i++)
                {
                    if (token.IsCancellationRequested) return false;
                    await Task.Delay(5000, token);

                    var pollPayload = new JObject
                    {
                        ["clientKey"] = apiKey,
                        ["taskId"] = taskId,
                    }.ToString();

                    var pollResp = await _captchaHttp.PostAsync(
                        "https://api.2captcha.com/getTaskResult",
                        new StringContent(pollPayload, System.Text.Encoding.UTF8, "application/json"));
                    string pollJson = await pollResp.Content.ReadAsStringAsync();
                    var pollResult = JObject.Parse(pollJson);

                    string status = pollResult["status"]?.Value<string>() ?? "";
                    if (status == "ready")
                    {
                        captchaToken = pollResult["solution"]?["gRecaptchaResponse"]?.Value<string>()
                                    ?? pollResult["solution"]?["token"]?.Value<string>();
                        break;
                    }

                    if (status != "processing")
                    {
                        string errDesc = pollResult["errorDescription"]?.Value<string>() ?? pollJson;
                        SetRowStatus($"{tTag} 2Captcha: {errDesc}");
                        return false;
                    }

                    SetRowStatus($"{tTag} Solving... {(i + 1) * 5}s");
                }

                if (string.IsNullOrEmpty(captchaToken))
                {
                    SetRowStatus($"{tTag} 2Captcha TIMEOUT");
                    return false;
                }

                SetRowStatus($"{tTag} Solved! Injecting...");

                // === BƯỚC 4: Inject token vào trang ===
                string safeToken = captchaToken.Replace("'", "\\'");
                ((IJavaScriptExecutor)drv).ExecuteScript($@"
                    var textarea = document.getElementById('g-recaptcha-response');
                    if (!textarea) textarea = document.querySelector('[name=""g-recaptcha-response""]');
                    if (!textarea) {{
                        textarea = document.createElement('textarea');
                        textarea.id = 'g-recaptcha-response';
                        textarea.name = 'g-recaptcha-response';
                        document.body.appendChild(textarea);
                    }}
                    textarea.style.display = 'block';
                    textarea.value = '{safeToken}';
                    textarea.innerHTML = '{safeToken}';
                ");

                // === BƯỚC 5: Trigger reCAPTCHA callback ===
                try
                {
                    ((IJavaScriptExecutor)drv).ExecuteScript($@"
                        try {{
                            if (typeof ___grecaptcha_cfg !== 'undefined') {{
                                var clients = ___grecaptcha_cfg.clients;
                                for (var cid in clients) {{
                                    var c = clients[cid];
                                    function findCallback(obj, depth) {{
                                        if (depth > 5) return null;
                                        for (var k in obj) {{
                                            if (typeof obj[k] === 'function') return obj[k];
                                            if (typeof obj[k] === 'object' && obj[k] !== null) {{
                                                var f = findCallback(obj[k], depth + 1);
                                                if (f) return f;
                                            }}
                                        }}
                                        return null;
                                    }}
                                    var cb = findCallback(c, 0);
                                    if (cb) cb('{safeToken}');
                                }}
                            }}
                        }} catch(e) {{}}
                    ");
                }
                catch { }

                // === BƯỚC 6: Submit form ===
                try
                {
                    ((IJavaScriptExecutor)drv).ExecuteScript(@"
                        var form = document.querySelector('form');
                        if (form) form.submit();
                        else {
                            var btn = document.querySelector('input[type=""submit""], button[type=""submit""]');
                            if (btn) btn.click();
                        }
                    ");
                }
                catch { }

                await Task.Delay(3000, token);
                SetRowStatus($"{tTag} CAPTCHA passed!");
                return true;
            }
            catch (Exception ex)
            {
                SetRowStatus($"{tTag} Captcha FAIL: {ex.Message.Substring(0, Math.Min(60, ex.Message.Length))}");
                return false;
            }
        }


        /// <summary>
        /// Sắp xếp cửa sổ Chrome theo lưới numHang × numCot dựa trên kích thước màn hình.
        /// threadIndex xác định vị trí trong lưới (trái→phải, trên→dưới).
        /// </summary>
        private void PositionChromeWindow(IWebDriver drv, int threadIndex)
        {
            try
            {
                // Lấy kích thước màn hình
                int screenW = Screen.PrimaryScreen.WorkingArea.Width;
                int screenH = Screen.PrimaryScreen.WorkingArea.Height;

                int cols = _numCot;  // số cột
                int rows = _numHang; // số hàng

                // Kích thước mỗi ô
                int cellW = screenW / cols;
                int cellH = screenH / rows;

                // Vị trí trong lưới (wrap nếu threadIndex >= rows*cols)
                int gridPos = threadIndex % (rows * cols);
                int row = gridPos / cols;
                int col = gridPos % cols;

                int x = col * cellW;
                int y = row * cellH;

                drv.Manage().Window.Position = new System.Drawing.Point(x, y);
                drv.Manage().Window.Size = new System.Drawing.Size(cellW, cellH);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Position] Lỗi sắp xếp Chrome #{threadIndex}: {ex.Message}");
            }
        }

        /// <summary>
        /// Kiểm tra kết quả crawl có trống hay không (chỉ có tên, còn lại N/A hoặc rỗng).
        /// </summary>
        private bool IsResultEmpty(dynamic item)
        {
            string web = item.website?.ToString() ?? "";
            string addr = item.address?.ToString() ?? "";
            string phone = item.phone?.ToString() ?? "";
            string email = item.email?.ToString() ?? "";
            string linkedin = item.linkedin?.ToString() ?? "";
            string industry = item.industry?.ToString() ?? "";

            bool IsBlank(string val) => string.IsNullOrWhiteSpace(val) || val.Trim().ToUpper() == "N/A";

            // Nếu tất cả field chính đều trống/N/A → coi như empty
            if (IsBlank(web) && IsBlank(addr) && IsBlank(phone) && IsBlank(email) && IsBlank(linkedin) && IsBlank(industry))
            {
                // Kiểm tra thêm leaders
                bool hasLeader = false;
                if (item.leaders != null)
                {
                    foreach (var leader in item.leaders)
                    {
                        string lName = leader.name?.ToString() ?? "";
                        string lPos = leader.position?.ToString() ?? "";
                        if (!IsBlank(lName) || !IsBlank(lPos))
                        {
                            hasLeader = true;
                            break;
                        }
                    }
                }
                return !hasLeader;
            }
            return false;
        }

        /// <summary>
        /// Multi-threaded wrapper cho RetryMissingWebsite
        /// </summary>
        private async Task RetryMissingWebsiteMultiThread(DataTable dtInput, CancellationToken token)
        {
            var allRows = new List<DataRow>();
            foreach (DataRow r in dtInput.Rows)
            {
                bool isSelected = true;
                if (dtInput.Columns.Contains("Select"))
                {
                    isSelected = r.Field<bool?>("Select") ?? false;
                }
                if (isSelected)
                    allRows.Add(r);
            }

            // Tạo 1 DataTable giả chứa tất cả rows để truyền vào RetryMissingWebsite
            var batches = new List<List<DataRow>>();
            for (int t = 0; t < _threadCount; t++)
                batches.Add(new List<DataRow>());
            for (int i = 0; i < allRows.Count; i++)
                batches[i % _threadCount].Add(allRows[i]);

            var tasks = new List<Task>();
            for (int t = 0; t < _threadCount; t++)
            {
                int threadIdx = t;
                var batch = batches[t];
                if (batch.Count == 0) continue;
                tasks.Add(Task.Run(async () => await RetryMissingWebsite(dtInput, threadIdx, token)));
            }

            await Task.WhenAll(tasks);
            this.Invoke(new Action(() => lblStatus.Text = "Hoàn thành tất cả!"));
        }

        /// <summary>
        /// Chạy lại lần 2 cho những công ty có website = N/A hoặc trống sau lần search đầu.
        /// </summary>
        private async Task RetryMissingWebsite(DataTable dtInput, int proxyIndex, CancellationToken token)
        {
            // 1. Lọc danh sách cần retry từ dtInput (những dòng Completed nhưng website vẫn N/A)
            var retryRows = new List<DataRow>();
            foreach (DataRow row in dtInput.Rows)
            {
                if (token.IsCancellationRequested) return;

                bool isSelected = true;
                if (dtInput.Columns.Contains("Select"))
                {
                    isSelected = row.Field<bool?>("Select") ?? false;
                }
                if (!isSelected) continue;

                string companyName = row[1].ToString();
                if (string.IsNullOrEmpty(companyName)) continue;

                // Kiểm tra trong DB: đã có record nhưng website vẫn N/A hoặc trống
                string sqlCheck = @"SELECT COUNT(*) FROM Company 
                                    WHERE TRIM(CompanyName) = TRIM($name) COLLATE NOCASE
                                    AND (Website IS NULL OR TRIM(Website) = '' OR UPPER(TRIM(Website)) = 'N/A')";
                DataTable dtCheck = DatabaseHelper.ExecuteQuery(sqlCheck, new[] { new SqliteParameter("$name", companyName) });
                bool needsRetry = dtCheck.Rows.Count > 0 && Convert.ToInt32(dtCheck.Rows[0][0]) > 0;

                if (needsRetry)
                    retryRows.Add(row);
            }

            if (retryRows.Count == 0) return; // Không có gì cần retry

            this.Invoke(new Action(() =>
                lblStatus.Text = $"[Retry] Tìm lại website cho {retryRows.Count} công ty..."));

            int sttCount = mydata.Rows.Count + 1;
            int retryIndex = 0;

            // 2. Vòng lặp retry (dùng cùng kiến trúc while-try-finally để tự restart Chrome nếu Captcha)
            while (retryIndex < retryRows.Count && !token.IsCancellationRequested)
            {
                bool captchaRestart = false;
                IWebDriver localDriver = null;
                try
                {
                    localDriver = await InitChromeDriverAsync(proxyIndex, proxyIndex);
                    _activeDrivers.Add(localDriver);

                    for (int i = retryIndex; i < retryRows.Count; i++)
                    {
                        if (token.IsCancellationRequested) break;

                        DataRow row = retryRows[i];
                        retryIndex = i;
                        string CompanyName = row[1].ToString();

                        this.Invoke(new Action(() =>
                            lblStatus.Text = $"[Retry] {i + 1}/{retryRows.Count}"));

                        try
                        {
                            // --- Tạo URL search giống MainProcess ---
                            string searchPrompt = $@"
                                Find public information about the company named '{CompanyName}'.
                                        Return ONLY the following fields in structured text:
                                        
                                        CompanyName
                                        Website (FULL URL)
                                        Address
                                        Business industry
                                        Phone
                                        Email
                                        LinkedIn (FULL URL)

                                        Executives (List if available)                                        
                                        Name
                                        Position
                                        LinkedIn (FULL URL)  

                                        Rules for Email and Phone:
                                        - Extract email and phone ONLY if they are explicitly written on:
                                          - Official website
                                          - Contact page
                                          - About page
                                          - Footer
                                        - Copy email/phone EXACTLY as displayed.
                                        - Do NOT infer, guess, or generate patterns.
                                        - If not explicitly found, return 'N/A'.

                                        Rules for LinkedIn:
                                        - Must be a FULL URL pointing to a specific company or profile.
                                        - If result is only 'https://www.linkedin.com', return 'N/A'.

                                        Rules for Executives:
                                        - Return executives ONLY if their names and roles are explicitly written
                                          on the official website or the company's LinkedIn page.
                                        - Do NOT infer executives from news, Wikipedia, or third-party sites.
                                        - If no executives are explicitly found, return 'Executives: N/A'.

                                        IMPORTANT:
                                        - Do NOT fabricate any data.
                                        - Partial information is allowed.
                                        - Accuracy is more important than completeness.                          
                                ";
                            string encodedPrompt = Uri.EscapeDataString(searchPrompt);
                            string fullUrl = $"https://www.google.com/search?q={encodedPrompt}&udm=50";
                            // Đóng tab thừa trước khi navigate
                            CloseExtraTabs(localDriver);
                            localDriver.Navigate().GoToUrl(fullUrl);

                            await Task.Delay(2000, token);
                            bool isCaptcha = true;
                            for (int poll = 0; poll < 10; poll++)
                            {
                                var els = localDriver.FindElements(By.CssSelector("[data-container-id='main-col']"));
                                if (els.Count > 0) { isCaptcha = false; break; }
                                await Task.Delay(500, token);
                            }

                            if (isCaptcha) throw new Exception("CaptchaDetected");

                            var mainColElement = localDriver.FindElements(By.CssSelector("[data-container-id ='main-col']")).FirstOrDefault();
                            if (mainColElement == null) { retryIndex++; continue; }

                            string fullPageText = mainColElement.Text;
                            if (string.IsNullOrWhiteSpace(fullPageText)) { retryIndex++; continue; }

                            string Prompt = "Trích xuất thông tin từ text sau: \n*** " + fullPageText + " ***. \n. - DO NOT use markdown\r\n- DO NOT wrap with ```json \r\n- Lấy TẤT CẢ số điện thoại và email tìm thấy. Nếu có nhiều, mỗi số nằm trên 1 dòng (cách nhau bằng \\n)\r\n- QUAN TRỌNG: Nếu là số Fax, hãy thêm tiền tố 'Fax: ' vào trước số đó và để ở dòng bên dưới số điện thoại.\r\n.Trả về JSON theo đúng schema: { 'companies': [{ 'name': '', 'website': '','industry': '', 'address': '', 'phone': 'số1\\nFax: số2', 'email': 'mail1\\nmail2', 'linkedin': '', 'leaders': [{ 'name': '', 'position': '', 'linkedin': '', 'email': '', 'phone': '' }] }] }";

                            string result = await CallOpenAI(Prompt);
                            if (result == "{}") result = await CallGeminiAPI(Prompt);
                            if (result == "{}" || string.IsNullOrWhiteSpace(result)) { retryIndex++; continue; }

                            string cleanJson = ExtractJsonSafe(result);
                            dynamic data = JsonConvert.DeserializeObject(cleanJson);

                            // Mỗi công ty chỉ xử lý 1 lần duy nhất — tìm được thì update, không thì skip sang công ty kế
                            bool foundWebsite = false;
                            if (data?.companies != null)
                            {
                                foreach (var item in data.companies)
                                {
                                    string rawWeb = item.website?.ToString() ?? "";
                                    if (!string.IsNullOrEmpty(rawWeb))
                                    {
                                        string web = rawWeb.ToLower().Trim();
                                        if (!web.StartsWith("http")) web = "https://" + web;
                                        try
                                        {
                                            var uri = new Uri(web);
                                            string host = uri.Host;
                                            if (host.StartsWith("www.")) host = host.Substring(4);
                                            item.website = host;
                                        }
                                        catch
                                        {
                                            item.website = rawWeb.Replace("https://", "").Replace("http://", "").Replace("www.", "").TrimEnd('/');
                                        }
                                    }
                                    else { item.website = "N/A"; }

                                    string newWeb = item.website?.ToString() ?? "";

                                    // Lần 2 vẫn không có website → skip sang công ty tiếp, không chạy thêm
                                    if (string.IsNullOrEmpty(newWeb) || newWeb.ToUpper() == "N/A")
                                        continue; // thử item tiếp trong companies (nếu có)

                                    // Tìm được website → lưu DB + update grid rồi dừng luôn
                                                                          var crawlRes = await SaveCrawlResult((object)item, CompanyName);
                                     string personId = crawlRes.lastPersonId ?? "";
                                     string companyId = crawlRes.companyId;
                                     foundWebsite = true;

                                     this.Invoke(new Action(() =>
                                     {
                                         var existingRows = mydata.AsEnumerable()
                                             .Where(r => r.Field<string>("CompanyName") == CompanyName)
                                             .ToList();

                                         if (existingRows.Any())
                                         {
                                             foreach (var er in existingRows)
                                             {
                                                 er["Website"] = item.website?.ToString() ?? "N/A";
                                                 er["CompanyAddress"] = string.IsNullOrEmpty(er["CompanyAddress"]?.ToString())
                                                     ? (item.address?.ToString() ?? "") : er["CompanyAddress"];
                                                 er["Industry"] = string.IsNullOrEmpty(er["Industry"]?.ToString())
                                                     ? (item.industry?.ToString() ?? "") : er["Industry"];
                                                 er["CompanyID"] = companyId;
                                             }
                                         }
                                         else
                                         {
                                             UpdateGridOutput(item, CompanyName, personId.ToString(), companyId, row["STT"]?.ToString() ?? "", ref sttCount);
                                         }

                                        row["Status"] = "Completed (Retry)";
                                         row["Select"] = false;
                                     }));

                                    break; // Đã xử lý xong 1 công ty → thoát foreach, sang công ty kế
                                }
                            }

                            // Dù tìm được hay không → luôn chuyển sang công ty kế (không retry lại)
                            retryIndex++;
                            await Task.Delay(2000, token);
                        }
                        catch (Exception rowEx)
                        {
                            if (rowEx.Message == "CaptchaDetected") throw;
                            Console.WriteLine($"[Retry Error] {CompanyName}: {rowEx.Message}");
                            retryIndex++;
                        }
                    }

                    break; // for-loop xong → thoát while
                }
                catch (OperationCanceledException) { break; }
                catch (Exception ex)
                {
                    if (ex.Message == "CaptchaDetected")
                    {
                        captchaRestart = true;
                        this.Invoke(new Action(() => lblStatus.Text = "[Retry] Captcha - Đang restart Chrome..."));
                    }
                    else
                    {
                        retryIndex++;
                    }
                    if (captchaRestart && !token.IsCancellationRequested)
                        await Task.Delay(3000, token);
                }
                finally
                {
                    if (localDriver != null)
                        try { localDriver.Quit(); localDriver.Dispose(); } catch { }
                }
            }

            this.Invoke(new Action(() => lblStatus.Text = "[Retry] Hoàn thành retry!"));
        }

        private async Task TypeLikeHuman(IWebElement element, string text, CancellationToken token)
        {
            Random rnd = new Random();
            element.Clear();
            //element.SendKeys(text);
            foreach (char c in text)
            {
                if (token.IsCancellationRequested) return;
                element.SendKeys(c.ToString());
                // Nghỉ ngẫu nhiên giữa các phím từ 50ms đến 250ms
                await Task.Delay(rnd.Next(50, 250), token);
            }
        }
        private async Task<IWebDriver> InitChromeDriverAsync(int proxyIndex, int threadIndex = 0)
        {
            // Lấy Proxy từ DB
            string fullProxyString = GetProxyFromDB();

            string currentProxy = "";
            if (!string.IsNullOrEmpty(fullProxyString))
            {
                // 2. Tách thành danh sách các proxy (theo dòng)
                string[] proxyList = fullProxyString.Split(new[] { "\n", "\r\n" }, StringSplitOptions.RemoveEmptyEntries);

                if (proxyList.Length > 0)
                {
                    // Nếu index vượt quá số lượng proxy, quay lại proxy đầu tiên (xoay vòng)
                if (proxyIndex >= proxyList.Length) proxyIndex = 0;

                    currentProxy = proxyList[proxyIndex].Trim();
                }
            }
            
            // FIX UI FREEZE: Truy cập control UI từ Background Thread phải dùng Invoke
            bool useProxy = false;
            this.Invoke(new Action(() => useProxy = chkUseProxy.Checked));

            if (!useProxy)
            {
                currentProxy = "";
                this.BeginInvoke(new Action(() =>
                {
                    lblProxy.Text = "None";
                }));
            }
            else
            {
                string[] parts = currentProxy.Split(':');
                this.BeginInvoke(new Action(() =>
                {
                    lblProxy.Text = parts[0] + ":" + parts[1];
                }));
            }
            ChromeDriverService service = ChromeDriverService.CreateDefaultService();
            //service.HelpPageView = false; // Tắt trang Help
            service.HideCommandPromptWindow = true; // Ẩn CMD

            var options = new ChromeOptions();
            // Cho phép tải hình ảnh (đè lên cấu hình lưu cũ trong Profile)
            options.AddUserProfilePreference("profile.default_content_setting_values.images", 1);
            // Vô hiệu hóa thông báo để tránh làm phiền
            options.AddUserProfilePreference("profile.default_content_setting_values.notifications", 2);
            string[] userAgents = {
                "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/121.0.0.0 Safari/537.36",
                "Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36",
                "Mozilla/5.0 (X11; Linux x86_64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/121.0.0.0 Safari/537.36"
            };
            var random = new Random();
            string selectedUA = userAgents[random.Next(userAgents.Length)];
            options.AddArgument($"--user-agent={selectedUA}");
            
            string baseDir = AppDomain.CurrentDomain.BaseDirectory;

            // Cấu hình đường dẫn đến Chrome (nằm trong thư mục app)
            options.BinaryLocation = Path.Combine(baseDir, "Chrome_v121", "chrome", "chrome.exe");

            // Cấu hình đường dẫn đến Profiles
            string profilePath = Path.Combine(baseDir, "Chrome_v121", "Profiles", $"Thread_{threadIndex}_Proxy_{proxyIndex}");

            // Thêm vào argument của Chrome
            options.AddArgument($"--user-data-dir={profilePath}");

            // Cấu hình Proxy nếu có
            if (!string.IsNullOrEmpty(currentProxy))
            {
                // Extension Proxy Auto Auth
                options.AddExtension("Proxy Auto Auth.crx");
                string[] parts = currentProxy.Split(':');
                if (parts.Length >= 2) options.AddArgument($"--proxy-server=http://{parts[0]}:{parts[1]}");
            }

            options.AddArgument("--disable-blink-features=AutomationControlled");
            options.AddExcludedArgument("enable-automation");
            options.AddArgument("--disable-infobars");
            options.AddArgument("--no-sandbox");
            options.AddArgument("--disable-dev-shm-usage");
            options.AddArgument("--disable-application-cache");

            // UPDATE STATUS
            this.BeginInvoke(new Action(() => lblStatus.Text = $"Opening Chrome T{threadIndex+1}..."));

            var driver = new ChromeDriver(service, options, TimeSpan.FromSeconds(120));
            driver.Manage().Timeouts().PageLoad = TimeSpan.FromSeconds(60);
            
            try
            {
                // Tối ưu Xóa dữ liệu: Dùng lệnh CDP hoặc JS thay vì giao diện
                try 
                {
                    this.BeginInvoke(new Action(() => lblStatus.Text = $"Clearing Data T{threadIndex+1}..."));
                    driver.Manage().Cookies.DeleteAllCookies();
                    ((IJavaScriptExecutor)driver).ExecuteScript("window.localStorage.clear();");
                    ((IJavaScriptExecutor)driver).ExecuteScript("window.sessionStorage.clear();");
                    
                    // RESTORED & IMPROVED: Dùng JS Shadow DOM để click nút "Clear data" chính xác 100%
                    driver.Navigate().GoToUrl("chrome://settings/clearBrowserData");
                    
                    // Dùng WebDriverWait chờ nút xuất hiện trong Shadow DOM
                    WebDriverWait waitClear = new WebDriverWait(driver, TimeSpan.FromSeconds(5));
                    try
                    {
                        waitClear.Until(d => ((IJavaScriptExecutor)d).ExecuteScript(@"
                            try {
                                const settingsUi = document.querySelector('settings-ui');
                                const settingsMain = settingsUi.shadowRoot.querySelector('settings-main');
                                const settingsBasicPage = settingsMain.shadowRoot.querySelector('settings-basic-page');
                                const settingsSection = settingsBasicPage.shadowRoot.querySelector('settings-section > settings-privacy-page');
                                const clearDialog = settingsSection.shadowRoot.querySelector('settings-clear-browsing-data-dialog');
                                const clearButton = clearDialog.shadowRoot.querySelector('#clearBrowsingDataConfirm');
                                if (clearButton) {
                                    clearButton.click();
                                    return true;
                                }
                            } catch(e) { return false; }
                            return false;
                        "));
                    }
                    catch 
                    {
                        // Nếu JS fail thì fallback về Enter (phương án dự phòng)
                        try {
                             var action = new OpenQA.Selenium.Interactions.Actions(driver);
                             action.SendKeys(OpenQA.Selenium.Keys.Enter).Build().Perform();
                        } catch {}
                    }
                }
                catch {}

                // Đóng các tab thừa (Settings, auto-opened extension tabs), giữ lại 1 tab
                try
                {
                    while (driver.WindowHandles.Count > 1)
                    {
                        driver.SwitchTo().Window(driver.WindowHandles.Last());
                        driver.Close();
                    }
                    driver.SwitchTo().Window(driver.WindowHandles.First());
                }
                catch { }

                if (currentProxy != "")
                {
                    this.BeginInvoke(new Action(() => lblStatus.Text = $"Configuring Proxy T{threadIndex+1}..."));
                    string[] parts = currentProxy.Split(':');
                    try
                    {
                        // Navigate đến Options của Extension
                        driver.Navigate().GoToUrl("chrome-extension://ggmdpepbjljkkkdaklfihhngmmgmpggp/options.html");
                        
                        // Dùng WebDriverWait thay vì Sleep cứng
                        WebDriverWait wait = new WebDriverWait(driver, TimeSpan.FromSeconds(5));
                        
                        // Chờ Element xuất hiện và tương tác ngay
                        var txtLogin = wait.Until(d => d.FindElement(By.Id("login")));
                        txtLogin.Clear();
                        txtLogin.SendKeys(parts[2]);

                        var txtPassword = driver.FindElement(By.Id("password"));
                        txtPassword.Clear();
                        txtPassword.SendKeys(parts[3]);

                        var txtRetry = driver.FindElement(By.Id("retry"));
                        txtRetry.Clear();
                        txtRetry.SendKeys("2");

                        driver.FindElement(By.Id("save")).Click();
                        await Task.Delay(1000); // Chờ Save hoàn thành

                        // Navigate về trang trắng (không đóng tab vì chỉ còn 1 tab)
                        driver.Navigate().GoToUrl("about:blank");
                    }
                    catch (Exception ex) 
                    {
                        Console.WriteLine("Lỗi config extension: " + ex.Message);
                    }
                }
                // Chống phát hiện Selenium
                ((IJavaScriptExecutor)driver).ExecuteScript("Object.defineProperty(navigator, 'webdriver', {get: () => undefined})");
            }
            catch (Exception)
            {
                try { driver.Quit(); driver.Dispose(); } catch {}
                throw;
            }
            
            return driver;
        }

        /// <summary>
        /// Đóng tất cả tab thừa, chỉ giữ lại tab đầu tiên.
        /// Gọi sau mỗi lần deep crawl website hoặc LinkedIn để tránh tích tụ tab.
        /// </summary>
        private void CloseExtraTabs(IWebDriver drv)
        {
            try
            {
                while (drv.WindowHandles.Count > 1)
                {
                    drv.SwitchTo().Window(drv.WindowHandles.Last());
                    drv.Close();
                }
                drv.SwitchTo().Window(drv.WindowHandles.First());
            }
            catch { }
        }
        
        private void LoadSpecificCompanyToGrid(string companyName, string inputSTT, string dbCompId = null)
        {
            // Kiểm tra đã load công ty này chưa
            bool alreadyLoaded = mydata.AsEnumerable().Any(x =>
                string.Equals((x.Field<string>("CompanyName") ?? "").Trim(), companyName.Trim(), StringComparison.OrdinalIgnoreCase) &&
                (x.Field<string>("RowType") ?? "") == "Company");
            if (alreadyLoaded) return;

            string sql;
            DataTable dtSaved;
            if (!string.IsNullOrEmpty(dbCompId))
            {
                sql = @"
            SELECT c.ID as CompanyID, c.CompanyName, c.Address, c.Industry, c.Phone, c.Website, c.Email, c.Linkedin as CoLink,
                   p.FullName, p.Position, p.Linkedin as PeLink, p.Email as PeEmail, p.Phone as PePhone
            FROM Company c
            LEFT JOIN Person p ON c.ID = p.CompanyID
            WHERE c.ID = $id";
                dtSaved = DatabaseHelper.ExecuteQuery(sql, new[] { new SqliteParameter("$id", dbCompId) });
            }
            else
            {
                sql = @"
            SELECT c.ID as CompanyID, c.CompanyName, c.Address, c.Industry, c.Phone, c.Website, c.Email, c.Linkedin as CoLink,
                   p.FullName, p.Position, p.Linkedin as PeLink, p.Email as PeEmail, p.Phone as PePhone
            FROM Company c
            LEFT JOIN Person p ON c.ID = p.CompanyID
            WHERE TRIM(c.CompanyName) = TRIM($name) COLLATE NOCASE";
                dtSaved = DatabaseHelper.ExecuteQuery(sql, new[] { new SqliteParameter("$name", companyName) });
            }

            if (dtSaved.Rows.Count == 0)
            {
                // Vẫn hiện 1 row trên output grid dù DB trống
                DataRow drEmpty = mydata.NewRow();
                drEmpty["STT"] = inputSTT;
                drEmpty["CompanyName"] = companyName;
                drEmpty["RowType"] = "Company";
                mydata.Rows.Add(drEmpty);
                return;
            }

            // ROW CHA: Công ty (lấy từ row đầu tiên)
            DataRow first = dtSaved.Rows[0];
            DataRow drCompany = mydata.NewRow();
            drCompany["STT"] = inputSTT;
            drCompany["CompanyID"] = first["CompanyID"];
            drCompany["CompanyName"] = first["CompanyName"];
            drCompany["CompanyAddress"] = first["Address"];
            drCompany["Industry"] = first["Industry"];
            drCompany["PhoneCo"] = CleanPhone(first["Phone"]?.ToString() ?? "");
            drCompany["Website"] = first["Website"];
            drCompany["EmailCo"] = first["Email"];
            drCompany["LinkedInCo"] = first["CoLink"];
            drCompany["RowType"] = "Company";
            mydata.Rows.Add(drCompany);

            // ROW CON: Nhân sự
            int personIdx = 0;
            foreach (DataRow r in dtSaved.Rows)
            {
                string fullName = r["FullName"]?.ToString() ?? "";
                if (string.IsNullOrEmpty(fullName)) continue;

                personIdx++;
                DataRow drPerson = mydata.NewRow();
                drPerson["STT"] = "  ├ " + personIdx;
                drPerson["CompanyName"] = "";
                drPerson["FullNamePe"] = fullName;
                drPerson["PositionPe"] = r["Position"];
                drPerson["LinkedInPe"] = r["PeLink"];
                drPerson["EmailPe"] = r["PeEmail"];
                drPerson["PhonePe"] = r["PePhone"];
                drPerson["RowType"] = "Person";
                mydata.Rows.Add(drPerson);
            }
        }
        private int totalStaffCrawl;
        /// <summary>
        /// Kiểm tra giá trị có phải N/A hoặc trống không
        /// </summary>
        private bool IsFieldBlank(string val)
        {
            return string.IsNullOrWhiteSpace(val) || val.Trim().ToUpper() == "N/A";
        }

        /// <summary>
        /// Merge giá trị mới vào cell: chỉ cập nhật nếu cell hiện tại đang trống/N/A
        /// </summary>
        private void MergeField(DataRow row, string columnName, string newValue)
        {
            string currentVal = row[columnName]?.ToString() ?? "";
            if (IsFieldBlank(currentVal) && !IsFieldBlank(newValue))
            {
                row[columnName] = newValue;
            }
        }

        private void UpdateGridOutput(dynamic item, string CompanyName, string personId, string companyId, string inputSTT, ref int sttCount)
        {
            // 1. Lấy thông tin công ty từ item (JSON AI trả về)
            string CompanyAddress = item.address ?? "";
            string PhoneCo = item.phone ?? "";
            string Website = item.website ?? "";
            string EmailCo = item.email ?? "";
            string LinkedInCo = item.linkedin ?? "";
            string Industry = item.industry ?? "";

            // --- Kiểm tra xem công ty đã có trong grid chưa ---
            DataRow existingCompanyRow = null;
            int existingCompanyIndex = -1;
            for (int ri = 0; ri < mydata.Rows.Count; ri++)
            {
                string cn = mydata.Rows[ri]["CompanyName"]?.ToString() ?? "";
                string rt = mydata.Rows[ri]["RowType"]?.ToString() ?? "";
                if (rt == "Company" && string.Equals(cn.Trim(), CompanyName.Trim(), StringComparison.OrdinalIgnoreCase))
                {
                    existingCompanyRow = mydata.Rows[ri];
                    existingCompanyIndex = ri;
                    break;
                }
            }

            if (existingCompanyRow != null)
            {
                // === OVERWRITE MODE: Cập nhật trực tiếp các field ===
                existingCompanyRow["CompanyAddress"] = CompanyAddress;
                existingCompanyRow["Industry"] = Industry;
                existingCompanyRow["PhoneCo"] = PhoneCo;
                existingCompanyRow["Website"] = Website;
                existingCompanyRow["EmailCo"] = EmailCo;
                existingCompanyRow["LinkedInCo"] = LinkedInCo;
                if (personId != "")
                    existingCompanyRow["personId"] = personId.ToString();
                if (!string.IsNullOrEmpty(companyId))
                    existingCompanyRow["CompanyID"] = companyId;

                // --- Xóa tất cả các dòng Person cũ của công ty này ---
                int nextIndex = existingCompanyIndex + 1;
                while (nextIndex < mydata.Rows.Count && mydata.Rows[nextIndex]["RowType"]?.ToString() == "Person")
                {
                    mydata.Rows.RemoveAt(nextIndex);
                }

                // --- Thêm mới các Person vừa crawl được ---
                if (item.leaders != null && item.leaders.Count > 0)
                {
                    int insertPos = existingCompanyIndex + 1;
                    for (int i = 0; i < item.leaders.Count; i++)
                    {
                        var leader = item.leaders[i];
                        string leaderName = leader.name?.ToString() ?? "";
                        if (string.IsNullOrWhiteSpace(leaderName)) continue;

                        DataRow dr = mydata.NewRow();
                        dr["STT"] = "  ├ " + (i + 1);
                        dr["CompanyName"] = "";
                        dr["FullNamePe"] = leaderName;
                        dr["PositionPe"] = leader.position ?? "";
                        dr["LinkedInPe"] = leader.linkedin ?? "";
                        dr["EmailPe"] = leader.email ?? "";
                        dr["PhonePe"] = leader.phone ?? "";
                        dr["RowType"] = "Person";
                        
                        mydata.Rows.InsertAt(dr, insertPos);
                        insertPos++;
                    }
                }
            }
            else
            {
                // === INSERT MODE: Công ty mới, thêm dòng mới như bình thường ===
                DataRow drCompany = mydata.NewRow();
                drCompany["STT"] = inputSTT;
                drCompany["CompanyName"] = CompanyName;
                drCompany["CompanyAddress"] = CompanyAddress;
                drCompany["Industry"] = Industry;
                drCompany["PhoneCo"] = PhoneCo;
                drCompany["Website"] = Website;
                drCompany["EmailCo"] = EmailCo;
                drCompany["LinkedInCo"] = LinkedInCo;
                if (personId != "") drCompany["personId"] = personId.ToString();
                if (!string.IsNullOrEmpty(companyId)) drCompany["CompanyID"] = companyId;
                drCompany["RowType"] = "Company";
                mydata.Rows.Add(drCompany);

                if (item.leaders != null && item.leaders.Count > 0)
                {
                    for (int i = 0; i < item.leaders.Count; i++)
                    {
                        var leader = item.leaders[i];

                        DataRow dr = mydata.NewRow();
                        dr["STT"] = "  ├ " + (i + 1);
                        dr["CompanyName"] = "";
                        dr["FullNamePe"] = leader.name ?? "";
                        dr["PositionPe"] = leader.position ?? "";
                        dr["LinkedInPe"] = leader.linkedin ?? "";
                        dr["EmailPe"] = leader.email ?? "";
                        dr["PhonePe"] = leader.phone ?? "";
                        dr["RowType"] = "Person";
                        mydata.Rows.Add(dr);
                    }
                }
            }

            totalStaffCrawl = mydata.Rows.Count + 1;
            this.Invoke(new Action(() => {
                lblOutput.Text = $"OUTPUT: {totalStaffCrawl}";

                if (!_isRerunMode && dgvOutput.Rows.Count > 0)
                {
                    int lastIndex = dgvOutput.Rows.Count - 1;
                    dgvOutput.FirstDisplayedScrollingRowIndex = lastIndex;

                    dgvOutput.ClearSelection();
                    dgvOutput.Rows[lastIndex].Selected = true;
                }
            }));
        }
        private void RenumberSTT()
        {
            this.Invoke(new Action(() => {
                for (int i = 0; i < mydata.Rows.Count; i++)
                {
                    mydata.Rows[i]["STT"] = i + 1;
                }
            }));
        }
        public Task SaveCrawlResult1(dynamic company,string CompanyName)
        {
            try
            {
                // 1. Xử lý Website
                string companyname = CompanyName;// company?.name?.ToString() ?? "";
                if(companyname=="") return Task.CompletedTask;
                string website = company.website?.ToString() ?? "";

                // DÙNG BATCH TRANSACTION ĐỂ TỐI ƯU TỐC ĐỘ LƯU
                DatabaseHelper.ExecuteBatch((conn, trans) =>
                {
                    // 2. Kiểm tra Company tồn tại
                    // Lưu ý: Khi dùng Transaction, tất cả Command phải dùng chung "conn" và "trans"
                    string companyId = "";
                    string checkCompSql = "SELECT ID FROM Company WHERE Website = $web";
                    using (var cmdCheck = new SqliteCommand(checkCompSql, conn, trans))
                    {
                        cmdCheck.Parameters.Add(new SqliteParameter("$web", website));
                        using (var reader = cmdCheck.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                companyId = reader["ID"].ToString();
                            }
                        }
                    }

                    string now = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                    string sqlCompany;

                    if (string.IsNullOrEmpty(companyId))
                    {
                        companyId = Guid.NewGuid().ToString();
                        sqlCompany = @"INSERT INTO Company (ID, CompanyName, Address, Website, Industry, Email, Linkedin, Phone, LastUpdate) 
                               Values ($id, $name, $addr, $web, $industry, $mail, $link, $phone, $date)";
                    }
                                                                                                sqlCompany = @"UPDATE Company 
                               SET CompanyName = CASE WHEN ($name IS NOT NULL AND TRIM($name) <> '' AND UPPER(TRIM($name)) <> 'N/A') THEN $name ELSE CompanyName END,
                                   Address = CASE WHEN ($addr IS NOT NULL AND TRIM($addr) <> '' AND UPPER(TRIM($addr)) <> 'N/A') THEN $addr ELSE Address END,
                                   Website = CASE WHEN ($web IS NOT NULL AND TRIM($web) <> '' AND UPPER(TRIM($web)) <> 'N/A') THEN $web ELSE Website END,
                                   Industry = CASE WHEN ($industry IS NOT NULL AND TRIM($industry) <> '' AND UPPER(TRIM($industry)) <> 'N/A') THEN $industry ELSE Industry END,
                                   Email = CASE WHEN ($mail IS NOT NULL AND TRIM($mail) <> '' AND UPPER(TRIM($mail)) <> 'N/A') THEN $mail ELSE Email END,
                                   Linkedin = CASE WHEN ($link IS NOT NULL AND TRIM($link) <> '' AND UPPER(TRIM($link)) <> 'N/A') THEN $link ELSE Linkedin END,
                                   Phone = CASE WHEN ($phone IS NOT NULL AND TRIM($phone) <> '' AND UPPER(TRIM($phone)) <> 'N/A') THEN $phone ELSE Phone END,
                                   LastUpdate = $date
                               WHERE ID=$id";

                    using (var cmdComp = new SqliteCommand(sqlCompany, conn, trans))
                    {
                        cmdComp.Parameters.AddRange(new[] {
                            new SqliteParameter("$id", companyId),
                            new SqliteParameter("$name", string.IsNullOrEmpty(companyname) ? (company.name?.ToString() ?? "") : companyname),
                            new SqliteParameter("$addr", company.address?.ToString() ?? ""),
                            new SqliteParameter("$web", website),
                            new SqliteParameter("$industry", company.industry?.ToString() ?? ""),
                            new SqliteParameter("$mail", company.email?.ToString() ?? ""),
                                                        new SqliteParameter("$link", company.linkedin?.ToString() ?? ""),
                            new SqliteParameter("$phone", CleanPhone(company.phone?.ToString() ?? "")),
                            new SqliteParameter("$date", now)
                        });
                        cmdComp.ExecuteNonQuery();
                    }

                    // 3. XỬ LÝ PERSON
                    if (company.leaders != null)
                    {
                        foreach (var leader in company.leaders)
                        {
                            string fullName = leader.name?.ToString() ?? "";
                            if (string.IsNullOrEmpty(fullName)) continue;

                            // Kiểm tra Person tồn tại
                            bool personExists = false;
                            string checkPersSql = "SELECT 1 FROM Person WHERE CompanyID = $cid AND FullName = $name";
                            using (var cmdCheckP = new SqliteCommand(checkPersSql, conn, trans))
                            {
                                cmdCheckP.Parameters.Add(new SqliteParameter("$cid", companyId));
                                cmdCheckP.Parameters.Add(new SqliteParameter("$name", fullName));
                                using (var reader = cmdCheckP.ExecuteReader())
                                {
                                    if (reader.Read()) personExists = true;
                                }
                            }

                            string sqlPers;
                            if (personExists)
                            {
                                                                                                                                sqlPers = @"UPDATE Person 
                                             SET Position = CASE WHEN ($pos IS NOT NULL AND TRIM($pos) <> '' AND UPPER(TRIM($pos)) <> 'N/A') THEN $pos ELSE Position END,
                                                 Linkedin = CASE WHEN ($link IS NOT NULL AND TRIM($link) <> '' AND UPPER(TRIM($link)) <> 'N/A') THEN $link ELSE Linkedin END,
                                                 Email = CASE WHEN ($mail IS NOT NULL AND TRIM($mail) <> '' AND UPPER(TRIM($mail)) <> 'N/A') THEN $mail ELSE Email END,
                                                 Phone = CASE WHEN ($phone IS NOT NULL AND TRIM($phone) <> '' AND UPPER(TRIM($phone)) <> 'N/A') THEN $phone ELSE Phone END,
                                                 LastUpdate=$date 
                                             WHERE CompanyID=$cid AND FullName=$name";
                            }
                            else
                            {
                                sqlPers = @"INSERT INTO Person (CompanyID, FullName, Position, Linkedin, Email, Phone, LastUpdate) 
                                            VALUES ($cid, $name, $pos, $link, $mail, $phone, $date)";
                            }

                            using (var cmdPers = new SqliteCommand(sqlPers, conn, trans))
                            {
                                cmdPers.Parameters.AddRange(new[] {
                                    new SqliteParameter("$cid", companyId),
                                    new SqliteParameter("$name", fullName),
                                    new SqliteParameter("$pos", leader.position?.ToString() ?? ""),
                                    new SqliteParameter("$link", leader.linkedin?.ToString() ?? ""),
                                    new SqliteParameter("$mail", leader.email?.ToString() ?? ""),
                                    new SqliteParameter("$phone", leader.phone?.ToString() ?? ""),
                                    new SqliteParameter("$date", now)
                                });
                                cmdPers.ExecuteNonQuery();
                            }
                            long newId = (long)new SqliteCommand("SELECT last_insert_rowid()", conn, trans).ExecuteScalar();
                        }
                    }
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Lỗi SaveCrawlResult: " + ex.Message);
            }

            return Task.CompletedTask;
        }

        private string CleanLinkedInUrl(string url)
        {
            if (string.IsNullOrEmpty(url)) return url;
            url = url.Trim();
            url = System.Text.RegularExpressions.Regex.Replace(url, @"(?<=://)[a-z]{2}\.linkedin\.com", "linkedin.com", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            url = System.Text.RegularExpressions.Regex.Replace(url, @"^([a-z]{2})\.linkedin\.com", "linkedin.com", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            return url;
        }

        private string CleanPhone(string phone)
        {
            if (string.IsNullOrEmpty(phone)) return "";
            var lines = phone.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries)
                             .Select(l => l.Trim())
                             .Where(l => !string.Equals(l, "Lỗi trích xuất email, số điện thoại", StringComparison.OrdinalIgnoreCase) && !string.IsNullOrEmpty(l));
            return string.Join("\n", lines);
        }

        private void UncheckOutputGrid()
        {
            if (dgvOutput.InvokeRequired)
            {
                dgvOutput.Invoke(new Action(UncheckOutputGrid));
                return;
            }
            foreach (DataGridViewRow row in dgvOutput.Rows)
            {
                if (row.Cells["chkSelect"].Value is bool b && b)
                {
                    row.Cells["chkSelect"].Value = false;
                }
            }
        }

        public Task<(string lastPersonId, string companyId)> SaveCrawlResult(dynamic company, string CompanyName)
        {
            string lastPersonId = "";
            string companyId = "";

            try
            {
                string companyname = CompanyName;
                if (string.IsNullOrEmpty(companyname))
                    return Task.FromResult<(string lastPersonId, string companyId)>((null, ""));

                // Clean LinkedIn URL
                string rawLinkedIn = company.linkedin?.ToString() ?? "";
                if (rawLinkedIn == "https://www.linkedin.com" || rawLinkedIn == "http://www.linkedin.com" || rawLinkedIn == "www.linkedin.com")
                {
                    company.linkedin = "N/A";
                }
                else if (!string.IsNullOrEmpty(rawLinkedIn))
                {
                    company.linkedin = CleanLinkedInUrl(rawLinkedIn);
                }

                if (company.leaders != null)
                {
                    foreach (var leader in company.leaders)
                    {
                        string leaderLinkedIn = leader.linkedin?.ToString() ?? "";
                        if (leaderLinkedIn == "https://www.linkedin.com" || leaderLinkedIn == "http://www.linkedin.com" || leaderLinkedIn == "www.linkedin.com")
                        {
                            leader.linkedin = "N/A";
                        }
                        else if (!string.IsNullOrEmpty(leaderLinkedIn))
                        {
                            leader.linkedin = CleanLinkedInUrl(leaderLinkedIn);
                        }
                    }
                }

                string website = company.website?.ToString() ?? "";
                string outCompanyId = "";

                DatabaseHelper.ExecuteBatch((conn, trans) =>
                {
                    // 1. Check Company - Check theo CompanyName là chắc chắn nhất
                    string checkCompSql = "SELECT ID FROM Company WHERE CompanyName = $name COLLATE NOCASE LIMIT 1";
                    using (var cmdCheck = new SqliteCommand(checkCompSql, conn, trans))
                    {
                        cmdCheck.Parameters.AddWithValue("$name", companyname);
                        var result = cmdCheck.ExecuteScalar();
                        if (result != null)
                            outCompanyId = result.ToString();
                    }

                    string now = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                    string sqlCompany;

                    if (string.IsNullOrEmpty(outCompanyId))
                    {
                        outCompanyId = Guid.NewGuid().ToString();
                        sqlCompany = @"INSERT INTO Company 
                    (ID, CompanyName, Address, Website, Industry, Email, Linkedin, Phone, LastUpdate) 
                    VALUES ($id, $name, $addr, $web, $industry, $mail, $link, $phone, $date)";
                    }
                    else
                    {
                        sqlCompany = @"UPDATE Company 
                    SET CompanyName = CASE WHEN ($name IS NOT NULL AND TRIM($name) <> '' AND UPPER(TRIM($name)) <> 'N/A') THEN $name ELSE CompanyName END,
                        Address = CASE WHEN ($addr IS NOT NULL AND TRIM($addr) <> '' AND UPPER(TRIM($addr)) <> 'N/A') THEN $addr ELSE Address END,
                        Website = CASE WHEN ($web IS NOT NULL AND TRIM($web) <> '' AND UPPER(TRIM($web)) <> 'N/A') THEN $web ELSE Website END,
                        Industry = CASE WHEN ($industry IS NOT NULL AND TRIM($industry) <> '' AND UPPER(TRIM($industry)) <> 'N/A') THEN $industry ELSE Industry END,
                        Email = CASE WHEN ($mail IS NOT NULL AND TRIM($mail) <> '' AND UPPER(TRIM($mail)) <> 'N/A') THEN $mail ELSE Email END,
                        Linkedin = CASE WHEN ($link IS NOT NULL AND TRIM($link) <> '' AND UPPER(TRIM($link)) <> 'N/A') THEN $link ELSE Linkedin END,
                        Phone = CASE WHEN ($phone IS NOT NULL AND TRIM($phone) <> '' AND UPPER(TRIM($phone)) <> 'N/A') THEN $phone ELSE Phone END,
                        LastUpdate=$date 
                    WHERE ID=$id";
                    }

                    using (var cmdComp = new SqliteCommand(sqlCompany, conn, trans))
                    {
                        cmdComp.Parameters.AddWithValue("$id", outCompanyId);
                        cmdComp.Parameters.AddWithValue("$name", string.IsNullOrEmpty(companyname) ? (company.name?.ToString() ?? "") : companyname);
                        cmdComp.Parameters.AddWithValue("$addr", company.address?.ToString() ?? "");
                        cmdComp.Parameters.AddWithValue("$web", website);
                        cmdComp.Parameters.AddWithValue("$industry", company.industry?.ToString() ?? "");
                        cmdComp.Parameters.AddWithValue("$mail", company.email?.ToString() ?? "");
                        cmdComp.Parameters.AddWithValue("$link", company.linkedin?.ToString() ?? "");
                        cmdComp.Parameters.AddWithValue("$phone", CleanPhone(company.phone?.ToString() ?? ""));
                        cmdComp.Parameters.AddWithValue("$date", now);

                        cmdComp.ExecuteNonQuery();
                    }

                    // 2. PERSON
                    if (company.leaders != null)
                    {
                        foreach (var leader in company.leaders)
                        {
                            string fullName = leader.name?.ToString() ?? "";
                            if (string.IsNullOrEmpty(fullName)) continue;

                            string personId = "";

                            // 🔥 Lấy ID nếu đã tồn tại
                            string checkPersSql = "SELECT ID FROM Person WHERE CompanyID = $cid AND FullName = $name";
                            using (var cmdCheckP = new SqliteCommand(checkPersSql, conn, trans))
                            {
                                cmdCheckP.Parameters.AddWithValue("$cid", outCompanyId);
                                cmdCheckP.Parameters.AddWithValue("$name", fullName);

                                var result = cmdCheckP.ExecuteScalar();
                                if (result != null)
                                    personId = result.ToString();
                            }

                            if (!string.IsNullOrEmpty(personId))
                            {
                                string sqlUpdate = @"UPDATE Person 
                            SET Position = CASE WHEN ($pos IS NOT NULL AND TRIM($pos) <> '' AND UPPER(TRIM($pos)) <> 'N/A') THEN $pos ELSE Position END,
                                Linkedin = CASE WHEN ($link IS NOT NULL AND TRIM($link) <> '' AND UPPER(TRIM($link)) <> 'N/A') THEN $link ELSE Linkedin END,
                                Email = CASE WHEN ($mail IS NOT NULL AND TRIM($mail) <> '' AND UPPER(TRIM($mail)) <> 'N/A') THEN $mail ELSE Email END,
                                Phone = CASE WHEN ($phone IS NOT NULL AND TRIM($phone) <> '' AND UPPER(TRIM($phone)) <> 'N/A') THEN $phone ELSE Phone END,
                                LastUpdate=$date 
                            WHERE ID=$id";

                                using (var cmd = new SqliteCommand(sqlUpdate, conn, trans))
                                {
                                    cmd.Parameters.AddWithValue("$id", personId);
                                    cmd.Parameters.AddWithValue("$pos", leader.position?.ToString() ?? "");
                                    cmd.Parameters.AddWithValue("$link", leader.linkedin?.ToString() ?? "");
                                    cmd.Parameters.AddWithValue("$mail", leader.email?.ToString() ?? "");
                                    cmd.Parameters.AddWithValue("$phone", leader.phone?.ToString() ?? "");
                                    cmd.Parameters.AddWithValue("$date", now);

                                    cmd.ExecuteNonQuery();
                                }
                            }
                            else
                            {
                                personId = Guid.NewGuid().ToString();
                                string sqlInsert = @"
                            INSERT INTO Person (ID, CompanyID, FullName, Position, Linkedin, Email, Phone, LastUpdate) 
                            VALUES ($id, $cid, $name, $pos, $link, $mail, $phone, $date)";

                                using (var cmd = new SqliteCommand(sqlInsert, conn, trans))
                                {
                                    cmd.Parameters.AddWithValue("$id", personId);
                                    cmd.Parameters.AddWithValue("$cid", outCompanyId);
                                    cmd.Parameters.AddWithValue("$name", fullName);
                                    cmd.Parameters.AddWithValue("$pos", leader.position?.ToString() ?? "");
                                    cmd.Parameters.AddWithValue("$link", leader.linkedin?.ToString() ?? "");
                                    cmd.Parameters.AddWithValue("$mail", leader.email?.ToString() ?? "");
                                    cmd.Parameters.AddWithValue("$phone", leader.phone?.ToString() ?? "");
                                    cmd.Parameters.AddWithValue("$date", now);

                                    cmd.ExecuteNonQuery();
                                }
                            }

                            // lưu ID cuối cùng (hoặc bạn có thể list nếu cần nhiều)
                            lastPersonId = personId;
                        }
                    }
                });
                companyId = outCompanyId;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Lỗi SaveCrawlResult: " + ex.Message);
            }

            return Task.FromResult<(string lastPersonId, string companyId)>((lastPersonId, companyId));
        }
        private async void btnStop_Click(object sender, EventArgs e)
        {
            btnStop.Enabled = false;
            lblStatus.Text = "Stopping...";

            if (_cts != null) _cts.Cancel();

            // Kill process ngay (nhanh, không block)
            KillAllChromeProcesses();

            // Quit drivers trên background thread để không đứng form
            await Task.Run(() =>
            {
                while (_activeDrivers.TryTake(out var d))
                {
                    try { d.Quit(); d.Dispose(); } catch { }
                }
                try { if (driver != null) { driver.Quit(); driver.Dispose(); driver = null; } } catch { }
            });

            SortOutputGridBySTT();
            btnStart.Enabled = true;
            lblStatus.Text = "Stopped";
        }
        private void SortOutputGridBySTT()
        {
            try
            {
                // Group rows: mỗi Company row + các Person row theo sau
                var groups = new List<List<DataRow>>();
                List<DataRow> current = null;

                foreach (DataRow r in mydata.Rows)
                {
                    string rowType = r.Table.Columns.Contains("RowType") ? (r["RowType"]?.ToString() ?? "") : "";
                    string sttVal = r["STT"]?.ToString() ?? "";
                    bool isChild = rowType == "Person" || sttVal.Contains("├");

                    if (!isChild)
                    {
                        current = new List<DataRow> { r };
                        groups.Add(current);
                    }
                    else if (current != null)
                    {
                        current.Add(r);
                    }
                }

                // Sort groups theo STT số của company row
                groups.Sort((a, b) =>
                {
                    int sttA = 0, sttB = 0;
                    int.TryParse(a[0]["STT"]?.ToString() ?? "0", out sttA);
                    int.TryParse(b[0]["STT"]?.ToString() ?? "0", out sttB);
                    return sttA.CompareTo(sttB);
                });

                // Rebuild DataTable theo thứ tự mới
                DataTable sorted = mydata.Clone();
                foreach (var grp in groups)
                {
                    foreach (var r in grp)
                        sorted.ImportRow(r);
                }

                mydata.Rows.Clear();
                foreach (DataRow r in sorted.Rows)
                    mydata.ImportRow(r);
            }
            catch { }
        }
        private void QuitAllDrivers()
        {
            // Quit tất cả Chrome instances đang chạy
            while (_activeDrivers.TryTake(out var d))
            {
                try { d.Quit(); d.Dispose(); } catch { }
            }
            try { if (driver != null) { driver.Quit(); driver.Dispose(); driver = null; } } catch { }
        }
        private void KillAllChromeProcesses()
        {
            try
            {
                string chromePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Chrome_v121").ToLower();
                // Kill chrome.exe từ thư mục Chrome_v121
                foreach (var proc in System.Diagnostics.Process.GetProcessesByName("chrome"))
                {
                    try
                    {
                        string exePath = proc.MainModule?.FileName?.ToLower() ?? "";
                        if (exePath.Contains(chromePath)) proc.Kill();
                    }
                    catch { }
                }
                // Kill chromedriver.exe
                foreach (var proc in System.Diagnostics.Process.GetProcessesByName("chromedriver"))
                {
                    try { proc.Kill(); } catch { }
                }
            }
            catch { }
        }
        private void QuitDriver()
        {
            QuitAllDrivers();
        }
        private void gridData_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
        {
            if (dgvInput.Columns[e.ColumnIndex].Name == "Shipper" && e.RowIndex > 0)
            {
                // Nếu tên công ty giống dòng trên thì làm mờ chữ đi cho dễ nhìn
                if (dgvInput.Rows[e.RowIndex].Cells["Shipper"].Value?.ToString() ==
                    dgvInput.Rows[e.RowIndex - 1].Cells["Shipper"].Value?.ToString())
                {
                    e.CellStyle.ForeColor = Color.LightGray;
                }
            }
        }

        private async void dgvOutput_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            if (dgvOutput.Columns[e.ColumnIndex].Name == "btnUnBlock" && e.RowIndex >= 0)
            {
                string peLinkedIn = dgvOutput.Rows[e.RowIndex].Cells["LinkedInPe"].Value.ToString();

                // Gọi API SaleQL (Giả lập)
                dgvOutput.Rows[e.RowIndex].Cells["btnUnBlock"].Value = "Loading...";
                if (peLinkedIn.ToLower() != "n/a")
                {
                    var contact = await CallSaleQLAPI(peLinkedIn); // Hàm này bạn tự định nghĩa API Key

                    if (contact != null)
                    {
                        string PersonID = dgvOutput.Rows[e.RowIndex].Cells["PersonID"].Value.ToString();
                        DataTable dt = DatabaseHelper.ExecuteQuery("SELECT ID FROM Person WHERE ID='" + PersonID + "'");
                        if (dt.Rows.Count > 0)
                        {
                            string phone = "";
                            string email = "";
                            if (contact.Phone != null) phone = contact.Phone;
                            if (contact.Email != null) email = contact.Email;
                            SqliteParameter[] paras = {
                            new SqliteParameter("$PersonID",PersonID),
                            new SqliteParameter("$Email", email),
                            new SqliteParameter("$Phone",phone)
                        };
                            string sql = "UPDATE Person SET Email = $Email,Phone=$Phone WHERE ID=$PersonID";

                            DatabaseHelper.ExecuteNonQuery(sql, paras);
                            //MessageBox.Show("Đã lưu thành công!");
                        }
                        dgvOutput.Rows[e.RowIndex].Cells["EmailPe"].Value = contact.Email;
                        dgvOutput.Rows[e.RowIndex].Cells["PhonePe"].Value = contact.Phone;
                        dgvOutput.Rows[e.RowIndex].Cells["btnUnBlock"].Value = "Done";
                    }
                }
            }
        }
        private string GetValueAfterLabel(string text, string label)
        {
            var line = text.Split('\n').FirstOrDefault(l => l.Contains(label));
            if (line != null && line.Contains(":"))
            {
                return line.Substring(line.IndexOf(":") + 1).Trim();
            }
            return "Đang cập nhật";
        }

        private void ParseGoogleAIContent(string aiText, int rowIndex)
        {
            // 1. Lấy thông tin công ty
            string website = GetValueAfterLabel(aiText, "Website");
            string address = GetValueAfterLabel(aiText, "Địa chỉ");
            string phone = GetValueAfterLabel(aiText, "Số điện thoại");
            string email = GetValueAfterLabel(aiText, "Email");
            string linkedinCo = GetValueAfterLabel(aiText, "Linkedin");

            // 2. Lấy thông tin lãnh đạo đầu tiên (Ví dụ: Richard Forwood hoặc Hoàng Thị Thúy An)
            // Bạn có thể dùng Regex để bắt cụm "Họ và tên: [Tên]"
            var nameMatch = Regex.Match(aiText, @"Họ và tên:\s*([^\n\r]+)");
            var posMatch = Regex.Match(aiText, @"Chức vụ:\s*([^\n\r]+)");

            string leaderName = nameMatch.Success ? nameMatch.Groups[1].Value.Trim() : "";
            string position = posMatch.Success ? posMatch.Groups[1].Value.Trim() : "";

        }
        /// <summary>
        /// Trích xuất chuỗi JSON hợp lệ đầu tiên từ text (bắt đầu bằng { và kết thúc bằng }).
        /// Xử lý trường hợp AI trả về text thừa trước/sau JSON.
        /// </summary>
        private string ExtractJsonSafe(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw)) return "{}";

            // Bước 1: Bỏ markdown code fences nếu có (```json ... ``` hoặc ``` ... ```)
            var mdMatch = Regex.Match(raw, @"```(?:json)?\s*([\s\S]*?)```", RegexOptions.IgnoreCase);
            if (mdMatch.Success)
                raw = mdMatch.Groups[1].Value.Trim();

            // Bước 2: Tìm vị trí { đầu tiên và } cuối cùng để cắt đúng JSON
            int start = raw.IndexOf('{');
            if (start < 0) return "{}";

            // Đếm ngoặc để tìm đúng } đóng cặp với { đầu tiên
            int depth = 0;
            int end = -1;
            for (int idx = start; idx < raw.Length; idx++)
            {
                if (raw[idx] == '{') depth++;
                else if (raw[idx] == '}') depth--;

                if (depth == 0)
                {
                    end = idx;
                    break;
                }
            }

            if (end < 0) return "{}";
            return raw.Substring(start, end - start + 1);
        }

        private async Task<string> CallGeminiAPI(string prompt)
        {
            // 1. Lấy key và model từ DB
            string apiKey = "";
            string modelName = "gemini-2.5-flash";
            DataTable dtConfig = DatabaseHelper.ExecuteQuery("SELECT aistudio_key, gemini_model FROM Config LIMIT 1");
            if (dtConfig.Rows.Count > 0)
            {
                apiKey = dtConfig.Rows[0]["aistudio_key"]?.ToString() ?? "";
                string savedModel = dtConfig.Rows[0]["gemini_model"]?.ToString() ?? "";
                if (!string.IsNullOrEmpty(savedModel))
                {
                    modelName = savedModel;
                }
            }
            if (string.IsNullOrEmpty(apiKey)) return "{}";

            try
            {
                // 2. Khởi tạo Client
                var client = new GenerativeModel(apiKey: apiKey, model: modelName);

                // 3. Cấu hình
                var config = new GenerationConfig
                {
                    Temperature = 0.1f,
                    ResponseMimeType = "application/json"
                };

                // 4. Giải quyết lỗi CS1503: Tạo object request đúng chuẩn
                var request = new GenerateContentRequest
                {
                    Contents = new List<Content>
            {
                new Content
                {
                    Role = "user",
                    Parts = new List<Part> { new Part { Text = prompt } }
                }
            },
                    GenerationConfig = config
                };

                // 5. Gọi AI
                var response = await client.GenerateContentAsync(request);

                // 6. Giải quyết lỗi CS0428 & CS1503: Lấy kết quả an toàn
                // Lưu ý: Candidates thường là một List, ta dùng .Any() hoặc .Count() 
                if (response.Candidates != null && response.Candidates.Any())
                {
                    var firstCandidate = response.Candidates.First();
                    if (firstCandidate.Content != null && firstCandidate.Content.Parts.Any())
                    {
                        // Truy cập trực tiếp vào Text của Part đầu tiên
                        return firstCandidate.Content.Parts.First().Text;
                    }
                }

                return "{}";
            }
            catch (Exception ex)
            {
                this.Invoke(new Action(() => lblStatus.Text = "Lỗi API: " + ex.Message));
                return "{}";
            }
        }
        private async Task<string> CallOpenAI(string prompt)
        {
            // 1. Lấy API key từ DB
            string apiKey = "";
            DataTable dtConfig = DatabaseHelper.ExecuteQuery("SELECT openai_key FROM Config LIMIT 1");
            if (dtConfig.Rows.Count > 0)
                apiKey = dtConfig.Rows[0]["openai_key"].ToString();
            if (string.IsNullOrEmpty(apiKey))
                return "{}";

            try
            {
                using var http = new HttpClient();
                http.DefaultRequestHeaders.Authorization =
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", apiKey);

                // 3. Request body
                var requestBody = new
                {
                    model = "gpt-4o-mini",
                    messages = new[]
                    {
                        new { role = "system", content = "You extract structured data and never hallucinate." },
                        new { role = "user", content = prompt }
                    },
                    temperature = 0.0
                };

                // 4. Gọi API
                var response = await http.PostAsJsonAsync(
                    "https://api.openai.com/v1/chat/completions",
                    requestBody
                );

                if (!response.IsSuccessStatusCode)
                {
                    string err = await response.Content.ReadAsStringAsync();
                    this.Invoke(new Action(() => lblStatus.Text = "OpenAI error: " + err));
                    return "{}";
                }

                // 5. Parse kết quả
                using var json = System.Text.Json.JsonDocument.Parse(
                    await response.Content.ReadAsStringAsync()
                );

                var content = json
                    .RootElement
                    .GetProperty("choices")[0]
                    .GetProperty("message")
                    .GetProperty("content")
                    .GetString();

                return content ?? "{}";
            }
            catch (Exception ex)
            {
                this.Invoke(new Action(() => lblStatus.Text = "Lỗi OpenAI: " + ex.Message));
                return "{}";
            }
        }



        private async Task<SaleQLResult> CallSaleQLAPI(string linkedInUrl)
        {
            // Thay API Key SaleQL của bạn vào đây
            DataTable dt = DatabaseHelper.ExecuteQuery("SELECT salesql_key FROM Config LIMIT 1");

            if (dt.Rows.Count == 0 || string.IsNullOrEmpty(dt.Rows[0]["salesql_key"].ToString()))
            {
                MessageBox.Show("Lỗi: Chưa tìm thấy SaleQL Key trong Database. Hãy lưu Key ở Form Config trước!");
                return null;
            }

            string apiKey = dt.Rows[0]["salesql_key"].ToString();
            // Endpoint chuẩn của SaleQL để lấy thông tin từ LinkedIn URL
            string url = $"https://api-public.salesql.com/v1/persons/enrich?linkedin_url={Uri.EscapeDataString(linkedInUrl)}&api_key=" + apiKey;
            //url = $"https://api-public.salesql.com/v1/persons/enrich?linkedin_url={Uri.EscapeDataString(" https://www.linkedin.com/in/brandon-l-b8871917a")}&api_key=" + apiKey;

            using (HttpClient client = new HttpClient())
            {
                try
                {
                    // SaleQL thường yêu cầu API Key trong Header
                    client.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));

                    var response = await client.GetAsync(url);

                    if (response.IsSuccessStatusCode)
                    {
                        string json = await response.Content.ReadAsStringAsync();

                        // Phân tích JSON trả về từ SaleQL
                        // Lưu ý: Cấu trúc này dựa trên Documentation phổ biến của SaleQL, 
                        // bạn có thể điều chỉnh tùy theo gói API bạn mua.
                        dynamic data = JsonConvert.DeserializeObject(json);

                        var result = new SaleQLResult();

                        // Lấy Email (nếu có)
                        if (data.emails != null && data.emails.Count > 0)
                        {
                            var emails = new List<string>();
                            foreach (var e in data.emails)
                            {
                                string mail = e.email?.ToString() ?? "";
                                if (!string.IsNullOrEmpty(mail) && !emails.Contains(mail)) emails.Add(mail);
                            }
                            result.Email = string.Join(" - ", emails);
                        }
                        else { result.Email = "_N/A"; }

                        // Lấy Phone (nếu có)
                        if (data.phones != null && data.phones.Count > 0)
                        {
                            var phones = new List<string>();
                            foreach (var p in data.phones)
                            {
                                string num = (p.phone ?? p.number)?.ToString() ?? "";
                                if (!string.IsNullOrEmpty(num) && !phones.Contains(num)) phones.Add(num);
                            }
                            result.Phone = string.Join(" - ", phones);
                        }
                        else { result.Phone = "_N/A"; }

                        return result;
                    }
                    else
                    {
                        string error = await response.Content.ReadAsStringAsync();
                        MessageBox.Show("Lỗi SaleQL: " + error);
                        return null;
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Lỗi kết nối SaleQL: " + ex.Message);
                    return null;
                }
            }
        }

                private void FormatGridView(string col1, string col2, string col3 = "")
        {
            dgvInput.ReadOnly = false;
            // Format Select column
            if (dgvInput.Columns.Contains("Select"))
            {
                dgvInput.Columns["Select"].Width = 50;
                dgvInput.Columns["Select"].HeaderText = "Chọn";
                dgvInput.Columns["Select"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
                dgvInput.Columns["Select"].Resizable = DataGridViewTriState.False;
                dgvInput.Columns["Select"].DisplayIndex = 0;
            }

            // 1. STT: Cho nhỏ lại và căn giữa
            dgvInput.Columns["STT"].Width = 40;
            dgvInput.Columns["STT"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            dgvInput.Columns["STT"].Resizable = DataGridViewTriState.False;
            if (dgvInput.Columns.Contains("STT"))
            {
                dgvInput.Columns["STT"].DisplayIndex = 1;
            }

            // 2. Status: Cho nhỏ lại, vừa đủ hiển thị chữ "Completed"
            dgvInput.Columns["Status"].Width = 90;
            dgvInput.Columns["Status"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;

            // 2b. ErrorDetail: Hiển thị chi tiết lỗi, ẩn đi nếu không cần
            if (dgvInput.Columns.Contains("ErrorDetail"))
            {
                dgvInput.Columns["ErrorDetail"].Width = 300;
                dgvInput.Columns["ErrorDetail"].HeaderText = "Error Detail";
                dgvInput.Columns["ErrorDetail"].DefaultCellStyle.ForeColor = Color.FromArgb(180, 0, 0);
                dgvInput.Columns["ErrorDetail"].DefaultCellStyle.WrapMode = DataGridViewTriState.True;
                // Đăng ký event CellFormatting để tô màu dòng Error
                dgvInput.CellFormatting -= dgvInput_CellFormatting; // tránh đăng ký 2 lần
                dgvInput.CellFormatting += dgvInput_CellFormatting;
            }

            // 3. Tên công ty (Shipper): Cho to ra và tự động lấp đầy khoảng trống
            dgvInput.Columns["" + col1 + ""].AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;

            // 4. Các cột khác (Địa chỉ...): Cho hiển thị vừa phải
            if (dgvInput.Columns.Contains("" + col2 + ""))
            {
                dgvInput.Columns["" + col2 + ""].Width = 600;
            }

            // 5. Cột Address (nếu có): Hiển thị vừa phải
            if (!string.IsNullOrEmpty(col3) && dgvInput.Columns.Contains(col3))
            {
                dgvInput.Columns[col3].Width = 400;
                dgvInput.Columns[col3].HeaderText = col3;
            }

            // Tùy chỉnh thêm để lưới trông sạch sẽ hơn
            dgvInput.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dgvInput.RowHeadersVisible = false; // Ẩn cột đầu dòng trống

            foreach (DataGridViewColumn col in dgvInput.Columns)
            {
                if (col.Name == "Select")
                {
                    col.ReadOnly = false;
                }
                else
                {
                    col.ReadOnly = true;
                }
            }
        }

        private void ChkCheckAll_CheckedChanged(object sender, EventArgs e)
        {
            if (_isUpdatingCheckAll) return;
            if (dgvInput.DataSource is DataTable dt)
            {
                if (dt.Columns.Contains("Select"))
                {
                    _isUpdatingCheckAll = true;
                    bool isChecked = chkCheckAll.Checked;
                    foreach (DataRow row in dt.Rows)
                    {
                        row["Select"] = isChecked;
                    }
                    _isUpdatingCheckAll = false;
                }
            }
        }

        private void ChkCheckAllOutput_CheckedChanged(object sender, EventArgs e)
        {
            if (_isUpdatingCheckAll) return;
            _isUpdatingCheckAll = true;
            bool isChecked = chkCheckAllOutput.Checked;
            foreach (DataGridViewRow row in dgvOutput.Rows)
            {
                string rowType = row.Cells["RowType"].Value?.ToString() ?? "";
                if (rowType == "Company")
                {
                    row.Cells["chkSelect"].Value = isChecked;
                }
            }
            _isUpdatingCheckAll = false;
        }

        private void dgvOutput_CellValueChanged(object sender, DataGridViewCellEventArgs e)
        {
            if (_isUpdatingCheckAll) return;
            if (e.RowIndex >= 0 && dgvOutput.Columns.Contains("chkSelect") && dgvOutput.Columns[e.ColumnIndex].Name == "chkSelect")
            {
                UpdateCheckAllOutputState();
            }
        }

        private void UpdateCheckAllOutputState()
        {
            if (_isUpdatingCheckAll) return;
            _isUpdatingCheckAll = true;
            bool allChecked = true;
            bool anyCompany = false;
            foreach (DataGridViewRow row in dgvOutput.Rows)
            {
                string rowType = row.Cells["RowType"].Value?.ToString() ?? "";
                if (rowType == "Company")
                {
                    anyCompany = true;
                    bool isChecked = row.Cells["chkSelect"].Value is bool b && b;
                    if (!isChecked)
                    {
                        allChecked = false;
                        break;
                    }
                }
            }
            chkCheckAllOutput.Checked = anyCompany && allChecked;
            _isUpdatingCheckAll = false;
        }

                private void dgvInput_CurrentCellDirtyStateChanged(object sender, EventArgs e)
        {
            if (dgvInput.IsCurrentCellDirty)
            {
                dgvInput.CommitEdit(DataGridViewDataErrorContexts.Commit);
            }
        }

        private void dgvOutput_CurrentCellDirtyStateChanged(object sender, EventArgs e)
        {
            if (dgvOutput.IsCurrentCellDirty)
            {
                dgvOutput.CommitEdit(DataGridViewDataErrorContexts.Commit);
            }
        }

        private void dgvInput_CellValueChanged(object sender, DataGridViewCellEventArgs e)
        {
            if (_isUpdatingCheckAll) return;
            if (e.RowIndex >= 0 && dgvInput.Columns[e.ColumnIndex].Name == "Select")
            {
                UpdateCheckAllState();
            }
        }

        private void UpdateCheckAllState()
        {
            if (_isUpdatingCheckAll) return;
            if (dgvInput.DataSource is DataTable dt && dt.Columns.Contains("Select"))
            {
                _isUpdatingCheckAll = true;
                bool allChecked = true;
                foreach (DataRow row in dt.Rows)
                {
                    if (row.RowState != DataRowState.Deleted)
                    {
                        bool isChecked = row.Field<bool?>("Select") ?? false;
                        if (!isChecked)
                        {
                            allChecked = false;
                            break;
                        }
                    }
                }
                chkCheckAll.Checked = allChecked;
                _isUpdatingCheckAll = false;
            }
        }
        private void dgvInput_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
        {
            if (e.RowIndex < 0) return;
            var grid = (DataGridView)sender;
            if (grid.Columns.Contains("Status"))
            {
                var statusCell = grid.Rows[e.RowIndex].Cells["Status"];
                string status = statusCell.Value?.ToString() ?? "";

                Color rowBack, rowFore;
                switch (status)
                {
                    case "Error":
                        rowBack = Color.FromArgb(255, 220, 220); // đỏ nhạt
                        rowFore = Color.FromArgb(140, 0, 0);
                        break;
                    case "API Error":
                        rowBack = Color.FromArgb(255, 235, 200); // cam nhạt
                        rowFore = Color.FromArgb(160, 80, 0);
                        break;
                    case "Completed":
                        rowBack = Color.FromArgb(220, 245, 220); // xanh lá nhạt
                        rowFore = Color.FromArgb(0, 100, 0);
                        break;
                    case "Skipped":
                        rowBack = Color.FromArgb(240, 240, 240); // xám nhạt
                        rowFore = Color.FromArgb(100, 100, 100);
                        break;
                    default:
                        return; // Pending, Not Found... giữ màu mặc định
                }

                // Không tô màu cột ErrorDetail riêng (đã set ForeColor đỏ rồi)
                string colName = grid.Columns[e.ColumnIndex].Name;
                if (colName == "ErrorDetail")
                {
                    // Để màu đỏ riêng cho cột này
                    if (status == "Error" || status == "API Error")
                        e.CellStyle.ForeColor = Color.FromArgb(180, 0, 0);
                    else
                        e.CellStyle.ForeColor = rowFore;
                    e.CellStyle.BackColor = rowBack;
                }
                else
                {
                    e.CellStyle.BackColor = rowBack;
                    e.CellStyle.ForeColor = rowFore;
                }
            }
        }

        private void PrepareColumns()
        {
            DataTable dt = (DataTable)dgvInput.DataSource;
            string[] newCols = { "Website", "Co_LinkedIn", "Co_Email", "Pe_FullName", "Pe_Position", "Pe_LinkedIn", "Pe_Phone", "Pe_Email" };

            foreach (var col in newCols)
            {
                if (!dt.Columns.Contains(col))
                    dt.Columns.Add(col);
            }
        }

        private void InitGridOutput()
        {
                        dgvOutput.KeyDown += (s, e) =>
            {
                if (e.Control && e.KeyCode == System.Windows.Forms.Keys.C)
                {
                    if (dgvOutput.CurrentCell != null)
                    {
                        string val = dgvOutput.CurrentCell.Value?.ToString() ?? "";
                        if (!string.IsNullOrEmpty(val))
                        {
                            Clipboard.SetText(val);
                        }
                        else
                        {
                            Clipboard.Clear();
                        }
                        e.Handled = true;
                    }
                }
            };
            dgvOutput.CurrentCellDirtyStateChanged += dgvOutput_CurrentCellDirtyStateChanged;
            dgvOutput.CellValueChanged += dgvOutput_CellValueChanged;

            dgvInput.KeyDown += (s, e) =>
            {
                if (e.Control && e.KeyCode == System.Windows.Forms.Keys.C)
                {
                    if (dgvInput.CurrentCell != null)
                    {
                        string val = dgvInput.CurrentCell.Value?.ToString() ?? "";
                        if (!string.IsNullOrEmpty(val))
                        {
                            Clipboard.SetText(val);
                        }
                        else
                        {
                            Clipboard.Clear();
                        }
                        e.Handled = true;
                    }
                }
            };

            mydata = new DataTable();
            mydata.Columns.Add("STT");
            mydata.Columns.Add("CompanyName");
            mydata.Columns.Add("CompanyAddress");
            mydata.Columns.Add("Industry");
            mydata.Columns.Add("PhoneCo");
            mydata.Columns.Add("Website");
            mydata.Columns.Add("EmailCo");
            mydata.Columns.Add("PersonID");
            mydata.Columns.Add("CompanyID");
            mydata.Columns.Add("LinkedInCo");
            mydata.Columns.Add("FullNamePe");
            mydata.Columns.Add("PositionPe");
            mydata.Columns.Add("LinkedInPe");
            mydata.Columns.Add("EmailPe");
            mydata.Columns.Add("PhonePe");
            mydata.Columns.Add("RowType"); // "Company" hoặc "Person"

                        // Tắt auto-gen để kiểm soát thứ tự cột hoàn toàn
            dgvOutput.AutoGenerateColumns = false;
            dgvOutput.MultiSelect = true; // Cho phép check nhiều dòng
            dgvOutput.ReadOnly = false;
            dgvOutput.DataSource = null;
            dgvOutput.Columns.Clear();
            dgvOutput.DataSource = mydata;

            // === CHECKBOX COLUMN - thêm đầu tiên để hiện ở cột đầu ===
            var chkCol = new DataGridViewCheckBoxColumn();
            chkCol.Name = "chkSelect";
            chkCol.HeaderText = "✓";
            chkCol.Width = 30;
            chkCol.AutoSizeMode = DataGridViewAutoSizeColumnMode.None;
                        chkCol.FalseValue = false;
            chkCol.TrueValue = true;
            chkCol.ReadOnly = false;
            chkCol.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            dgvOutput.Columns.Add(chkCol);

            // === Thêm các cột bound từ DataTable theo thứ tự ===
            var boundCols = new (string name, string header)[]
            {
                ("STT", "STT"), ("CompanyName", "CompanyName"), ("CompanyAddress", "CompanyAddress"),
                ("Industry", "Industry"), ("PhoneCo", "PhoneCo"), ("Website", "Website"),
                ("EmailCo", "EmailCo"), ("PersonID", "PersonID"), ("CompanyID", "CompanyID"), ("LinkedInCo", "LinkedInCo"),
                ("FullNamePe", "FullNamePe"), ("PositionPe", "PositionPe"),
                ("LinkedInPe", "LinkedInPe"), ("EmailPe", "EmailPe"), ("PhonePe", "PhonePe"),
                ("RowType", "RowType")
            };
                        foreach (var (name, header) in boundCols)
            {
                var col = new DataGridViewTextBoxColumn();
                col.Name = name;
                col.HeaderText = header;
                col.DataPropertyName = name; // Bind vào DataTable column
                col.ReadOnly = true;
                dgvOutput.Columns.Add(col);
            }


            DataGridViewButtonColumn btnColumn = new DataGridViewButtonColumn();
            btnColumn.Name = "btnUnBlock";
            btnColumn.HeaderText = "Action";
            btnColumn.Text = "UnBlock";
            btnColumn.UseColumnTextForButtonValue = true;
            btnColumn.FlatStyle = FlatStyle.Flat;
            btnColumn.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleLeft;
            btnColumn.DefaultCellStyle.BackColor = Color.FromArgb(40, 167, 69);
            btnColumn.DefaultCellStyle.ForeColor = Color.White;
            btnColumn.DefaultCellStyle.SelectionBackColor = Color.FromArgb(33, 136, 56);
            btnColumn.DefaultCellStyle.SelectionForeColor = Color.White;
            btnColumn.DefaultCellStyle.Padding = new Padding(2, 5, 20, 5);
            btnColumn.Width = 80;
            btnColumn.AutoSizeMode = DataGridViewAutoSizeColumnMode.None;
            btnColumn.DefaultCellStyle.Font = new Font("Segoe UI", 8, FontStyle.Regular);
            dgvOutput.Columns.Add(btnColumn);

            dgvOutput.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None;
            dgvOutput.RowHeadersVisible = false;
            dgvOutput.DefaultCellStyle.WrapMode = DataGridViewTriState.True;
            dgvOutput.AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.AllCells;
            foreach (DataGridViewColumn col in dgvOutput.Columns)
            {
                col.AutoSizeMode = DataGridViewAutoSizeColumnMode.None;
                col.Width = 150;
            }
            if (dgvOutput.Columns.Contains("STT"))
                dgvOutput.Columns["STT"].Width = 50;
            if (dgvOutput.Columns.Contains("chkSelect"))
                dgvOutput.Columns["chkSelect"].Width = 30;
            if (dgvOutput.Columns.Contains("RowType"))
                dgvOutput.Columns["RowType"].Visible = false;
            if (dgvOutput.Columns.Contains("CompanyID"))
                dgvOutput.Columns["CompanyID"].Visible = false;
            if (dgvOutput.Columns.Contains("PersonID"))
                dgvOutput.Columns["PersonID"].Visible = false;

            // Đăng ký CellFormatting để phân biệt cha/con
            dgvOutput.CellFormatting -= dgvOutput_CellFormatting;
            dgvOutput.CellFormatting += dgvOutput_CellFormatting;

            // Đăng ký DataBindingComplete để ẩn checked dòng con Person
            dgvOutput.DataBindingComplete -= dgvOutput_DataBindingComplete;
            dgvOutput.DataBindingComplete += dgvOutput_DataBindingComplete;

            // Đăng ký CellPainting để ẩn vẽ checkbox cho dòng con Person
            dgvOutput.CellPainting -= dgvOutput_CellPainting;
            dgvOutput.CellPainting += dgvOutput_CellPainting;

            // === RIGHT-CLICK CONTEXT MENU ===
            var ctxMenu = new ContextMenuStrip();
            var menuChayLai = new ToolStripMenuItem("🔄 Chạy lại");
            var menuUnblock = new ToolStripMenuItem("🔓 Unblock (SalesQL)");
            var menuCrawlGoogle = new ToolStripMenuItem("🔍 Crawl từ Google");
            var menuCrawlWebsite = new ToolStripMenuItem("🌐 Crawl từ website");
            var menuCrawlLinkedIn = new ToolStripMenuItem("🔗 Crawl từ LinkedIn");
            ctxMenu.Items.Add(menuChayLai);
            ctxMenu.Items.Add(menuUnblock);
            ctxMenu.Items.Add(menuCrawlGoogle);
            ctxMenu.Items.Add(menuCrawlWebsite);
            ctxMenu.Items.Add(menuCrawlLinkedIn);

                                    menuChayLai.Click += async (s, ev) =>
            {
                dgvOutput.EndEdit();
                _isRerunMode = true;
                // Thu thập tất cả CompanyName từ các dòng đang được ✅ check hoặc đang được bôi xanh (Selected)
                var companiesToRerun = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

                // 1. Kiểm tra các dòng được check
                foreach (DataGridViewRow row in dgvOutput.Rows)
                {
                    bool isChecked = row.Cells["chkSelect"].Value is bool b && b;
                    if (isChecked)
                    {
                        AddCompanyFromGridRow(row, companiesToRerun);
                    }
                }

                // 2. Nếu không có dòng nào được check, kiểm tra các dòng được chọn (bôi xanh)
                if (companiesToRerun.Count == 0)
                {
                    foreach (DataGridViewRow row in dgvOutput.SelectedRows)
                    {
                        AddCompanyFromGridRow(row, companiesToRerun);
                    }
                }

                // 3. Nếu vẫn trống, lấy dòng chứa các ô đang được chọn (SelectedCells)
                if (companiesToRerun.Count == 0)
                {
                    var processedRows = new HashSet<int>();
                    foreach (DataGridViewCell cell in dgvOutput.SelectedCells)
                    {
                        int rowIdx = cell.RowIndex;
                        if (rowIdx >= 0 && processedRows.Add(rowIdx))
                        {
                            AddCompanyFromGridRow(dgvOutput.Rows[rowIdx], companiesToRerun);
                        }
                    }
                }

                // 4. Nếu vẫn trống, dùng CurrentRow
                if (companiesToRerun.Count == 0 && dgvOutput.CurrentRow != null)
                {
                    AddCompanyFromGridRow(dgvOutput.CurrentRow, companiesToRerun);
                }

                if (companiesToRerun.Count == 0) return;

                // KHÔNG xoá dòng hiện tại trên grid output.
                // UpdateGridOutput sẽ tự merge (chỉ cập nhật các field N/A hoặc trống).

                // Đặt lại Status = Pending trong input grid
                DataTable dtInput = dgvInput.DataSource as DataTable;
                var targetRows = new List<DataRow>();
                if (dtInput != null)
                {
                    foreach (DataRow r in dtInput.Rows)
                    {
                        string cName = r.Table.Columns.Count > 1 ? r[1]?.ToString() ?? "" : "";
                        if (companiesToRerun.Contains(cName))
                        {
                            r["Status"] = "Pending";
                            targetRows.Add(r);
                        }
                    }
                }

                // Chạy lại nhiều công ty
                if (!btnStart.Enabled)
                {
                    MessageBox.Show("Đang chạy, hãy dừng trước!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                if (targetRows.Count == 0) return;
                _cts = new CancellationTokenSource();
                _activeDrivers = new ConcurrentBag<IWebDriver>();
                btnStart.Enabled = false; btnStop.Enabled = true;
                lblStatus.Text = $"Chạy lại {targetRows.Count} công ty...";
                try
                {
                    int tc = Math.Max(1, _threadCount);
                    var batches = new List<List<DataRow>>();
                    for (int t = 0; t < tc; t++) batches.Add(new List<DataRow>());
                    for (int i = 0; i < targetRows.Count; i++) batches[i % tc].Add(targetRows[i]);
                    var tasks2 = new List<Task>();
                    for (int t = 0; t < tc; t++)
                    {
                        int ti = t; var batch = batches[t];
                        if (batch.Count > 0)
                            tasks2.Add(Task.Run(async () => await ProcessBatch(batch, ti, _cts.Token)));
                    }
                    await Task.WhenAll(tasks2);
                }
                catch { }
                                finally { btnStart.Enabled = true; btnStop.Enabled = false; lblStatus.Text = "Finished"; UncheckOutputGrid(); }
            };


                                    menuUnblock.Click += async (s, ev) =>
            {
                dgvOutput.EndEdit();
                _isRerunMode = true;
                // Lấy tất cả các dòng Person được check, chọn hoặc bôi xanh
                var checkedRows = new List<DataGridViewRow>();
                
                // 1. Kiểm tra các dòng được check
                foreach (DataGridViewRow row in dgvOutput.Rows)
                {
                    bool isChecked = row.Cells["chkSelect"].Value is bool b && b;
                    string rowType = row.Cells["RowType"].Value?.ToString() ?? "";
                    if (isChecked && rowType == "Person")
                        checkedRows.Add(row);
                }

                // 2. Nếu không có dòng nào được check, lấy các dòng được chọn (bôi xanh)
                if (checkedRows.Count == 0)
                {
                    foreach (DataGridViewRow row in dgvOutput.SelectedRows)
                    {
                        string rowType = row.Cells["RowType"].Value?.ToString() ?? "";
                        if (rowType == "Person")
                            checkedRows.Add(row);
                    }
                }

                // 3. Lấy dòng chứa các ô đang được chọn (SelectedCells)
                if (checkedRows.Count == 0)
                {
                    var processedRows = new HashSet<int>();
                    foreach (DataGridViewCell cell in dgvOutput.SelectedCells)
                    {
                        int rowIdx = cell.RowIndex;
                        if (rowIdx >= 0 && processedRows.Add(rowIdx))
                        {
                            var row = dgvOutput.Rows[rowIdx];
                            string rowType = row.Cells["RowType"].Value?.ToString() ?? "";
                            if (rowType == "Person")
                                checkedRows.Add(row);
                        }
                    }
                }

                // 4. Dùng CurrentRow
                if (checkedRows.Count == 0 && dgvOutput.CurrentRow != null)
                {
                    string rt = dgvOutput.CurrentRow.Cells["RowType"].Value?.ToString() ?? "";
                    if (rt == "Person") checkedRows.Add(dgvOutput.CurrentRow);
                }

                if (checkedRows.Count == 0)
                {
                    MessageBox.Show("Hãy chọn (tick) ít nhất 1 nhân sự để Unblock!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                                foreach (var row in checkedRows)
                {
                    string peLinkedIn = row.Cells["LinkedInPe"].Value?.ToString() ?? "";
                    if (string.IsNullOrEmpty(peLinkedIn) || peLinkedIn.ToLower() == "n/a")
                    {
                        row.Cells["chkSelect"].Value = false;
                        continue;
                    }

                    row.Cells["btnUnBlock"].Value = "Loading...";
                    try
                    {
                        var contact = await CallSaleQLAPI(peLinkedIn);
                        if (contact != null)
                        {
                            string personId = row.Cells["PersonID"].Value?.ToString() ?? "";
                            if (!string.IsNullOrEmpty(personId))
                            {
                                DatabaseHelper.ExecuteNonQuery(
                                    "UPDATE Person SET Email=$Email, Phone=$Phone WHERE ID=$ID",
                                    new[] { new SqliteParameter("$Email", contact.Email ?? ""),
                                            new SqliteParameter("$Phone", contact.Phone ?? ""),
                                            new SqliteParameter("$ID", personId) });
                            }
                            row.Cells["EmailPe"].Value = contact.Email;
                            row.Cells["PhonePe"].Value = contact.Phone;
                            row.Cells["btnUnBlock"].Value = "Done";
                        }
                    }
                    catch (Exception ex)
                    {
                        row.Cells["btnUnBlock"].Value = "Error";
                        Console.WriteLine($"[Unblock] {peLinkedIn}: {ex.Message}");
                    }
                    finally
                    {
                        row.Cells["chkSelect"].Value = false;
                    }
                }
            };

                                    menuCrawlGoogle.Click += async (s, ev) =>
            {
                dgvOutput.EndEdit();
                _isRerunMode = true;
                // Thu thập tất cả CompanyName từ các dòng đang được ✅ check
                var companiesToRerun = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

                // 1. Kiểm tra các dòng được check
                foreach (DataGridViewRow row in dgvOutput.Rows)
                {
                    bool isChecked = row.Cells["chkSelect"].Value is bool b && b;
                    if (isChecked)
                    {
                        AddCompanyFromGridRow(row, companiesToRerun);
                    }
                }

                // 2. Nếu không có dòng nào được check, kiểm tra các dòng được chọn (bôi xanh)
                if (companiesToRerun.Count == 0)
                {
                    foreach (DataGridViewRow row in dgvOutput.SelectedRows)
                    {
                        AddCompanyFromGridRow(row, companiesToRerun);
                    }
                }

                // 3. Nếu vẫn trống, lấy dòng chứa các ô đang được chọn (SelectedCells)
                if (companiesToRerun.Count == 0)
                {
                    var processedRows = new HashSet<int>();
                    foreach (DataGridViewCell cell in dgvOutput.SelectedCells)
                    {
                        int rowIdx = cell.RowIndex;
                        if (rowIdx >= 0 && processedRows.Add(rowIdx))
                        {
                            AddCompanyFromGridRow(dgvOutput.Rows[rowIdx], companiesToRerun);
                        }
                    }
                }

                // 4. Nếu vẫn trống, dùng CurrentRow
                if (companiesToRerun.Count == 0 && dgvOutput.CurrentRow != null)
                {
                    AddCompanyFromGridRow(dgvOutput.CurrentRow, companiesToRerun);
                }

                if (companiesToRerun.Count == 0) return;

                // Đặt lại Status = Pending trong input grid
                DataTable dtInput = dgvInput.DataSource as DataTable;
                var targetRows = new List<DataRow>();
                if (dtInput != null)
                {
                    foreach (DataRow r in dtInput.Rows)
                    {
                        string cName = r.Table.Columns.Count > 1 ? r[1]?.ToString() ?? "" : "";
                        if (companiesToRerun.Contains(cName))
                        {
                            r["Status"] = "Pending";
                            targetRows.Add(r);
                        }
                    }
                }

                // Chạy lại Google search
                if (!btnStart.Enabled)
                {
                    MessageBox.Show("Đang chạy tiến trình khác, hãy dừng trước!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                if (targetRows.Count == 0) return;
                _cts = new CancellationTokenSource();
                _activeDrivers = new ConcurrentBag<IWebDriver>();
                btnStart.Enabled = false; btnStop.Enabled = true;
                lblStatus.Text = $"Crawl Google cho {targetRows.Count} công ty...";
                try
                {
                    int tc = Math.Max(1, _threadCount);
                    var batches = new List<List<DataRow>>();
                    for (int t = 0; t < tc; t++) batches.Add(new List<DataRow>());
                    for (int i = 0; i < targetRows.Count; i++) batches[i % tc].Add(targetRows[i]);
                    var tasks2 = new List<Task>();
                    for (int t = 0; t < tc; t++)
                    {
                        int ti = t; var batch = batches[t];
                        if (batch.Count > 0)
                            tasks2.Add(Task.Run(async () => await ProcessGoogleCrawlBatch(batch, ti, _cts.Token)));
                    }
                    await Task.WhenAll(tasks2);
                }
                catch { }
                                finally { btnStart.Enabled = true; btnStop.Enabled = false; lblStatus.Text = "Finished Google Crawl"; UncheckOutputGrid(); }
            };

                                    menuCrawlWebsite.Click += async (s, ev) =>
            {
                dgvOutput.EndEdit();
                _isRerunMode = true;
                // Thu thập các dòng Company được chọn
                var selectedCompRows = new List<DataGridViewRow>();
                var seenCompanies = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

                // 1. Kiểm tra các dòng được check
                foreach (DataGridViewRow row in dgvOutput.Rows)
                {
                    bool isChecked = row.Cells["chkSelect"].Value is bool b && b;
                    if (isChecked)
                    {
                        AddCompanyRowFromGridRow(row, selectedCompRows, seenCompanies);
                    }
                }

                // 2. Nếu không có dòng nào được check, lấy các dòng được chọn (bôi xanh)
                if (selectedCompRows.Count == 0)
                {
                    foreach (DataGridViewRow row in dgvOutput.SelectedRows)
                    {
                        AddCompanyRowFromGridRow(row, selectedCompRows, seenCompanies);
                    }
                }

                // 3. Lấy dòng chứa các ô đang được chọn (SelectedCells)
                if (selectedCompRows.Count == 0)
                {
                    var processedRows = new HashSet<int>();
                    foreach (DataGridViewCell cell in dgvOutput.SelectedCells)
                    {
                        int rowIdx = cell.RowIndex;
                        if (rowIdx >= 0 && processedRows.Add(rowIdx))
                        {
                            AddCompanyRowFromGridRow(dgvOutput.Rows[rowIdx], selectedCompRows, seenCompanies);
                        }
                    }
                }

                // 4. Dùng CurrentRow
                if (selectedCompRows.Count == 0 && dgvOutput.CurrentRow != null)
                {
                    AddCompanyRowFromGridRow(dgvOutput.CurrentRow, selectedCompRows, seenCompanies);
                }

                if (selectedCompRows.Count == 0) return;

                // Lọc các công ty có cột Website có dữ liệu
                var targets = new List<WebsiteCrawlTarget>();
                DataTable dtInput = dgvInput.DataSource as DataTable;

                foreach (var row in selectedCompRows)
                {
                    string companyName = row.Cells["CompanyName"].Value?.ToString() ?? "";
                    string website = row.Cells["Website"].Value?.ToString() ?? "";
                    if (string.IsNullOrEmpty(website) || website.ToUpper() == "N/A" || website.Trim() == "")
                        continue;

                    string phone = row.Cells["PhoneCo"].Value?.ToString() ?? "";
                    string email = row.Cells["EmailCo"].Value?.ToString() ?? "";
                    string linkedin = row.Cells["LinkedInCo"].Value?.ToString() ?? "";
                    string stt = row.Cells["STT"].Value?.ToString() ?? "";

                    DataRow inputRow = null;
                    if (dtInput != null)
                    {
                        foreach (DataRow r in dtInput.Rows)
                        {
                            string cName = r.Table.Columns.Count > 1 ? r[1]?.ToString() ?? "" : "";
                            if (string.Equals(cName.Trim(), companyName.Trim(), StringComparison.OrdinalIgnoreCase))
                            {
                                inputRow = r;
                                break;
                            }
                        }
                    }

                    targets.Add(new WebsiteCrawlTarget
                    {
                        CompanyName = companyName,
                        Website = website,
                        ExistingPhone = phone,
                        ExistingEmail = email,
                        ExistingLinkedIn = linkedin,
                        STT = stt,
                        InputRow = inputRow
                    });
                }

                if (targets.Count == 0)
                {
                    MessageBox.Show("Vui lòng chọn (hoặc click) các công ty có chứa dữ liệu Website!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                if (!btnStart.Enabled)
                {
                    MessageBox.Show("Đang chạy tiến trình khác, hãy dừng trước!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                _cts = new CancellationTokenSource();
                _activeDrivers = new ConcurrentBag<IWebDriver>();
                btnStart.Enabled = false; btnStop.Enabled = true;
                lblStatus.Text = $"Crawl website cho {targets.Count} công ty...";

                try
                {
                    int tc = Math.Max(1, _threadCount);
                    var batches = new List<List<WebsiteCrawlTarget>>();
                    for (int t = 0; t < tc; t++) batches.Add(new List<WebsiteCrawlTarget>());
                    for (int i = 0; i < targets.Count; i++) batches[i % tc].Add(targets[i]);

                    var tasks = new List<Task>();
                    for (int t = 0; t < tc; t++)
                    {
                        int ti = t;
                        var batch = batches[t];
                        if (batch.Count > 0)
                            tasks.Add(Task.Run(async () => await ProcessWebsiteCrawlBatch(batch, ti, _cts.Token)));
                    }
                    await Task.WhenAll(tasks);
                }
                catch { }
                                finally
                {
                    btnStart.Enabled = true; btnStop.Enabled = false;
                    lblStatus.Text = "Finished Website Crawl";
                    UncheckOutputGrid();
                }
            };

                                    menuCrawlLinkedIn.Click += async (s, ev) =>
            {
                dgvOutput.EndEdit();
                _isRerunMode = true;
                // Thu thập các dòng Company được chọn
                var selectedCompRows = new List<DataGridViewRow>();
                var seenCompanies = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

                // 1. Kiểm tra các dòng được check
                foreach (DataGridViewRow row in dgvOutput.Rows)
                {
                    bool isChecked = row.Cells["chkSelect"].Value is bool b && b;
                    if (isChecked)
                    {
                        AddCompanyRowFromGridRow(row, selectedCompRows, seenCompanies);
                    }
                }

                // 2. Nếu không có dòng nào được check, lấy các dòng được chọn (bôi xanh)
                if (selectedCompRows.Count == 0)
                {
                    foreach (DataGridViewRow row in dgvOutput.SelectedRows)
                    {
                        AddCompanyRowFromGridRow(row, selectedCompRows, seenCompanies);
                    }
                }

                // 3. Lấy dòng chứa các ô đang được chọn (SelectedCells)
                if (selectedCompRows.Count == 0)
                {
                    var processedRows = new HashSet<int>();
                    foreach (DataGridViewCell cell in dgvOutput.SelectedCells)
                    {
                        int rowIdx = cell.RowIndex;
                        if (rowIdx >= 0 && processedRows.Add(rowIdx))
                        {
                            AddCompanyRowFromGridRow(dgvOutput.Rows[rowIdx], selectedCompRows, seenCompanies);
                        }
                    }
                }

                // 4. Dùng CurrentRow
                if (selectedCompRows.Count == 0 && dgvOutput.CurrentRow != null)
                {
                    AddCompanyRowFromGridRow(dgvOutput.CurrentRow, selectedCompRows, seenCompanies);
                }

                if (selectedCompRows.Count == 0) return;

                // Lọc các công ty có cột LinkedInCo có dữ liệu
                var targets = new List<LinkedInCrawlTarget>();
                DataTable dtInput = dgvInput.DataSource as DataTable;

                foreach (var row in selectedCompRows)
                {
                    string companyName = row.Cells["CompanyName"].Value?.ToString() ?? "";
                    string linkedin = row.Cells["LinkedInCo"].Value?.ToString() ?? "";
                    if (string.IsNullOrEmpty(linkedin) || linkedin.ToUpper() == "N/A" || linkedin.Trim() == "")
                        continue;

                    string stt = row.Cells["STT"].Value?.ToString() ?? "";

                    DataRow inputRow = null;
                    if (dtInput != null)
                    {
                        foreach (DataRow r in dtInput.Rows)
                        {
                            string cName = r.Table.Columns.Count > 1 ? r[1]?.ToString() ?? "" : "";
                            if (string.Equals(cName.Trim(), companyName.Trim(), StringComparison.OrdinalIgnoreCase))
                            {
                                inputRow = r;
                                break;
                            }
                        }
                    }

                    targets.Add(new LinkedInCrawlTarget
                    {
                        CompanyName = companyName,
                        LinkedInCo = linkedin,
                        STT = stt,
                        InputRow = inputRow
                    });
                }

                if (targets.Count == 0)
                {
                    MessageBox.Show("Vui lòng chọn (hoặc click) các công ty có chứa dữ liệu LinkedIn Co!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                if (!btnStart.Enabled)
                {
                    MessageBox.Show("Đang chạy tiến trình khác, hãy dừng trước!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                _cts = new CancellationTokenSource();
                _activeDrivers = new ConcurrentBag<IWebDriver>();
                btnStart.Enabled = false; btnStop.Enabled = true;
                lblStatus.Text = $"Crawl LinkedIn cho {targets.Count} công ty...";

                try
                {
                    int tc = Math.Max(1, _threadCount);
                    var batches = new List<List<LinkedInCrawlTarget>>();
                    for (int t = 0; t < tc; t++) batches.Add(new List<LinkedInCrawlTarget>());
                    for (int i = 0; i < targets.Count; i++) batches[i % tc].Add(targets[i]);

                    var tasks = new List<Task>();
                    for (int t = 0; t < tc; t++)
                    {
                        int ti = t;
                        var batch = batches[t];
                        if (batch.Count > 0)
                            tasks.Add(Task.Run(async () => await ProcessLinkedInCrawlBatch(batch, ti, _cts.Token)));
                    }
                    await Task.WhenAll(tasks);
                }
                catch { }
                                finally
                {
                    btnStart.Enabled = true; btnStop.Enabled = false;
                    lblStatus.Text = "Finished LinkedIn Crawl";
                    UncheckOutputGrid();
                }
            };

            dgvOutput.ContextMenuStrip = ctxMenu;

            //dgvOutput.Columns["CompanyName"].AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
            //dgvOutput.Columns.Clear(); // Xóa sạch để tránh trùng lặp
            //dgvOutput.Columns.Add("STT", "STT");
            //dgvOutput.Columns.Add("CompanyName", "CompanyName");
            //dgvOutput.Columns.Add("CompanyAddress", "Address Co");
            //dgvOutput.Columns.Add("PhoneCo", "Phone Co");
            //dgvOutput.Columns.Add("Website", "Website");
            //dgvOutput.Columns.Add("EmailCo", "Email Co");
            //dgvOutput.Columns.Add("LinkedInCo", "LinkedIn Co");
            //dgvOutput.Columns.Add("FullNamePe", "Full Name");
            //dgvOutput.Columns.Add("PositionPe", "Position");
            //dgvOutput.Columns.Add("LinkedInPe", "LinkedIn Pe");
            //dgvOutput.Columns.Add("EmailPe", "Email Pe");
            //dgvOutput.Columns.Add("PhonePe", "Phone Pe");
            //if (!SystemInformation.TerminalServerSession)
            //{
            //    Type dgvType = dgvOutput.GetType();
            //    PropertyInfo pi = dgvType.GetProperty("DoubleBuffered", BindingFlags.Instance | BindingFlags.NonPublic);
            //    pi.SetValue(dgvOutput, true, null);
            //}

            //// Thêm cột nút bấm Unblock
            //DataGridViewButtonColumn btnUnblock = new DataGridViewButtonColumn();
            //btnUnblock.HeaderText = "Action";
            //btnUnblock.Text = "Unblock";
            //btnUnblock.UseColumnTextForButtonValue = true;
            //dgvOutput.Columns.Add(btnUnblock);

            //// ĐỊNH DẠNG KÍCH THƯỚC CỘT
            //dgvOutput.Columns["STT"].Width = 40;
            //dgvOutput.Columns["CompanyName"].Width = 200; // Tên công ty to ra

            //dgvOutput.RowHeadersVisible = false; // Ẩn cột thừa bên trái
        }

        /// <summary>
        /// Tô màu phân biệt row cha (công ty) và row con (nhân sự) - Dark Theme.
        /// </summary>
        private void dgvOutput_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
        {
            if (e.RowIndex < 0) return;
            var dgv = (DataGridView)sender;
            if (!dgv.Columns.Contains("RowType")) return;

            string rowType = dgv.Rows[e.RowIndex].Cells["RowType"].Value?.ToString() ?? "";

            if (rowType == "Company")
            {
                // Cha: nền tối đậm hơn, chữ sáng xanh, bold
                e.CellStyle.BackColor = Color.FromArgb(35, 38, 55);
                e.CellStyle.ForeColor = Color.FromArgb(137, 180, 250);
                e.CellStyle.SelectionBackColor = Color.FromArgb(69, 71, 90);
                e.CellStyle.SelectionForeColor = Color.White;
                e.CellStyle.Font = new Font(dgv.Font, FontStyle.Bold);
            }
            else if (rowType == "Person")
            {
                // Con: nền tối nhạt hơn, chữ sáng
                e.CellStyle.BackColor = Color.FromArgb(30, 30, 46);
                e.CellStyle.ForeColor = Color.FromArgb(205, 214, 244);
                e.CellStyle.SelectionBackColor = Color.FromArgb(69, 71, 90);
                e.CellStyle.SelectionForeColor = Color.White;
            }
            else
            {
                // Default
                e.CellStyle.BackColor = Color.FromArgb(30, 30, 46);
                e.CellStyle.ForeColor = Color.FromArgb(205, 214, 244);
                e.CellStyle.SelectionBackColor = Color.FromArgb(69, 71, 90);
                e.CellStyle.SelectionForeColor = Color.White;
            }

            // Ẩn nút UnBlock cho row không phải Person
            if (dgv.Columns.Contains("btnUnBlock") && dgv.Columns[e.ColumnIndex].Name == "btnUnBlock")
            {
                if (rowType != "Person")
                {
                    // Ẩn nút: nền + chữ cùng màu → vô hình
                    var bgColor = rowType == "Company" ? Color.FromArgb(35, 38, 55) : Color.FromArgb(30, 30, 46);
                    e.CellStyle.BackColor = bgColor;
                    e.CellStyle.ForeColor = bgColor;
                    e.CellStyle.SelectionBackColor = Color.FromArgb(69, 71, 90);
                    e.CellStyle.SelectionForeColor = Color.FromArgb(69, 71, 90);
                }
            }
        }

        private void dgvOutput_DataBindingComplete(object sender, DataGridViewBindingCompleteEventArgs e)
        {
            if (dgvOutput.Columns.Contains("chkSelect") && dgvOutput.Columns.Contains("RowType"))
            {
                foreach (DataGridViewRow row in dgvOutput.Rows)
                {
                    if (row.Cells["RowType"].Value?.ToString() == "Person")
                    {
                        row.Cells["chkSelect"].Value = false;
                        row.Cells["chkSelect"].ReadOnly = true;
                    }
                }
            }
        }

        private void dgvOutput_CellPainting(object sender, DataGridViewCellPaintingEventArgs e)
        {
            if (e.RowIndex >= 0 && e.ColumnIndex >= 0)
            {
                var dgv = (DataGridView)sender;
                if (dgv.Columns[e.ColumnIndex].Name == "chkSelect")
                {
                    if (dgv.Columns.Contains("RowType"))
                    {
                        string rowType = dgv.Rows[e.RowIndex].Cells["RowType"].Value?.ToString() ?? "";
                        if (rowType == "Person")
                        {
                            // Chỉ vẽ background mà không vẽ checkbox glyph
                            e.PaintBackground(e.CellBounds, true);
                            e.Handled = true;
                        }
                    }
                }
            }
        }

                private void AddCompanyFromGridRow(DataGridViewRow row, HashSet<string> companySet)
        {
            string rt = row.Cells["RowType"].Value?.ToString() ?? "";
            if (rt == "Company")
            {
                string cn = row.Cells["CompanyName"].Value?.ToString() ?? "";
                if (!string.IsNullOrEmpty(cn)) companySet.Add(cn);
            }
            else if (rt == "Person")
            {
                string companyId = row.Cells["CompanyID"].Value?.ToString() ?? "";
                bool found = false;
                if (!string.IsNullOrEmpty(companyId))
                {
                    foreach (DataGridViewRow r in dgvOutput.Rows)
                    {
                        if (r.Cells["RowType"].Value?.ToString() == "Company" &&
                            (r.Cells["CompanyID"].Value?.ToString() ?? "") == companyId)
                        {
                            string cn = r.Cells["CompanyName"].Value?.ToString() ?? "";
                            if (!string.IsNullOrEmpty(cn)) companySet.Add(cn);
                            found = true;
                            break;
                        }
                    }
                }
                if (!found)
                {
                    for (int ri = row.Index - 1; ri >= 0; ri--)
                    {
                        if (dgvOutput.Rows[ri].Cells["RowType"].Value?.ToString() == "Company")
                        {
                            string cn = dgvOutput.Rows[ri].Cells["CompanyName"].Value?.ToString() ?? "";
                            if (!string.IsNullOrEmpty(cn)) companySet.Add(cn);
                            break;
                        }
                    }
                }
            }
        }

        private void AddCompanyRowFromGridRow(DataGridViewRow row, List<DataGridViewRow> targetList, HashSet<string> seenSet)
        {
            string rt = row.Cells["RowType"].Value?.ToString() ?? "";
            if (rt == "Company")
            {
                string cn = row.Cells["CompanyName"].Value?.ToString() ?? "";
                if (!string.IsNullOrEmpty(cn) && seenSet.Add(cn))
                    targetList.Add(row);
            }
            else if (rt == "Person")
            {
                string companyId = row.Cells["CompanyID"].Value?.ToString() ?? "";
                bool found = false;
                if (!string.IsNullOrEmpty(companyId))
                {
                    foreach (DataGridViewRow r in dgvOutput.Rows)
                    {
                        if (r.Cells["RowType"].Value?.ToString() == "Company" &&
                            (r.Cells["CompanyID"].Value?.ToString() ?? "") == companyId)
                        {
                            string cn = r.Cells["CompanyName"].Value?.ToString() ?? "";
                            if (!string.IsNullOrEmpty(cn) && seenSet.Add(cn))
                                targetList.Add(r);
                            found = true;
                            break;
                        }
                    }
                }
                if (!found)
                {
                    for (int ri = row.Index - 1; ri >= 0; ri--)
                    {
                        if (dgvOutput.Rows[ri].Cells["RowType"].Value?.ToString() == "Company")
                        {
                            string cn = dgvOutput.Rows[ri].Cells["CompanyName"].Value?.ToString() ?? "";
                            if (!string.IsNullOrEmpty(cn) && seenSet.Add(cn))
                                targetList.Add(dgvOutput.Rows[ri]);
                            break;
                        }
                    }
                }
            }
        }

        private void GenerateColumnFilters()
        {
            flpColumns.Controls.Clear();
            // Thiết lập hướng xếp hàng ngang
            flpColumns.FlowDirection = FlowDirection.LeftToRight;
            flpColumns.WrapContents = true; // Tự xuống dòng nếu quá nhiều cột

            foreach (DataGridViewColumn col in dgvOutput.Columns)
            {
                // Không hiển thị bộ lọc cho các cột hệ thống
                if (col.Name == "CompanyID" || col.Name == "PersonID" || col.Name == "RowType") continue;

                // Ẩn các cột hệ thống không cần thiết phải hiện checkbox
                if (!col.Visible && string.IsNullOrEmpty(col.HeaderText)) continue;

                CheckBox chk = new CheckBox();
                chk.Text = col.HeaderText;
                chk.Tag = col.Name; // Lưu tên cột vào Tag để dễ truy vấn
                chk.Checked = col.Visible;
                chk.AutoSize = true;
                chk.ForeColor = System.Drawing.Color.FromArgb(205, 214, 244);
                chk.Margin = new Padding(5, 5, 15, 5); // Khoảng cách bên phải 15px để thưa hàng ngang

                chk.CheckedChanged += (s, ev) => {
                    dgvOutput.Columns[chk.Tag.ToString()].Visible = chk.Checked;
                    SaveColumnSettings(); // Lưu ngay lập tức mỗi khi thay đổi
                };

                flpColumns.Controls.Add(chk);
            }
        }
        private void SaveColumnSettings()
        {
            List<string> hiddenColumns = new List<string>();

            foreach (DataGridViewColumn col in dgvOutput.Columns)
            {
                if (!col.Visible)
                {
                    hiddenColumns.Add(col.Name); // Lưu tên các cột đang bị ẩn
                }
            }

            // Chuyển danh sách thành chuỗi phân cách bởi dấu phẩy và lưu lại
            Properties.Settings.Default.ColumnConfigs = string.Join(",", hiddenColumns);
            Properties.Settings.Default.Save();
        }
        private void LoadColumnSettings()
        {
            // Luôn ẩn các cột hệ thống
            if (dgvOutput.Columns.Contains("RowType")) dgvOutput.Columns["RowType"].Visible = false;
            if (dgvOutput.Columns.Contains("CompanyID")) dgvOutput.Columns["CompanyID"].Visible = false;
            if (dgvOutput.Columns.Contains("PersonID")) dgvOutput.Columns["PersonID"].Visible = false;

            string savedConfig = Properties.Settings.Default.ColumnConfigs;
            if (string.IsNullOrEmpty(savedConfig)) return;

            string[] hiddenColumns = savedConfig.Split(',');

            foreach (DataGridViewColumn col in dgvOutput.Columns)
            {
                // Bỏ qua các cột hệ thống để chúng luôn ẩn
                if (col.Name == "CompanyID" || col.Name == "PersonID" || col.Name == "RowType") continue;

                // Nếu tên cột nằm trong danh sách đã lưu, thì ẩn nó đi
                if (hiddenColumns.Contains(col.Name))
                {
                    col.Visible = false;
                }
                else
                {
                    col.Visible = true;
                }
            }
        }

        private string GetProxyFromDB()
        {
            DataTable dt = DatabaseHelper.ExecuteQuery("SELECT proxy FROM Config LIMIT 1");
            if (dt.Rows.Count > 0)
            {
                return dt.Rows[0]["proxy"].ToString().Trim();
            }
            return "";
        }
        /// <summary>
        /// Kiểm tra nhanh: công ty có tồn tại trong DB không (bất kể data đầy đủ hay không)
        /// Dùng khi upload file → load data ra grid output
        /// </summary>
        private string GetCompanyIdFromDB(string companyName)
        {
            string sql = @"SELECT ID FROM Company 
                           WHERE TRIM(CompanyName) = TRIM($name) COLLATE NOCASE
                              OR ($name LIKE '%' || CompanyName || '%' AND length(CompanyName) >= 4)
                              OR (CompanyName LIKE '%' || $name || '%' AND length($name) >= 4)
                           LIMIT 1";
            DataTable dt = DatabaseHelper.ExecuteQuery(sql, new[] { new SqliteParameter("$name", companyName) });
            if (dt.Rows.Count > 0)
                return dt.Rows[0]["ID"].ToString();
            return null;
        }

        private bool IsCompanyProcessedById(string companyId)
        {
            string sql = @"SELECT COUNT(*) FROM Company 
                           WHERE ID = $id
                           AND Website IS NOT NULL 
                           AND TRIM(Website) != '' 
                           AND UPPER(TRIM(Website)) != 'N/A'
                           AND (
                               (Address IS NOT NULL AND TRIM(Address) != '' AND UPPER(TRIM(Address)) != 'N/A')
                               OR (Phone IS NOT NULL AND TRIM(Phone) != '' AND UPPER(TRIM(Phone)) != 'N/A')
                               OR (Email IS NOT NULL AND TRIM(Email) != '' AND UPPER(TRIM(Email)) != 'N/A')
                               OR (Linkedin IS NOT NULL AND TRIM(Linkedin) != '' AND UPPER(TRIM(Linkedin)) != 'N/A')
                           )";
            DataTable dt = DatabaseHelper.ExecuteQuery(sql, new[] { new SqliteParameter("$id", companyId) });
            return dt.Rows.Count > 0 && Convert.ToInt32(dt.Rows[0][0]) > 0;
        }

        private bool IsCompanyInDB(string companyName)
        {
            return GetCompanyIdFromDB(companyName) != null;
        }

        /// <summary>
        /// Kiểm tra nghiêm ngặt: công ty đã có data đầy đủ chưa (Website + ít nhất 1 field khác)
        /// Dùng khi crawl → quyết định skip hay chạy lại
        /// </summary>
        private bool IsCompanyProcessed(string companyName)
        {
            // Chỉ skip nếu đã có Website thực sự VÀ có ít nhất 1 trường data khác (address/phone/email/linkedin)
            // Nếu chỉ có tên mà các trường khác đều trống → cho phép chạy lại
            string sql = @"SELECT COUNT(*) FROM Company 
                           WHERE TRIM(CompanyName) = TRIM($name) COLLATE NOCASE
                           AND Website IS NOT NULL 
                           AND TRIM(Website) != '' 
                           AND UPPER(TRIM(Website)) != 'N/A'
                           AND (
                               (Address IS NOT NULL AND TRIM(Address) != '' AND UPPER(TRIM(Address)) != 'N/A')
                               OR (Phone IS NOT NULL AND TRIM(Phone) != '' AND UPPER(TRIM(Phone)) != 'N/A')
                               OR (Email IS NOT NULL AND TRIM(Email) != '' AND UPPER(TRIM(Email)) != 'N/A')
                               OR (Linkedin IS NOT NULL AND TRIM(Linkedin) != '' AND UPPER(TRIM(Linkedin)) != 'N/A')
                           )";

            DataTable dt = DatabaseHelper.ExecuteQuery(sql, new[] { new SqliteParameter("$name", companyName) });

            if (dt.Rows.Count > 0 && Convert.ToInt32(dt.Rows[0][0]) > 0)
            {
                return true; // Đã chạy và có data thực sự → skip
            }
            return false; // Chưa chạy hoặc data trống → cho phép search lại
        }

        private void btnConfig_Click(object sender, EventArgs e)
        {
            FrmConfig frm = new FrmConfig();
            frm.Show();
        }

        private void chkRecaptcha_CheckedChanged(object sender, EventArgs e)
        {
            if (chkRecaptcha.Checked)
            {
                try
                {
                    DataTable dt = DatabaseHelper.ExecuteQuery("SELECT captcha_key FROM Config LIMIT 1");
                    string key = dt.Rows.Count > 0 ? dt.Rows[0]["captcha_key"]?.ToString()?.Trim() ?? "" : "";
                    if (string.IsNullOrEmpty(key))
                    {
                        chkRecaptcha.Checked = false;
                        MessageBox.Show("Chưa có 2Captcha API Key!\nVui lòng vào Config để thêm key.", "Thiếu cấu hình", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    }
                }
                catch
                {
                    chkRecaptcha.Checked = false;
                    MessageBox.Show("Chưa có 2Captcha API Key!\nVui lòng vào Config để thêm key.", "Thiếu cấu hình", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
        }

        private void chkUseProxy_CheckedChanged(object sender, EventArgs e)
        {
            if (chkUseProxy.Checked)
            {
                try
                {
                    DataTable dt = DatabaseHelper.ExecuteQuery("SELECT proxy FROM Config LIMIT 1");
                    string proxy = dt.Rows.Count > 0 ? dt.Rows[0]["proxy"]?.ToString()?.Trim() ?? "" : "";
                    if (string.IsNullOrEmpty(proxy))
                    {
                        chkUseProxy.Checked = false;
                        MessageBox.Show("Chưa có Proxy!\nVui lòng vào Config để thêm proxy.", "Thiếu cấu hình", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    }
                }
                catch
                {
                    chkUseProxy.Checked = false;
                    MessageBox.Show("Chưa có Proxy!\nVui lòng vào Config để thêm proxy.", "Thiếu cấu hình", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
        }
        private async void btnStart_Click1(object sender, EventArgs e)
        {
            _isRerunMode = false;
            _cts = new CancellationTokenSource();
            var token = _cts.Token;
            btnStart.Enabled = false;
            btnStop.Enabled = true;
            lblStatus.Text = "Running";
            InitGridOutput();
            DataTable dtInput = (DataTable)dgvInput.DataSource;

            await Task.Run(async () =>
            {
                ChromeDriverService service = ChromeDriverService.CreateDefaultService();
                service.HideCommandPromptWindow = true;
                var options = new ChromeOptions();
                // Cho phép tải hình ảnh (đè lên cấu hình lưu cũ trong Profile)
                options.AddUserProfilePreference("profile.default_content_setting_values.images", 1);
                // Vô hiệu hóa thông báo để tránh làm phiền
                options.AddUserProfilePreference("profile.default_content_setting_values.notifications", 2);
                options.BinaryLocation = @"E:\DEV\Chrome_v121\chrome\chrome.exe";
                options.AddArgument(@"user-data-dir=E:\DEV\Chrome_v121\UserData_Tool");
                options.AddArgument("--no-sandbox");
                options.AddArgument("--disable-dev-shm-usage");
                options.AddArgument("--remote-allow-origins=*");
                options.AddExcludedArgument("enable-automation");
                //options.AddArgument("--incognito");
                options.AddArgument("--disable-blink-features=AutomationControlled");
                //options.AddArgument("--headless");
                driver = new ChromeDriver(service, options, TimeSpan.FromSeconds(120)); // Khởi tạo biến toàn cục
                driver.Manage().Timeouts().PageLoad = TimeSpan.FromSeconds(60);
                try
                {
                    ((IJavaScriptExecutor)driver).ExecuteScript("Object.defineProperty(navigator, 'webdriver', {get: () => undefined})");
                    int sttCount = 1; // Biến đếm số thứ tự cho lưới bên phải
                    foreach (DataRow row in dtInput.Rows)
                    {
                        string CompanyName = "";
                        string CompanyAddress = "";
                        string Industry = "";
                        string PhoneCo = "";
                        string Website = "";
                        string EmailCo = "";
                        string LinkedInCo = "";
                        string FullNamePe = "";
                        string PositionPe = "";
                        string LinkedInPe = "";
                        string EmailPe = "";
                        string PhonePe = "";
                        if (_cts.Token.IsCancellationRequested) break;

                        this.Invoke(new Action(() => lblStatus.Text = $"Processing"));
                        CompanyName = row["Shipper"].ToString();
                        // BƯỚC 1: TẠO DÒNG TRỐNG TRÊN GRID ENRICH TRƯỚC
                        int rowIndex = 0;

                        string prompt = $"tìm cho tôi thông tin của công ty {CompanyName}. " +
                            "trả về thông tin của công ty gồm: website, địa chỉ, phone, email, linkedin, " +
                            "và tất cả các thông tin lãnh đạo và quản lý chủ chốt gồm: tên, chức vụ, linkedin, email, phone. " +
                            "QUAN TRỌNG: Nếu là số Fax, hãy thêm tiền tố 'Fax: ' vào trước số đó (ví dụ: '028... - Fax: 028...'). " +
                            "linkedin trả lại full đường dẫn không dấu link. ngoài ra không trả ra gì nữa";

                        // BƯỚC ENCODE: Chuyển tiếng Việt có dấu và ký tự đặc biệt sang dạng %C3%AC, %20...
                        string encodedPrompt = Uri.EscapeDataString(prompt);

                        // Ghép vào URL chuẩn của Google với tham số udm=50 (AI Overview)
                        string fullUrl = $"https://www.google.com/search?q={encodedPrompt}&udm=50";
                        driver.Navigate().GoToUrl(fullUrl);
                        Thread.Sleep(8000);
                        string fullPageText = driver.FindElement(By.TagName("body")).Text;
                        ParseGoogleAIContent(fullPageText, rowIndex);
                        //prompt = "Trích xuất thông tin từ text sau: \n***" + fullPageText + "***. \nTrả về JSON theo đúng schema: { 'company': {'website': '', 'address': '', 'phone': '', 'email': '', 'linkedin': ''}, 'leaders': [{'name': '', 'position': '', 'linkedin': '', 'email': '', 'phone': ''}] }";
                        prompt = "Trích xuất thông tin từ text sau: \n***" + fullPageText + "***. nếu dữ liệu Đang cập nhật hay không có hay không có thông tin công khai về công ty hoặc nhân sự thì trả về N/A. Lấy TẤT CẢ số điện thoại và email tìm thấy, cách nhau bằng dấu ' - ' \nTrả về JSON theo đúng schema: { 'companies': [{ 'name': '', 'website': '', 'address': '', 'phone': 'số1 - số2', 'email': 'mail1 - mail2', 'linkedin': '', 'leaders': [{ 'name': '', 'position': '', 'linkedin': '', 'email': '', 'phone': '' }] }] }";
                        string result = "";
                        try
                        {
                            result = await CallGeminiAPI(prompt);
                            dynamic data = JsonConvert.DeserializeObject(result);
                            if (data != null)
                            {
                                foreach (var item in data.companies)
                                {
                                    if (token.IsCancellationRequested) break;

                                    // LƯU VÀO DATABASE
                                                                          var crawlRes = await SaveCrawlResult((object)item, CompanyName);
                                     CompanyAddress = item.address ?? "";
                                     Industry = item.industry ?? "";
                                     PhoneCo = item.phone ?? "";                                    
                                     Website = item.website ?? "";
                                     EmailCo = item.email ?? "";
                                     LinkedInCo = item.linkedin ?? "";
                                     if (item.leaders != null)
                                     {
                                         // Người đầu tiên để điền vào dòng hiện tại
                                         if (item.leaders.Count > 0)
                                         {
                                             FullNamePe = item.leaders[0].name ?? "";
                                             PositionPe = item.leaders[0].position ?? "";
                                             LinkedInPe = item.leaders[0].linkedin ?? "";
                                             EmailPe = item.leaders[0].email ?? "";
                                             PhonePe = item.leaders[0].phone ?? "";
                                         }
                                     }

                                     // --- ROW CHA: Thông tin công ty ---
                                     DataRow dr = mydata.NewRow();
                                     dr["STT"] = sttCount++;
                                     dr["CompanyName"] = CompanyName;
                                     dr["CompanyAddress"] = CompanyAddress;
                                     dr["Industry"] = Industry;
                                     dr["PhoneCo"] = PhoneCo;
                                     dr["Website"] = Website;
                                     dr["EmailCo"] = EmailCo;
                                     dr["LinkedInCo"] = LinkedInCo;
                                     dr["CompanyID"] = crawlRes.companyId;
                                     dr["RowType"] = "Company";
                                    this.Invoke(new Action(() =>
                                     {
                                         mydata.Rows.Add(dr);
                                         row["Status"] = "Completed";
                                         row["Select"] = false;
                                     }));

                                    // --- ROW CON: Nhân sự ---
                                    if (item.leaders != null && item.leaders.Count > 0)
                                    {
                                        for (int i = 0; i < item.leaders.Count; i++)
                                        {
                                            DataRow drPerson = mydata.NewRow();
                                            drPerson["STT"] = "  ├ " + (i + 1);
                                            drPerson["CompanyName"] = "";
                                            drPerson["FullNamePe"] = item.leaders[i].name ?? "";
                                            drPerson["PositionPe"] = item.leaders[i].position ?? "";
                                            drPerson["LinkedInPe"] = item.leaders[i].linkedin ?? "";
                                            drPerson["EmailPe"] = item.leaders[i].email ?? "";
                                            drPerson["PhonePe"] = item.leaders[i].phone ?? "";
                                            drPerson["RowType"] = "Person";
                                            this.Invoke(new Action(() =>
                                            {
                                                mydata.Rows.Add(drPerson);
                                            }));
                                        }
                                    }
                                }
                            }
                            this.Invoke(new Action(() =>
                            {
                                row["Status"] = "Completed";
                            }));
                        }
                        catch { }
                        await Task.Delay(2000, token);
                    }
                }
                catch (Exception ex) { if (!token.IsCancellationRequested) this.Invoke(new Action(() => MessageBox.Show(ex.Message))); }
                finally { QuitDriver(); }
            }, token);

            lblStatus.Text = token.IsCancellationRequested ? "Stopped" : "Finished";
            btnStart.Enabled = true;
            btnStop.Enabled = false;
        }
        private void btnExport_Click(object sender, EventArgs e)
        {
            SaveFileDialog sfd = new SaveFileDialog();
            sfd.Filter = "Excel Workbook|*.xlsx";
            sfd.FileName = "KetQuaCrawl_" + DateTime.Now.ToString("yyyyMMdd_HHmmss");

            if (sfd.ShowDialog() == DialogResult.OK)
            {
                try
                {
                    string filePath = sfd.FileName;
                    using (var workbook = new XLWorkbook())
                    {
                        var worksheet = workbook.Worksheets.Add("Data");

                        string[] headerTexts = {
                            "STT", "Tên Công Ty", "Địa Chỉ Công Ty", "Ngành", "SĐT Công Ty", "Website",
                            "Email Công Ty", "LinkedIn Công Ty", "Họ Tên Nhân Sự", "Chức Vụ",
                            "LinkedIn Cá Nhân", "Email Cá Nhân", "SĐT Cá Nhân"
                        };

                        for (int i = 0; i < headerTexts.Length; i++)
                        {
                            var cell = worksheet.Cell(1, i + 1);
                            cell.Value = headerTexts[i];
                            cell.Style.Font.Bold = true;
                            cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#4F81BD");
                            cell.Style.Font.FontColor = XLColor.White;
                            cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                        }

                        int rowIndex = 2;
                        int companySTT = 0;
                        int companyStartRow = -1;

                        foreach (DataRow row in mydata.Rows)
                        {
                            string rowType = row.Table.Columns.Contains("RowType") ? (row["RowType"]?.ToString() ?? "") : "";
                            string sttVal = row["STT"]?.ToString() ?? "";
                            bool isChild = rowType == "Person" || sttVal.Contains("├");

                            if (!isChild)
                            {
                                if (companyStartRow > 0 && rowIndex - 1 > companyStartRow)
                                {
                                    for (int c = 1; c <= 8; c++)
                                    {
                                        var range = worksheet.Range(companyStartRow, c, rowIndex - 1, c);
                                        range.Merge();
                                        range.Style.Alignment.Vertical = XLAlignmentVerticalValues.Top;
                                    }
                                }

                                companySTT++;
                                companyStartRow = rowIndex;

                                worksheet.Cell(rowIndex, 1).Value = sttVal != "" ? sttVal : companySTT.ToString();
                                worksheet.Cell(rowIndex, 2).Value = row["CompanyName"]?.ToString() ?? "";
                                worksheet.Cell(rowIndex, 3).Value = row["CompanyAddress"]?.ToString() ?? "";
                                worksheet.Cell(rowIndex, 4).Value = row["Industry"]?.ToString() ?? "";
                                worksheet.Cell(rowIndex, 5).Value = row["PhoneCo"]?.ToString() ?? "";
                                worksheet.Cell(rowIndex, 6).Value = row["Website"]?.ToString() ?? "";
                                worksheet.Cell(rowIndex, 7).Value = row["EmailCo"]?.ToString() ?? "";
                                worksheet.Cell(rowIndex, 8).Value = row["LinkedInCo"]?.ToString() ?? "";

                                var companyRange = worksheet.Range(rowIndex, 1, rowIndex, 13);
                                companyRange.Style.Fill.BackgroundColor = XLColor.FromHtml("#E8F0FE");
                                companyRange.Style.Font.Bold = true;
                            }
                            else
                            {
                                worksheet.Cell(rowIndex, 9).Value = row["FullNamePe"]?.ToString() ?? "";
                                worksheet.Cell(rowIndex, 10).Value = row["PositionPe"]?.ToString() ?? "";
                                worksheet.Cell(rowIndex, 11).Value = row["LinkedInPe"]?.ToString() ?? "";
                                worksheet.Cell(rowIndex, 12).Value = row["EmailPe"]?.ToString() ?? "";
                                worksheet.Cell(rowIndex, 13).Value = row["PhonePe"]?.ToString() ?? "";
                            }

                            for (int c = 1; c <= 13; c++)
                                worksheet.Cell(rowIndex, c).Style.Border.OutsideBorder = XLBorderStyleValues.Thin;

                            rowIndex++;
                        }

                        if (companyStartRow > 0 && rowIndex - 1 > companyStartRow)
                        {
                            for (int c = 1; c <= 8; c++)
                            {
                                var range = worksheet.Range(companyStartRow, c, rowIndex - 1, c);
                                range.Merge();
                                range.Style.Alignment.Vertical = XLAlignmentVerticalValues.Top;
                            }
                        }

                        worksheet.Columns().AdjustToContents();
                        workbook.SaveAs(sfd.FileName);

                        var result = MessageBox.Show("Export Excel thành công! Bạn có muốn mở file ngay không?",
                                                    "Thông báo",
                                                    MessageBoxButtons.YesNo,
                                                    MessageBoxIcon.Question);

                        if (result == DialogResult.Yes)
                        {
                            Process.Start(new ProcessStartInfo(filePath) { UseShellExecute = true });
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Lỗi khi export: " + ex.Message);
                }
            }
        }

        public class PersonModel
        {
            public string FullName { get; set; }
            public string Position { get; set; }
            public string Linkedin { get; set; }
            public string Email { get; set; }
            public string Phone { get; set; }
        }

        public class CompanyModel
        {
            public string ID { get; set; } // Sẽ sinh Guid nếu là tạo mới
            public string CompanyName { get; set; }
            public string Address { get; set; }
            public string Website { get; set; }
            public string Email { get; set; }
            public string Linkedin { get; set; }
            public string Phone { get; set; }
            public List<PersonModel> Leaders { get; set; } = new List<PersonModel>();
        }

        public class SaleQLResult
        {
            public string Email { get; set; }
            public string Phone { get; set; }
        }

        private void btnTotalData_Click(object sender, EventArgs e)
        {
            FrmData frm = new FrmData();
            frm.Show();
        }

        /// <summary>
        /// Nếu LinkedIn đang hiện trang security check / challenge, chờ user giải cho xong rồi mới chạy tiếp.
        /// Cứ 3 giây poll 1 lần, hiển thị đếm ngược trên lblStatus.
        /// </summary>
        private async Task WaitForLinkedInCheckpoint(CancellationToken token, IWebDriver localDriver)
        {
            int waited = 0;
            while (!token.IsCancellationRequested)
            {
                string currentUrl = "";
                try { currentUrl = localDriver.Url; } catch { break; }

                bool isChallenge = currentUrl.Contains("linkedin.com/checkpoint") ||
                                   currentUrl.Contains("linkedin.com/challenge") ||
                                   currentUrl.Contains("linkedin.com/security");

                if (!isChallenge) break;

                waited += 3;
                this.Invoke(new Action(() =>
                    lblStatus.Text = $"⏳ LinkedIn Security Check – Vui lòng hoàn thành trên trình duyệt... ({waited}s)"
                ));

                await Task.Delay(3000, token);
            }
        }

        /// <summary>
        /// Phát hiện Error 1200 (Cloudflare rate limit) sau mỗi navigate LinkedIn.
        /// Nếu bị rate limit: chờ 60 giây rồi tự reload lại URL đó, lặp cho đến khi hết.
        /// </summary>
        private async Task WaitForLinkedInRateLimit(CancellationToken token, IWebDriver localDriver)
        {
            int attempt = 0;
            while (!token.IsCancellationRequested)
            {
                bool isRateLimited = false;
                string retryUrl = "";
                try
                {
                    string src = localDriver.PageSource ?? "";
                    retryUrl = localDriver.Url;
                    isRateLimited = src.Contains("error-1200") ||
                                    src.Contains("rate limited") ||
                                    src.Contains("Error 1200") ||
                                    src.Contains("Too many requests");
                }
                catch { break; }

                if (!isRateLimited) break;

                attempt++;
                // Chờ 60 giây, hiển đếm ngược mỗi giây
                for (int sec = 60; sec > 0 && !token.IsCancellationRequested; sec--)
                {
                    int s = sec;
                    this.Invoke(new Action(() =>
                        lblStatus.Text = $"🛑 LinkedIn Rate Limit (Error 1200) – Chờ {s}s rồi thử lại... (lần {attempt})"
                    ));
                    await Task.Delay(1000, token);
                }

                // Reload lại URL vừa bị block
                try
                {
                    if (!string.IsNullOrEmpty(retryUrl))
                        localDriver.Navigate().GoToUrl(retryUrl);
                    await Task.Delay(3000, token);
                }
                catch { break; }
            }
        }

        public async Task GetPersionFromLinkedInCompany(string CompanyName,string curCompanyname, string linkedInUrl, CancellationToken token, IWebDriver localDriver)
        {
            try
            {
                this.Invoke(new Action(() => lblStatus.Text = "Searching LinkedIn for Company"));
                
                // 1. Tải cấu hình LinkedIn từ DB
                string liUser = "";
                string liPass = "";
                string liCookie = "";
                string liKeywords = "manager|director|vp|president|chief|founder|owner|leader|executive|ceo|cto|cfo|coo|head|lead";
                DataTable dtConfig = DatabaseHelper.ExecuteQuery("SELECT linkedin_id, linkedin_pass, linkedin_cookies, linkedin_keywords FROM Config LIMIT 1");
                if (dtConfig.Rows.Count > 0)
                {
                    liUser = dtConfig.Rows[0]["linkedin_id"].ToString();
                    liPass = dtConfig.Rows[0]["linkedin_pass"].ToString();
                    liCookie = dtConfig.Rows[0]["linkedin_cookies"].ToString();
                    var kwObj = dtConfig.Rows[0]["linkedin_keywords"];
                    if (kwObj != null && !string.IsNullOrEmpty(kwObj.ToString()))
                        liKeywords = kwObj.ToString();
                }

                if (string.IsNullOrEmpty(liUser) || string.IsNullOrEmpty(liPass))
                {
                    this.Invoke(new Action(() => lblStatus.Text = "Skipped LinkedIn: No ID/Pass in Config."));
                    return;
                }

                string[] keywords = liKeywords.Split(new[] { '|' }, StringSplitOptions.RemoveEmptyEntries);

                bool isLoggedIn = false;

                // ── BƯỚC 1: Kiểm tra browser đã login LinkedIn chưa (tận dụng session cũ) ──
                this.Invoke(new Action(() => lblStatus.Text = "Checking LinkedIn session..."));
                try
                {
                    // Chỉ navigate nếu chưa ở trang LinkedIn (tránh reload không cần thiết)
                    if (!localDriver.Url.Contains("linkedin.com"))
                        localDriver.Navigate().GoToUrl("https://www.linkedin.com/feed/");
                    else
                        localDriver.Navigate().GoToUrl("https://www.linkedin.com/feed/");

                    await Task.Delay(2500, token);
                    await WaitForLinkedInRateLimit(token, localDriver);
                    await WaitForLinkedInCheckpoint(token, localDriver);

                    if (localDriver.Url.Contains("linkedin.com/feed") || localDriver.FindElements(By.Id("global-nav")).Count > 0)
                    {
                        isLoggedIn = true;
                        this.Invoke(new Action(() => lblStatus.Text = "LinkedIn: already logged in ✔"));
                    }
                }
                catch { }

                // ── BƯỚC 2: Nếu chưa login → thử dùng cookie đã lưu ──
                if (!isLoggedIn && !string.IsNullOrEmpty(liCookie))
                {
                    this.Invoke(new Action(() => lblStatus.Text = "LinkedIn: trying saved cookies..."));
                    try
                    {
                        localDriver.Navigate().GoToUrl("https://www.linkedin.com");
                        await Task.Delay(1000, token);

                        JObject cookieObj = JObject.Parse(liCookie);
                        JArray cookies = (JArray)cookieObj["cookies"];
                        foreach (var c in cookies)
                        {
                            var name   = c["name"]?.ToString();
                            var value  = c["value"]?.ToString();
                            var domain = c["domain"]?.ToString();
                            var path   = c["path"]?.ToString();

                            DateTime? expiry = null;
                            if (c["expirationDate"] != null)
                            {
                                double expDouble = (double)c["expirationDate"];
                                expiry = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc).AddSeconds(expDouble);
                            }

                            if (!string.IsNullOrEmpty(name) && !string.IsNullOrEmpty(value))
                            {
                                try
                                {
                                    var seleniumCookie = new OpenQA.Selenium.Cookie(name, value, domain, path, expiry);
                                    localDriver.Manage().Cookies.AddCookie(seleniumCookie);
                                }
                                catch { }
                            }
                        }

                        localDriver.Navigate().GoToUrl("https://www.linkedin.com/feed/");
                        await Task.Delay(3000, token);
                        await WaitForLinkedInRateLimit(token, localDriver);
                        await WaitForLinkedInCheckpoint(token, localDriver);

                        if (localDriver.Url.Contains("linkedin.com/feed") || localDriver.FindElements(By.Id("global-nav")).Count > 0)
                        {
                            isLoggedIn = true;
                            this.Invoke(new Action(() => lblStatus.Text = "LinkedIn: cookie login OK ✔"));
                        }
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine("Lỗi cookie LinkedIn: " + ex.Message);
                    }
                }

               if (!isLoggedIn)
                {
                    this.Invoke(new Action(() => lblStatus.Text = "Logging in to LinkedIn..."));
                    localDriver.Navigate().GoToUrl("https://www.linkedin.com/login");
                    
                    // Chờ tối đa 10 giây để trang login tải xong và có ô username hoặc đã tự động đăng nhập
                    try
                    {
                        var loginWait = new WebDriverWait(localDriver, TimeSpan.FromSeconds(10));
                        loginWait.Until(d => d.FindElements(By.Id("username")).Count > 0 
                                            || d.FindElements(By.CssSelector("input[type='email'], input[autocomplete*='username']")).Count > 0
                                            || d.Url.Contains("linkedin.com/feed")
                                            || d.FindElements(By.XPath("//p[contains(text(), 'another account')] | //a[contains(@href, 'login') or contains(text(), 'Sign in')] | //button[contains(text(), 'another account')]")).Count > 0);
                    }
                    catch { }
                    
                    var txtUser = localDriver.FindElements(By.Id("username"));
                    if (txtUser.Count == 0)
                    {
                        txtUser = localDriver.FindElements(By.CssSelector("input[type='email'], input[autocomplete*='username']"));
                    }
                    
                    // Xử lý trường hợp có màn hình "Welcome Back" chọn profile thay vì ô nhập user/pass
                    if (txtUser.Count == 0)
                    {
                        // Thỉnh thoảng LinkedIn ẩn 1 phần email (t******@gmail.com) trên danh sách Welcome Back, nên click thẳng vào "Sign in using another account"
                        var btnAnotherAccount = localDriver.FindElements(By.XPath("//p[contains(text(), 'another account')] | //a[contains(@href, 'login') or contains(text(), 'Sign in')] | //button[contains(text(), 'another account')] | //div[contains(@class, 'sign-in') or contains(@class, 'account')]"));
                        if (btnAnotherAccount.Count > 0)
                        {
                            try
                            {
                                btnAnotherAccount[0].Click();
                            }
                            catch { }
                            await Task.Delay(2000, token);
                            txtUser = localDriver.FindElements(By.CssSelector("input[type='email'], input[autocomplete*='username']"));
                        }
                    }

                    if (txtUser.Count > 0)
                    {
                        try
                        {
                            // Clear field bằng JS + Ctrl+A để tránh nối thêm vào giá trị cũ
                            ((IJavaScriptExecutor)localDriver).ExecuteScript("arguments[0].value = '';", txtUser[0]);
                            txtUser[0].Click();
                            txtUser[0].SendKeys(OpenQA.Selenium.Keys.Control + "a");
                            txtUser[0].SendKeys(OpenQA.Selenium.Keys.Delete);
                            txtUser[0].SendKeys(liUser);

                            IWebElement txtP = null;
                            var passEls = localDriver.FindElements(By.Id("password"));
                            if (passEls.Count > 0)
                            {
                                txtP = passEls[0];
                            }
                            else
                            {
                                passEls = localDriver.FindElements(By.CssSelector("input[type='password'], input[autocomplete*='password']"));
                                if (passEls.Count > 0) txtP = passEls[0];
                            }

                            if (txtP != null)
                            {
                                ((IJavaScriptExecutor)localDriver).ExecuteScript("arguments[0].value = '';", txtP);
                                txtP.Click();
                                txtP.SendKeys(OpenQA.Selenium.Keys.Control + "a");
                                txtP.SendKeys(OpenQA.Selenium.Keys.Delete);
                                txtP.SendKeys(liPass);
                            }

                            IWebElement btnSubmit = null;
                            var submitBtns = localDriver.FindElements(By.CssSelector("button[type='submit']"));
                            if (submitBtns.Count > 0)
                            {
                                btnSubmit = submitBtns[0];
                            }
                            else
                            {
                                submitBtns = localDriver.FindElements(By.XPath("//button[contains(., 'Sign in') or contains(., 'Sign In') or contains(., 'Đăng nhập') or contains(., 'đăng nhập')]"));
                                if (submitBtns.Count > 0) btnSubmit = submitBtns[submitBtns.Count-1];
                            }

                            if (btnSubmit != null)
                            {
                                try
                                {
                                    btnSubmit.Click();
                                }
                                catch
                                {
                                    ((IJavaScriptExecutor)localDriver).ExecuteScript("arguments[0].click();", btnSubmit);
                                }
                            }
                            await Task.Delay(5000, token);
                        }
                        catch { }
                        // Nếu LinkedIn yêu cầu rate limit hoặc security check
                        await WaitForLinkedInRateLimit(token, localDriver);
                        await WaitForLinkedInCheckpoint(token, localDriver);

                        // Check if login successful
                        if (localDriver.Url.Contains("linkedin.com/feed") || localDriver.FindElements(By.Id("global-nav")).Count > 0)
                        {
                            isLoggedIn = true;
                            // Save new cookies back to DB
                            var allCookies = localDriver.Manage().Cookies.AllCookies;
                            JArray cookieArray = new JArray();
                            foreach (var c in allCookies)
                            {
                                JObject cObj = new JObject();
                                cObj["name"] = c.Name;
                                cObj["value"] = c.Value;
                                cObj["domain"] = c.Domain;
                                cObj["path"] = c.Path;
                                cObj["secure"] = c.Secure;
                                cObj["httpOnly"] = c.IsHttpOnly;
                                if (c.Expiry.HasValue)
                                {
                                    cObj["expirationDate"] = (c.Expiry.Value.ToUniversalTime() - new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)).TotalSeconds;
                                }
                                cookieArray.Add(cObj);
                            }
                            JObject finalCookieObj = new JObject();
                            finalCookieObj["url"] = "https://www.linkedin.com";
                            finalCookieObj["cookies"] = cookieArray;
                            string newCookieStr = finalCookieObj.ToString(Newtonsoft.Json.Formatting.None);

                            DatabaseHelper.ExecuteNonQuery("UPDATE Config SET linkedin_cookies = $cookie", new[] { new SqliteParameter("$cookie", newCookieStr) });
                        }
                    }
                }

                if (!isLoggedIn)
                {
                    this.Invoke(new Action(() => lblStatus.Text = "LinkedIn login failed. Skipping."));
                    return; 
                }

                // === BƯỚC 2: Có LinkedIn URL → vào thẳng /people/. Không có → search tên ===
                bool hasDirectUrl = !string.IsNullOrEmpty(linkedInUrl)
                                    && linkedInUrl != "N/A"
                                    && linkedInUrl.Contains("linkedin.com/company/");

                string compUrlResolved = ""; // URL công ty đã xác định (dùng cho /people/)

                if (hasDirectUrl)
                {
                    // Chuẩn hoá URL
                    compUrlResolved = linkedInUrl.Split('?')[0].TrimEnd('/');
                    this.Invoke(new Action(() => lblStatus.Text = $"LinkedIn direct: {compUrlResolved}"));

                    string checkKeyword = keywords.Length > 0 ? keywords[0].Trim() : "manager";
                    string peopleUrlDirect = compUrlResolved + "/people/?keywords=" + Uri.EscapeDataString(checkKeyword);
                    localDriver.Navigate().GoToUrl(peopleUrlDirect);
                    await Task.Delay(5000, token);
                    await WaitForLinkedInRateLimit(token, localDriver);
                    await WaitForLinkedInCheckpoint(token, localDriver);

                    // Nếu bị redirect ra ngoài /people → fallback sang search
                    if (!localDriver.Url.Contains("/company/"))
                    {
                        hasDirectUrl = false; // fallback
                        Console.WriteLine($"[LinkedIn] Direct URL failed, falling back to search: {linkedInUrl}");
                    }
                }

                // Khai báo wait TRƯỚC if để dùng chung ở cả 2 nhánh (direct URL + search)
                WebDriverWait wait = new WebDriverWait(localDriver, TimeSpan.FromSeconds(10));

                if (!hasDirectUrl)
                {
                // 2. Tìm kiếm theo tên công ty
                string urlSearch = $"https://www.linkedin.com/search/results/companies/?keywords={Uri.EscapeDataString(curCompanyname)}&origin=SWITCH_SEARCH_VERTICAL";
                localDriver.Navigate().GoToUrl(urlSearch);

                await Task.Delay(2000, token);
                await WaitForLinkedInRateLimit(token, localDriver);
                await WaitForLinkedInCheckpoint(token, localDriver);
                // --- Xử lý chặn Welcome Back / Sign in as ... ---
                try
                {
                    this.Invoke(new Action(() => lblStatus.Text = "Checking LinkedIn Welcome..."));
                    
                    // Trường hợp 1: Màn hình Welcome Back có iframe của Google One Tap (ảnh 1)
                    var iframes = localDriver.FindElements(By.TagName("iframe"));
                    bool isIframeClicked = false;
                    foreach (var iframe in iframes)
                    {
                        if (iframe.GetAttribute("src").Contains("smartlock") || iframe.GetAttribute("title").Contains("Sign in"))
                        {
                            localDriver.SwitchTo().Frame(iframe);
                            var googleBtn = localDriver.FindElements(By.CssSelector("[role='button'], .nsm7Bb-HzV7m-LgbsSe"));
                            if (googleBtn.Count > 0)
                            {
                                googleBtn[0].Click();
                                isIframeClicked = true;
                                await Task.Delay(3000, token);
                                break;
                            }
                            localDriver.SwitchTo().DefaultContent();
                        }
                    }

                    localDriver.SwitchTo().DefaultContent(); // Đảm bảo về lại trang chính

                    // Trường hợp 2: Màn hình Welcome Back Native của LinkedIn (ảnh 2)
                    if (!isIframeClicked)
                    {
                        var nativeBtns = localDriver.FindElements(By.XPath("//button[contains(., 'Sign in') or contains(., '@gmail.com')] | //div[@role='button' and (contains(., 'Sign in') or contains(., '@gmail.com'))] | //div[contains(@class, 'sign-in') or contains(@class, 'account')]"));
                        if (nativeBtns.Count > 0)
                        {
                            nativeBtns[0].Click();
                            await Task.Delay(3000, token);
                        }
                    }

                    // Đảm bảo vẫn ở đúng trang search
                    if (!localDriver.Url.Contains("search/results/companies"))
                    {
                        localDriver.Navigate().GoToUrl(urlSearch);
                        await Task.Delay(2000, token);
                    }
                }
                catch { 
                    localDriver.SwitchTo().DefaultContent(); 
                }
                // ----------------------------------------------
                

                try
                {
                    // Chờ thẻ search result của LinkedIn hiện ra
                    // Dùng nhiều selector phối hợp vì LinkedIn hay thay đổi giao diện (VD: app-aware-link)
                    var elements = wait.Until(d => d.FindElements(By.CssSelector(".entity-result__title-text > a, a[href*='/company/']")));
                    
                    IWebElement targetComp = null;
                    foreach (var el in elements)
                    {
                        if (token.IsCancellationRequested) return;
                        
                        // Loại bỏ các link phụ của company như /life, /about, /people
                        string href = el.GetAttribute("href") ?? "";
                        if (href.Contains("/company/") && !href.Contains("/life") && !href.Contains("/people") && !href.Contains("/about"))
                        {
                            string text = el.Text.Trim();
                            if (!string.IsNullOrEmpty(text))
                            {
                                // Nếu chưa có target thì lưu tạm el đầu tiên (để fallback nếu không match 100%)
                                if (targetComp == null)
                                {
                                    targetComp = el;
                                }

                                // Kiểm tra nếu tên giống CompanyName (ưu tiên chọn chính xác)
                                if (text.IndexOf(curCompanyname, StringComparison.OrdinalIgnoreCase) >= 0 ||
                                    CompanyName.IndexOf(text, StringComparison.OrdinalIgnoreCase) >= 0)
                                {
                                    targetComp = el;
                                    break;
                                }
                            }
                        }
                    }

                    if (targetComp != null)
                    {
                        string compUrl = CleanLinkedInUrl(targetComp.GetAttribute("href").Split('?')[0].TrimEnd('/'));
                        compUrlResolved = compUrl;

                        // Cập nhật LinkedIn URL vào DB
                        string companyIdSearch = "";
                        string checkCompSql = "SELECT ID FROM Company WHERE CompanyName = $name COLLATE NOCASE LIMIT 1";
                        DataTable dtComp = DatabaseHelper.ExecuteQuery(checkCompSql, new[] { new SqliteParameter("$name", CompanyName) });
                        if (dtComp.Rows.Count > 0)
                        {
                            companyIdSearch = dtComp.Rows[0]["ID"].ToString();
                            DatabaseHelper.ExecuteNonQuery("UPDATE Company SET Linkedin = $link WHERE ID = $id",
                                new[] { new SqliteParameter("$link", compUrl), new SqliteParameter("$id", companyIdSearch) });
                        }
                        else
                        {
                            companyIdSearch = Guid.NewGuid().ToString();
                            string now2 = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                            DatabaseHelper.ExecuteNonQuery(
                                "INSERT INTO Company (ID, CompanyName, Linkedin, Address, Website, Industry, Email, Phone, LastUpdate) VALUES ($id, $name, $link, '', '', '', '', '', $date)",
                                new[] { new SqliteParameter("$id", companyIdSearch), new SqliteParameter("$name", CompanyName), new SqliteParameter("$link", compUrl), new SqliteParameter("$date", now2) });
                        }
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine($"Không tìm thấy trên LinkedIn Company: {CompanyName}");
                    }
                } // end try - search block
                catch (WebDriverTimeoutException)
                {
                    System.Diagnostics.Debug.WriteLine("Timeout khi chờ kết quả LinkedIn");
                }
                } // end of if (!hasDirectUrl)

                // === BƯỚC 3 CHUNG: Nếu tìm được URL công ty → vào /people/ tìm các vị trí mong muốn ===
                if (!string.IsNullOrEmpty(compUrlResolved))
                {
                    try
                    {
                        // Lấy companyId và Linkedin hiện tại từ DB để lưu Person
                        string companyId = "";
                        string dbLinkedin = "";
                        DataTable dtCoInfo = DatabaseHelper.ExecuteQuery(
                            "SELECT ID, Linkedin FROM Company WHERE CompanyName = $name COLLATE NOCASE LIMIT 1",
                            new[] { new SqliteParameter("$name", CompanyName) });
                        if (dtCoInfo.Rows.Count > 0)
                        {
                            companyId = dtCoInfo.Rows[0]["ID"].ToString();
                            dbLinkedin = dtCoInfo.Rows[0]["Linkedin"]?.ToString() ?? "";
                        }

                        // Nếu tìm thấy công ty và trường Linkedin hiện tại đang trống hoặc 'N/A', cập nhật lại luôn
                        if (!string.IsNullOrEmpty(companyId))
                        {
                            if (string.IsNullOrEmpty(dbLinkedin) || dbLinkedin == "N/A" || dbLinkedin.Trim() == "")
                            {
                                DatabaseHelper.ExecuteNonQuery(
                                    "UPDATE Company SET Linkedin = $link WHERE ID = $id",
                                    new[] { new SqliteParameter("$link", compUrlResolved), new SqliteParameter("$id", companyId) });
                            }
                        }

                        // Cập nhật lại đường dẫn LinkedIn lên grid hiển thị nếu đang trống/N/A
                        this.Invoke(new Action(() =>
                        {
                            lock (_gridLock)
                            {
                                for (int ri = 0; ri < mydata.Rows.Count; ri++)
                                {
                                    string cn = mydata.Rows[ri]["CompanyName"]?.ToString() ?? "";
                                    string rt = mydata.Rows[ri]["RowType"]?.ToString() ?? "";
                                    if (rt == "Company" && string.Equals(cn.Trim(), CompanyName.Trim(), StringComparison.OrdinalIgnoreCase))
                                    {
                                        string currentGridLink = mydata.Rows[ri]["LinkedInCo"]?.ToString() ?? "";
                                        if (string.IsNullOrEmpty(currentGridLink) || currentGridLink == "N/A" || currentGridLink.Trim() == "")
                                        {
                                            mydata.Rows[ri]["LinkedInCo"] = compUrlResolved;
                                        }
                                        break;
                                    }
                                }
                            }
                        }));

                        // Lặp qua từng keyword để thực hiện tìm kiếm
                        foreach (string keywordRaw in keywords)
                        {
                            if (token.IsCancellationRequested) break;
                            string keyword = keywordRaw.Trim();
                            if (string.IsNullOrEmpty(keyword)) continue;

                            this.Invoke(new Action(() => lblStatus.Text = $"Crawling LinkedIn: {keyword}..."));

                            string pUrl = compUrlResolved + "/people/?keywords=" + Uri.EscapeDataString(keyword);
                            localDriver.Navigate().GoToUrl(pUrl);
                            await Task.Delay(6000, token);
                            await WaitForLinkedInRateLimit(token, localDriver);
                            await WaitForLinkedInCheckpoint(token, localDriver);

                        // Hỗ trợ gõ trực tiếp vào ô tìm kiếm nếu url parameters không tự động filter (fallback)
                        try
                        {
                            var inputSelectors = new[] {
                                "input#people-search-keywords",
                                "input[placeholder*='Search employees']",
                                "input[placeholder*='Search']",
                                "input[aria-label*='Search']",
                                "input.org-people-bar__search-input",
                                ".org-people-bar__search-input input",
                                ".org-people__search-input"
                            };

                            IWebElement searchInput = null;
                            foreach (var selector in inputSelectors)
                            {
                                try
                                {
                                    var elements = localDriver.FindElements(By.CssSelector(selector));
                                    if (elements.Count > 0 && elements[0].Displayed && elements[0].Enabled)
                                    {
                                        searchInput = elements[0];
                                        break;
                                    }
                                }
                                catch {}
                            }

                            if (searchInput != null)
                            {
                                string currentVal = searchInput.GetAttribute("value") ?? "";
                                if (!currentVal.ToLower().Contains(keyword.ToLower()))
                                {
                                    searchInput.Click();
                                    searchInput.SendKeys(OpenQA.Selenium.Keys.Control + "a");
                                    searchInput.SendKeys(OpenQA.Selenium.Keys.Delete);
                                    await Task.Delay(500, token);
                                    searchInput.SendKeys(keyword);
                                    searchInput.SendKeys(OpenQA.Selenium.Keys.Enter);
                                    await Task.Delay(6000, token);
                                }
                            }
                        }
                        catch {}

                        // Click "Show more results" if exists to load more people
                        try
                        {
                            int clickCount = 0;
                            int maxClicks = 5;
                            while (clickCount < maxClicks && !token.IsCancellationRequested)
                            {
                                try
                                {
                                    ((IJavaScriptExecutor)localDriver).ExecuteScript("window.scrollTo(0, document.body.scrollHeight);");
                                    await Task.Delay(1500, token);
                                }
                                catch {}

                                var loadMoreBtns = localDriver.FindElements(By.CssSelector("button.scaffold-finite-scroll__load-button, button[class*='scaffold-finite-scroll__load-button']"));
                                if (loadMoreBtns.Count > 0 && loadMoreBtns[0].Displayed && loadMoreBtns[0].Enabled)
                                {
                                    this.Invoke(new Action(() => lblStatus.Text = $"LinkedIn ({keyword}): loading more people ({clickCount + 1})..."));
                                    try
                                    {
                                        loadMoreBtns[0].Click();
                                    }
                                    catch
                                    {
                                        ((IJavaScriptExecutor)localDriver).ExecuteScript("arguments[0].click();", loadMoreBtns[0]);
                                    }
                                    clickCount++;
                                    await Task.Delay(3000, token);
                                }
                                else
                                {
                                    break;
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine("Error clicking load-more button: " + ex.Message);
                        }

                        // 6. Quét thẻ profile
                        var peopleCards = localDriver.FindElements(By.CssSelector(".org-people-profile-card__profile-info"));
                        if (peopleCards.Count > 0 && !string.IsNullOrEmpty(companyId))
                        {
                            foreach (var card in peopleCards)
                            {
                                if (token.IsCancellationRequested) return;

                                try
                                {
                                    var linkNode = card.FindElement(By.CssSelector("a"));
                                    string personLink = linkNode.GetAttribute("href");
                                    
                                    var nameNodes = card.FindElements(By.CssSelector(".artdeco-entity-lockup__title, .org-people-profile-card__profile-title, .lt-line-clamp--single-line"));
                                    string personName = nameNodes.Count > 0 ? nameNodes[0].Text.Trim() : "";

                                    var subtitleNodes = card.FindElements(By.CssSelector(".artdeco-entity-lockup__subtitle, .lt-line-clamp--multi-line, .artdeco-entity-lockup__caption"));
                                    string subtitle = subtitleNodes.Count > 0 ? subtitleNodes[0].Text.Trim() : "";
                                    
                                    if (personLink.Contains("?")) personLink = personLink.Split('?')[0];

                                    // LƯU VÀO DATABASE - kiểm tra đã có chưa
                                    DataTable dtPerson = DatabaseHelper.ExecuteQuery("SELECT ID, Email, Phone FROM Person WHERE Linkedin = $peLink LIMIT 1", new[] { new SqliteParameter("$peLink", personLink) });
                                    
                                    string existingEmail = "";
                                    string existingPhone = "";
                                    if (dtPerson.Rows.Count == 0)
                                    {
                                        string nowStr = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                                        string sqlInsert = "INSERT INTO Person(CompanyID, FullName, Position, Linkedin, Email, Phone, LastUpdate) VALUES($cId, $fname, $pos, $link, '', '', $date)";
                                        DatabaseHelper.ExecuteNonQuery(sqlInsert, new[] {
                                            new SqliteParameter("$cId", companyId),
                                            new SqliteParameter("$fname", personName),
                                            new SqliteParameter("$pos", subtitle),
                                            new SqliteParameter("$link", personLink),
                                            new SqliteParameter("$date", nowStr)
                                        });
                                    }
                                    else
                                    {
                                        existingEmail = dtPerson.Rows[0]["Email"]?.ToString() ?? "";
                                        existingPhone = dtPerson.Rows[0]["Phone"]?.ToString() ?? "";
                                    }

                                    // Luôn hiển thị lên Grid (mydata) nếu chưa có trên Grid
                                    this.Invoke(new Action(() => {
                                        // Kiểm tra xem person đã có trên Grid chưa
                                        bool alreadyInGrid = false;
                                        for (int ri = 0; ri < mydata.Rows.Count; ri++)
                                        {
                                            if ((mydata.Rows[ri]["RowType"]?.ToString() ?? "") == "Person"
                                                && (mydata.Rows[ri]["LinkedInPe"]?.ToString() == personLink))
                                            {
                                                alreadyInGrid = true;
                                                mydata.Rows[ri]["FullNamePe"] = personName;
                                                mydata.Rows[ri]["PositionPe"] = subtitle;
                                                if (!string.IsNullOrEmpty(existingEmail)) mydata.Rows[ri]["EmailPe"] = existingEmail;
                                                if (!string.IsNullOrEmpty(existingPhone)) mydata.Rows[ri]["PhonePe"] = existingPhone;
                                                break;
                                            }
                                        }

                                        if (!alreadyInGrid)
                                        {
                                            // Tìm index để insert sau dòng Company + các Person đã có
                                            int insertIndex = -1;
                                            bool foundCompany = false;
                                            for (int ri = 0; ri < mydata.Rows.Count; ri++)
                                            {
                                                string rt = mydata.Rows[ri]["RowType"]?.ToString() ?? "";
                                                string cn = mydata.Rows[ri]["CompanyName"]?.ToString() ?? "";
                                                if (rt == "Company" && string.Equals(cn.Trim(), CompanyName.Trim(), StringComparison.OrdinalIgnoreCase))
                                                {
                                                    foundCompany = true;
                                                    insertIndex = ri + 1;
                                                    while (insertIndex < mydata.Rows.Count
                                                           && (mydata.Rows[insertIndex]["RowType"]?.ToString() ?? "") == "Person")
                                                        insertIndex++;
                                                    break;
                                                }
                                            }
                                            // Đếm số Person con hiện có để đặt STT
                                            int siblingCount = 0;
                                            if (foundCompany && insertIndex > 0)
                                            {
                                                for (int ri = insertIndex - 1; ri >= 0; ri--)
                                                {
                                                    if ((mydata.Rows[ri]["RowType"]?.ToString() ?? "") == "Person")
                                                        siblingCount++;
                                                    else
                                                        break;
                                                }
                                            }

                                            DataRow dr = mydata.NewRow();
                                            dr["STT"]         = "  ├ " + (siblingCount + 1);
                                            dr["CompanyName"] = ""; // Không lặp lại tên công ty
                                            dr["FullNamePe"]  = personName;
                                            dr["PositionPe"]  = subtitle;
                                            dr["LinkedInPe"]  = personLink;
                                            dr["EmailPe"]     = existingEmail;
                                            dr["PhonePe"]     = existingPhone;
                                            dr["RowType"]     = "Person";

                                            if (foundCompany && insertIndex >= 0 && insertIndex <= mydata.Rows.Count)
                                                mydata.Rows.InsertAt(dr, insertIndex);
                                            else
                                                mydata.Rows.Add(dr);

                                            totalStaffCrawl = mydata.Rows.Count;
                                            lblOutput.Text = $"OUTPUT: {totalStaffCrawl}";
                                            if (dgvOutput.Rows.Count > 0)
                                                dgvOutput.FirstDisplayedScrollingRowIndex = dgvOutput.Rows.Count - 1;
                                        }
                                    }));
                                }
                                catch
                                {
                                    // Bỏ qua lỗi 1 người nếu DOM ko chuẩn
                                }
                            }
                        }
                        } // end foreach keyword
                    }
                    catch (Exception peopleEx)
                    {
                        Console.WriteLine($"[LinkedIn People] {CompanyName}: {peopleEx.Message}");
                    }
                } // end if (compUrlResolved)

                this.Invoke(new Action(() => lblStatus.Text = "Finished LinkedIn search."));
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Error in GetPersionFromLinkedInCompany: " + ex.Message);
            }
        }
    }
}
