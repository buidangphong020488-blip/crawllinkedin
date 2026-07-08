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
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace crawlData
{
    public partial class FrmExtractor : Form
    {
        // Khai báo ở đầu Class Form
        private CancellationTokenSource _cts;
        DataTable mydata;
        private IWebDriver driver; // THÊM DÒNG NÀY
        public FrmExtractor(string pcKey)
        {
            InitializeComponent();
            lblPcKey.Text ="Key: "+ pcKey;
            InitGridOutput();
            LoadColumnSettings();
            GenerateColumnFilters();

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
                                string col1Name = dtOriginal.Columns[0].ColumnName; // Cột 1 (Index 0)
                                string col2Name = dtOriginal.Columns[1].ColumnName; // Cột 2 (Index 1)
                                // 4. Lọc lấy 2 cột chính
                                DataTable dtFiltered = dtOriginal.DefaultView.ToTable(false, col1Name, col2Name);

                                // --- PHẦN THÊM MỚI TẠI ĐÂY ---
                                // 4.1 Thêm cột STT vào vị trí đầu tiên (index 0)
                                if (!dtFiltered.Columns.Contains("STT"))
                                {
                                    DataColumn sttCol = new DataColumn("STT", typeof(int));
                                    dtFiltered.Columns.Add(sttCol);
                                    sttCol.SetOrdinal(0); // Đưa STT lên đầu
                                }

                                // 4.2 Thêm cột Status vào cuối
                                if (!dtFiltered.Columns.Contains("Status"))
                                {
                                    dtFiltered.Columns.Add("Status", typeof(string));
                                }

                                // 4.3 Duyệt qua dữ liệu để đánh số thứ tự và gán trạng thái mặc định
                                for (int i = 0; i < dtFiltered.Rows.Count; i++)
                                {
                                    dtFiltered.Rows[i]["STT"] = i + 1;
                                    dtFiltered.Rows[i]["Status"] = "Pending";
                                }
                                // --- KẾT THÚC PHẦN THÊM MỚI ---

                                // 5. Đổ dữ liệu lên Grid
                                dgvInput.DataSource = dtFiltered;
                                // 1. HIỂN THỊ VÒNG XOAY
                                //spinnerLoading.Visible = true;
                                //btnUploadExcel.Enabled = false;
                                FormatGridView(col1Name, col2Name);
                                int InputCount = dtFiltered.Rows.Count + 1;
                                lblInput.Text = $"INPUT: { InputCount}";
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
            // 1. KIỂM TRA KEY TRƯỚC KHI CHẠY
            DataTable dtConfig = DatabaseHelper.ExecuteQuery("SELECT aistudio_key FROM Config LIMIT 1");

            if (dtConfig.Rows.Count == 0 || string.IsNullOrEmpty(dtConfig.Rows[0]["aistudio_key"].ToString()))
            {
                MessageBox.Show("Vui lòng vào phần Cấu hình để nhập AI Studio Key trước khi bắt đầu!",
                                "Thiếu cấu hình", MessageBoxButtons.OK, MessageBoxIcon.Warning);

                // Có thể tự động mở tab/form cấu hình ở đây nếu muốn
                // tabControl1.SelectedTab = tpConfig; 
                return;
            }
            _cts = new CancellationTokenSource();
            btnStart.Enabled = false;
            btnStop.Enabled = true;
            lblStatus.Text = "Running";
            DataTable dtInput = (DataTable)dgvInput.DataSource;
            try
            {
                // Chạy toàn bộ MainProcess ở Background Thread để không chặn UI
                await Task.Run(async () => await MainProcess(dtInput, _cts.Token));
            }
            catch (Exception ex)
            {
                //MessageBox.Show("Lỗi hệ thống: " + ex.Message);
            }
            finally
            {
                lblStatus.Text = _cts.Token.IsCancellationRequested ? "Stopped" : "Finished";
                btnStart.Enabled = true;
                btnStop.Enabled = false;
            }
        }
        // --- NEW LOGIC: Multi-stage Crawling ---

        private async Task MainProcess(DataTable dtInput, CancellationToken token)
        {
            int sttCount = mydata.Rows.Count + 1;
            int proxyIndex = 0;
            // Biến lưu chỉ số dòng hiện tại để không bị reset khi khởi động lại Chrome
            int currentRowIndex = 0;

            // Vòng lặp chính: Chừng nào chưa hết danh sách thì vẫn tiếp tục
            while (currentRowIndex < dtInput.Rows.Count && !token.IsCancellationRequested)
            {
                try
                {
                    // Khởi tạo Chrome (đã ở trong background thread do MainProcess chạy background)
                    if (driver == null)
                    {
                        driver = await InitChromeDriverAsync(proxyIndex);
                    }

                    // Chạy từ dòng bị dừng lần trước
                    for (int i = currentRowIndex; i < dtInput.Rows.Count; i++)
                    {
                        if (token.IsCancellationRequested) break;

                        DataRow row = dtInput.Rows[i];
                        currentRowIndex = i;

                        this.Invoke(new Action(() => lblStatus.Text = $"Processing: {row[1]}"));

                        string CompanyName = row[1].ToString();
                        string status = row["Status"]?.ToString();

                        if (status == "Completed" || IsCompanyProcessed(CompanyName))
                        {
                            this.Invoke(new Action(() =>
                            {
                                row["Status"] = "Skipped";
                                LoadSpecificCompanyToGrid(CompanyName);
                            }));
                            currentRowIndex++; // Nhảy sang dòng tiếp theo
                            continue;
                        }

                        // === STAGE 1: SEARCH GOOGLE FOR WEBSITE ===
                        this.Invoke(new Action(() => lblStatus.Text = $"Step 1/3: Finding Website for {CompanyName}..."));
                        string websiteUrl = await GetWebsiteFromGoogleSearchResults(driver, CompanyName, token);

                        // Object chứa thông tin tổng hợp
                        dynamic companyInfo = new JObject();
                        companyInfo.name = CompanyName;
                        companyInfo.website = websiteUrl;
                        companyInfo.leaders = new JArray();

                        if (!string.IsNullOrEmpty(websiteUrl) && websiteUrl != "N/A")
                        {
                            // === STAGE 2: CRAWL WEBSITE & SUBPAGES ===
                            this.Invoke(new Action(() => lblStatus.Text = $"Step 2/3: Crawling Website {websiteUrl}..."));
                            await CrawlCompanyWebsite(driver, companyInfo, token);
                            // Đóng tab thừa sau khi crawl website (website có thể mở popup/tab mới)
                            CloseExtraTabs(driver);
                        }
                        else
                        {
                            companyInfo.website = "N/A";
                            // Nếu không có website, vẫn cố tìm LinkedIn ở bước 3
                        }

                        // === STAGE 3: SEARCH LINKEDIN FOR REPRESENTATIVE (If needed) ===
                        // Kiểm tra xem đã có leader chưa
                        var leaders = (JArray)companyInfo.leaders;
                        if (leaders.Count == 0)
                        {
                            this.Invoke(new Action(() => lblStatus.Text = $"Step 3/3: Searching Leaders on LinkedIn..."));
                            await SearchRepresentativeOnGoogle(driver, companyInfo, token);
                            // Đóng tab thừa sau LinkedIn search
                            CloseExtraTabs(driver);
                        }

                        // === FINAL: SAVE & UPDATE ===     
                        this.Invoke(new Action(() => lblStatus.Text = "Saving Data..."));
                        await SaveCrawlResult(companyInfo);
                        this.Invoke(new Action(() =>
                        {
                            try { 
                                UpdateGridOutput(companyInfo, ref sttCount);
                                row["Status"] = "Completed";
                            } catch { } // Ignore UI update errors
                        }));

                        currentRowIndex++;
                        await Task.Delay(2000, token);
                    }
                }
                catch (Exception ex)
                {
                    if (ex.Message == "CaptchaDetected")
                    {
                        Console.WriteLine("Phát hiện Captcha. Đang đổi Proxy và khởi động lại Chrome...");
                         // Logic đổi Proxy
                        string proxyString = GetProxyFromDB();
                        string[] proxyList = string.IsNullOrEmpty(proxyString)
                                            ? new string[0]
                                            : proxyString.Split(new[] { "\n", "\r\n" }, StringSplitOptions.RemoveEmptyEntries);

                        if (chkUseProxy.Checked && proxyIndex < proxyList.Length - 1)
                        {
                            proxyIndex++; // Đổi sang proxy tiếp theo
                        }
                        else if (chkUseProxy.Checked)
                        {
                            proxyIndex = 0;
                            await Task.Delay(30000, token);
                        }
                    }
                    else
                    {
                        Console.WriteLine("Lỗi hệ thống: " + ex.Message);
                    }

                    // Reset driver để retry hoặc continue
                    if (driver != null)
                    {
                        try { driver.Quit(); driver.Dispose(); } catch { }
                        driver = null;
                    }
                    await Task.Delay(3000, token);
                }
            }

            this.Invoke(new Action(() => lblStatus.Text = "Hoàn thành tất cả!"));
            if (driver != null)
            {
                try { driver.Quit(); driver.Dispose(); } catch { }
                driver = null;
            }
        }

        // 1. Tìm Website từ Google
        private async Task<string> GetWebsiteFromGoogleSearchResults(IWebDriver driver, string companyName, CancellationToken token)
        {
            string query = $"\"{companyName}\" official website";
            string fullUrl = $"https://www.google.com/search?q={Uri.EscapeDataString(query)}&udm=14"; // udm=14 là chế độ web đơn giản mới của Google
            
            // Đóng tab thừa trước khi navigate tới Google
            CloseExtraTabs(driver);
            driver.Navigate().GoToUrl(fullUrl);
            await CheckCaptcha(driver, fullUrl, token);

            // Lấy kết quả đầu tiên không phải quảng cáo
            try 
            {
                var links = driver.FindElements(By.CssSelector("div#search a[href^='http']"));
                foreach (var link in links)
                {
                    string url = link.GetAttribute("href");
                    if (IsValidCompanyUrl(url))
                    {
                        return url;
                    }
                }
            }
            catch {}
            
            return "N/A";
        }

        private bool IsValidCompanyUrl(string url)
        {
            if (string.IsNullOrEmpty(url)) return false;
            if (url.Contains("google.com")) return false;
            if (url.Contains("facebook.com")) return false; // Thường ta muốn web chính chủ, không phải fanpage (tùy nhu cầu)
            if (url.Contains("yellowpages")) return false;
            if (url.Contains("masothue")) return false;
            return true;
        }

        // 2. Crawl Website & Subpages
        private async Task CrawlCompanyWebsite(IWebDriver driver, dynamic companyInfo, CancellationToken token)
        {
            string url = companyInfo.website;
            if (string.IsNullOrEmpty(url) || url == "N/A") return;

            // 2.1. Trang chủ
            try
            {
                driver.Navigate().GoToUrl(url);
                await Task.Delay(3000, token); // Đợi load

                var pageData = GetSafePageText(driver);
                await ExtractInfoFromText(pageData.text, pageData.html, companyInfo);

                // Nếu thiếu thông tin quan trọng, tìm Subpage
                if (IsMissingInfo(companyInfo))
                {
                    var subPages = FindSubPageLinks(driver);
                    foreach (var subLink in subPages)
                    {
                        if (token.IsCancellationRequested) return;
                        try
                        {
                            driver.Navigate().GoToUrl(subLink);
                            await Task.Delay(2000, token);
                            pageData = GetSafePageText(driver);
                            await ExtractInfoFromText(pageData.text, pageData.html, companyInfo);
                            if (!IsMissingInfo(companyInfo)) break; // Đủ thông tin rồi thì thôi
                        }
                        catch { }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Lỗi Crawl Web: " + ex.Message);
            }
        }

        private (string text, string html) GetSafePageText(IWebDriver driver)
        {
             try {
                string text = driver.FindElement(By.TagName("body")).Text;
                string html = driver.PageSource;
                return (text, html);
             } catch { return ("", ""); }
        }

        private bool IsMissingInfo(dynamic info)
        {
            // Kiểm tra xem có thiếu Phone, Email hoặc Leaders không
            string email = info.email?.ToString();
            string phone = info.phone?.ToString();
            var leaders = (JArray)info.leaders;
            
            return string.IsNullOrEmpty(email) || string.IsNullOrEmpty(phone) || leaders.Count == 0;
        }

        private List<string> FindSubPageLinks(IWebDriver driver)
        {
            List<string> results = new List<string>();
            try
            {
                var elements = driver.FindElements(By.TagName("a"));
                foreach (var el in elements)
                {
                    string href = el.GetAttribute("href");
                    if (string.IsNullOrEmpty(href) || href.StartsWith("javascript") || href.StartsWith("tel") || href.StartsWith("mailto")) continue;

                    // 1. Check Keywords trong URL (Ưu tiên cao nhất)
                    string hrefLower = href.ToLower();
                    bool isMatchUrl = hrefLower.Contains("about") || hrefLower.Contains("gioi-thieu") || hrefLower.Contains("contact") || hrefLower.Contains("lien-he") || hrefLower.Contains("leadership") || hrefLower.Contains("management");

                    // 2. Check Keywords trong Text (Dùng textContent để lấy cả text ẩn trong menu)
                    string text = (el.GetAttribute("textContent") ?? "").ToLower();
                    bool isMatchText = text.Contains("about") || text.Contains("giới thiệu") || text.Contains("về chúng tôi") ||
                                       text.Contains("contact") || text.Contains("liên hệ") || text.Contains("leadership") || text.Contains("management") || text.Contains("ban lãnh đạo");

                    if (isMatchUrl || isMatchText)
                    {
                        if (!results.Contains(href)) results.Add(href);
                    }
                }
            }
            catch {}
            return results.Take(3).ToList(); // Lấy tối đa 3 link
        }

        private async Task ExtractInfoFromText(string pageText, string pageHtml, dynamic companyInfo)
        {
            if (string.IsNullOrEmpty(pageText) && string.IsNullOrEmpty(pageHtml)) return;
            Console.WriteLine($"[DEBUG] Extracting info. TextLen: {pageText?.Length ?? 0}, HtmlLen: {pageHtml?.Length ?? 0}");
            
            // --- 1. REGEX EXTRACTION (Priority - using HTML) ---
            try
            {
               
                // A. Email
                if (IsNull(companyInfo.email))
                {
                    var emailMatches = Regex.Matches(pageHtml, @"[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}");
                    if (emailMatches.Count > 0)
                    {
                        var emails = emailMatches.Cast<Match>().Select(m => m.Value).Distinct().ToList();
                        companyInfo.email = string.Join(" - ", emails);
                    }
                }

                // B. Phone (VN & Intl format patterns)
                if (IsNull(companyInfo.phone))
                {
                    var allPhones = new List<string>();
                    // Pattern: (+84|0) followed by 9-10 digits
                    var matches1 = Regex.Matches(pageHtml, @"(\+84|0)(3|5|7|8|9|2[4|8])([\s.-]?[0-9]){8}\b");
                    foreach (Match m in matches1)
                    {
                        string p = Regex.Replace(m.Value.Trim(), "<.*?>", "");
                        if (p.Length >= 8 && p.Length < 25 && !allPhones.Contains(p)) allPhones.Add(p);
                    }

                    if (allPhones.Count == 0)
                    {
                        // Try broader pattern
                        var matches2 = Regex.Matches(pageHtml, @"(\(?\+?[0-9]*\)?)?[0-9_\- \(\)]{8,}");
                        foreach (Match m in matches2)
                        {
                            string p = Regex.Replace(m.Value.Trim(), "<.*?>", "");
                            if (p.Length >= 8 && p.Length < 25 && !allPhones.Contains(p)) allPhones.Add(p);
                        }
                    }

                    if (allPhones.Count > 0)
                    {
                        companyInfo.phone = string.Join(" - ", allPhones);
                    }
                }

                // C. Address (Heuristic via Keywords)
                if (IsNull(companyInfo.address))
                {
                     // Regex trên HTML có thể dính tag, nên dùng Text cho Address thì an toàn hơn, hoặc phải strip tag
                     // Theo yêu cầu dùng HTML cho tất cả Regex, nhưng Address thường phức tạp.
                     // Tuy nhiên, để tuân thủ, ta thử pattern trên HTML nhưng cẩn thận tag
                     var addrMatch = Regex.Match(pageHtml, @"(Địa chỉ|Address|Trụ sở|Headquarter)[:\.]\s*([^\n\r<]+)", RegexOptions.IgnoreCase);
                     if (addrMatch.Success)
                     {
                         companyInfo.address = addrMatch.Groups[2].Value.Trim();
                     }
                }
            }
            catch (Exception ex) { Console.WriteLine($"[DEBUG] Regex Error: {ex.Message}"); }

            // --- 2. AI ENRICHMENT (Using Text) ---
            if (pageText.Length < 50) return; // Text quá ngắn thì thôi không gọi AI

            // Cắt ngắn bớt nếu quá dài để tiết kiệm token

            // --- 2. AI ENRICHMENT (For Leaders & Missing Info) ---
            
            // Cắt ngắn bớt nếu quá dài để tiết kiệm token
            if (pageText.Length > 10000) pageText = pageText.Substring(0, 10000);

            string prompt = $@"
            Extract ALL phone numbers and email addresses from this text:
            *** {pageText} ***
            ---
            Update JSON data with existing fields if found.
            JSON Schema: {{ 'phone': 'số1 - Fax: số2', 'email': 'mail1 - mail2', 'address': '', 'industry': '', 'linkedin': '', 'leaders': [{{ 'name': '', 'position': '', 'linkedin': '', 'email': '', 'phone': '' }}] }}
            - Lấy TẤT CẢ số điện thoại và email tìm thấy, cách nhau bằng dấu ' - '.
            - Append to 'leaders' list if new leaders found.
            - Only return valid data, use 'N/A' if not found.
            ";

            Console.WriteLine("[DEBUG] Calling AI...");
            string jsonResult = await CallOpenAI(prompt); // Ưu tiên OpenAI
            if (jsonResult == "{}" || jsonResult.Contains("Error")) jsonResult = await CallGeminiAPI(prompt);
            Console.WriteLine($"[DEBUG] AI Result: {jsonResult}");

            if (jsonResult != "{}")
            {
                MergeJsonData(companyInfo, jsonResult);
            }
        }

        private void MergeJsonData(dynamic target, string jsonSource)
        {
            try
            {
                dynamic source = JsonConvert.DeserializeObject(jsonSource);
                if (source == null) return;

                // Merge simple fields if target is null/empty
                // Merge Phone
                string targetPhone = target.phone?.ToString() ?? "";
                string sourcePhone = source.phone?.ToString() ?? "";
                if (!string.IsNullOrEmpty(sourcePhone) && sourcePhone.ToUpper() != "N/A")
                {
                    var allPhones = (targetPhone + " - " + sourcePhone)
                        .Split(new[] { " - " }, StringSplitOptions.RemoveEmptyEntries)
                        .Select(s => s.Trim())
                        .Where(s => !string.IsNullOrEmpty(s) && s.ToUpper() != "N/A")
                        .Distinct(StringComparer.OrdinalIgnoreCase);
                    target.phone = string.Join(" - ", allPhones);
                }

                // Merge Email
                string targetEmail = target.email?.ToString() ?? "";
                string sourceEmail = source.email?.ToString() ?? "";
                if (!string.IsNullOrEmpty(sourceEmail) && sourceEmail.ToUpper() != "N/A")
                {
                    var allEmails = (targetEmail + " - " + sourceEmail)
                        .Split(new[] { " - " }, StringSplitOptions.RemoveEmptyEntries)
                        .Select(s => s.Trim())
                        .Where(s => !string.IsNullOrEmpty(s) && s.ToUpper() != "N/A")
                        .Distinct(StringComparer.OrdinalIgnoreCase);
                    target.email = string.Join(" - ", allEmails);
                }
                if (IsNull(target.address) && !IsNull(source.address)) target.address = source.address;
                if (IsNull(target.industry) && !IsNull(source.industry)) target.industry = source.industry;
                if (IsNull(target.linkedin) && !IsNull(source.linkedin)) target.linkedin = source.linkedin;

                // Merge Leaders
                if (source.leaders != null)
                {
                    var targetLeaders = (JArray)target.leaders;
                    foreach (var l in source.leaders)
                    {
                        // Check duplicates by name
                        bool exists = false;
                        foreach (var tl in targetLeaders)
                        {
                            if (tl["name"]?.ToString() == l["name"]?.ToString())
                            {
                                exists = true; 
                                break;
                            }
                        }
                        if (!exists) targetLeaders.Add(l);
                    }
                }
            }
            catch (Exception ex) { Console.WriteLine($"[DEBUG] Merge Error: {ex.Message}"); }
        }
        
        private bool IsNull(dynamic val) 
        {
             return val == null || val.ToString() == "" || val.ToString() == "N/A";
        }

        // 3. Search Representative on Google (New LinkedIn Query)
        private async Task SearchRepresentativeOnGoogle(IWebDriver driver, dynamic companyInfo, CancellationToken token)
        {
            string companyName = companyInfo.name;
            // Query cập nhật theo yêu cầu mới
            string query = $"site:linkedin.com/in/ \"{companyName}\" (CEO OR \"Giám đốc\" OR Founder OR \"Chủ tịch\" OR \"Chairman\" OR \"Director\" OR \"Manager\" OR \"Head of\" OR \"Trưởng phòng\" OR \"Quản lý\" OR \"Phó giám đốc\" OR VP) -intitle:\"nhân viên\" -intitle:\"staff\" -intitle:\"thực tập\"";
           
            string fullUrl = $"https://www.google.com/search?q={Uri.EscapeDataString(query)}";

            driver.Navigate().GoToUrl(fullUrl);
            await CheckCaptcha(driver, fullUrl, token);

            // Lấy kết quả Google (Title + Snippet) và nhờ AI phân tích
            try 
            {
                var mainCol = driver.FindElement(By.Id("search")); // Hoặc main-col tùy layout
                string contextText = mainCol.Text;
                
                string prompt = $@"
                From this Google Search Results for '{companyName}' leaders on LinkedIn:
                *** {contextText} ***
                ---
                Identify key people (Name, Position, LinkedIn URL).
                Return JSON: {{ 'leaders': [ {{ 'name': '', 'position': '', 'linkedin': '' }} ] }}
                Ignore generic results.
                ";

                string jsonResult = await CallOpenAI(prompt);
                if (jsonResult == "{}") jsonResult = await CallGeminiAPI(prompt);

                if (jsonResult != "{}")
                {
                    MergeJsonData(companyInfo, jsonResult);
                }
            }
            catch { }
        }

        private async Task CheckCaptcha(IWebDriver driver, string url, CancellationToken token)
        {
             // Hàm check captcha: Chỉ báo lỗi khi THỰC SỰ bị chặn
             try {
                // 1. Check form Captcha của Google
                var captchaForms = driver.FindElements(By.Id("captcha-form"));
                if (captchaForms.Count > 0)
                {
                    throw new Exception("CaptchaDetected");
                }

                // 2. Check Title
                if (driver.Title.Contains("Sorry") || driver.Title.Contains("Captcha"))
                {
                    throw new Exception("CaptchaDetected");
                }
                
                // 3. Check text cảnh báo cụ thể
                if (driver.PageSource.Contains("unusual traffic") && driver.PageSource.Contains("robot"))
                {
                     throw new Exception("CaptchaDetected");
                }
             }
             catch (Exception ex)
             {
                 if (ex.Message == "CaptchaDetected") throw;
             }
             await Task.Delay(1000, token);
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
        private async Task<IWebDriver> InitChromeDriverAsync(int proxyIndex)
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
                this.Invoke(new Action(() =>
                {
                    lblProxy.Text = "None";
                }));
            }
            else
            {
                string[] parts = currentProxy.Split(':');
                this.Invoke(new Action(() =>
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
            options.AddArgument($"--user-agent={userAgents[proxyIndex % userAgents.Length]}");
            
            string baseDir = AppDomain.CurrentDomain.BaseDirectory;

            // Cấu hình đường dẫn đến Chrome (nằm trong thư mục app)
            options.BinaryLocation = Path.Combine(baseDir, "Chrome_v121", "chrome", "chrome.exe");

            // Cấu hình đường dẫn đến Profiles
            string profilePath = Path.Combine(baseDir, "Chrome_v121", "Profiles", $"Proxy_{proxyIndex}");

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
            options.AddArgument("--disable-application-cache"); // Vô hiệu hóa cache ứng dụng

            //options.AddArgument("--disable-blink-features=AutomationControlled"); // Quan trọng nhất
            //options.AddExcludedArgument("enable-automation");
            //options.AddArgument("--disable-infobars");
            //options.AddArgument("--no-sandbox");

            // UPDATE STATUS
            this.Invoke(new Action(() => lblStatus.Text = "Opening Chrome..."));

            var driver = new ChromeDriver(service, options, TimeSpan.FromSeconds(120));
            driver.Manage().Timeouts().PageLoad = TimeSpan.FromSeconds(60);
            
            try
            {
                // Tối ưu Xóa dữ liệu: Dùng lệnh CDP hoặc JS thay vì giao diện
                try 
                {
                    this.Invoke(new Action(() => lblStatus.Text = "Clearing Data..."));
                    driver.Manage().Cookies.DeleteAllCookies();
                    ((IJavaScriptExecutor)driver).ExecuteScript("window.localStorage.clear();");
                    ((IJavaScriptExecutor)driver).ExecuteScript("window.sessionStorage.clear();");
                    
                    // RESTORED & IMPROVED: Dùng JS Shadow DOM để click nút "Clear data" chính xác 100%
                    driver.Navigate().GoToUrl("chrome://settings/clearBrowserData");
                    // await Task.Delay(1000); // Giảm delay
                    
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

                if (currentProxy != "")
                {
                    this.Invoke(new Action(() => lblStatus.Text = "Configuring Proxy..."));
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

                        // Đóng tab extension nhanh gọn
                        // await Task.Delay(500); // Giảm delay tối đa
                        driver.Close(); 
                        
                        // Switch về tab chính (thường là tab đầu tiên đang mở - data:,)
                        driver.SwitchTo().Window(driver.WindowHandles.First());
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
        /// Gọi sau mỗi lần crawl website hoặc LinkedIn để tránh tích tụ tab.
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
        
        private void LoadSpecificCompanyToGrid(string companyName)
        {
            // Lấy thông tin từ DB của riêng công ty này
            string sql = @"
        SELECT c.CompanyName, c.Address, c.Industry, c.Phone, c.Website, c.Email, c.Linkedin as CoLink,
               p.FullName, p.Position, p.Linkedin as PeLink, p.Email as PeEmail, p.Phone as PePhone
        FROM Company c
        LEFT JOIN Person p ON c.ID = p.CompanyID
        WHERE c.CompanyName = $name";

            DataTable dtSaved = DatabaseHelper.ExecuteQuery(sql, new[] { new SqliteParameter("$name", companyName) });

            if (dtSaved.Rows.Count == 0)
            {
                // Vẫn hiện 1 row trên output grid dù DB trống
                DataRow drEmpty = mydata.NewRow();
                drEmpty["STT"] = mydata.Rows.Count + 1;
                drEmpty["CompanyName"] = companyName;
                mydata.Rows.Add(drEmpty);
                return;
            }

            foreach (DataRow r in dtSaved.Rows)
            {
                // Kiểm tra xem đã có trong dgvOutput chưa để tránh trùng lặp nếu nhấn Start nhiều lần
                bool exists = mydata.AsEnumerable().Any(x =>
                    x.Field<string>("CompanyName") == companyName &&
                    x.Field<string>("FullNamePe") == r["FullName"].ToString());

                if (!exists)
                {
                    DataRow dr = mydata.NewRow();
                    dr["STT"] = mydata.Rows.Count + 1;
                    dr["CompanyName"] = r["CompanyName"];
                    dr["CompanyAddress"] = r["Address"];
                    dr["Industry"] = r["Industry"];
                    dr["PhoneCo"] = r["Phone"];
                    dr["Website"] = r["Website"];
                    dr["EmailCo"] = r["Email"];
                    dr["LinkedInCo"] = r["CoLink"];
                    dr["FullNamePe"] = r["FullName"];
                    dr["PositionPe"] = r["Position"];
                    dr["LinkedInPe"] = r["PeLink"];
                    dr["EmailPe"] = r["PeEmail"];
                    dr["PhonePe"] = r["PePhone"];
                    mydata.Rows.Add(dr);
                }
            }
        }
        private int totalStaffCrawl;
        private void UpdateGridOutput(dynamic item, ref int sttCount)
        {
            // 1. Lấy thông tin công ty từ item (JSON AI trả về)
            string CompanyName= item.name ?? "";
            if (CompanyName == "") return;
            string CompanyAddress = item.address ?? "";
            string PhoneCo = item.phone ?? "";
            string Website = item.website ?? "";
            string EmailCo = item.email ?? "";
            string LinkedInCo = item.linkedin ?? "";
            string Industry = item.industry ?? "";

            // 2. Xử lý danh sách lãnh đạo (Leaders)
            if (item.leaders != null && item.leaders.Count > 0)
            {
                for (int i = 0; i < item.leaders.Count; i++)
                {
                    var leader = item.leaders[i];

                    DataRow dr = mydata.NewRow();
                    //dr["STT"] = sttCount++;
                    dr["STT"] = mydata.Rows.Count + 1;
                    dr["CompanyName"] = item.name ?? ""; // Tên công ty
                    dr["CompanyAddress"] = CompanyAddress;
                    dr["Industry"] = Industry;
                    dr["PhoneCo"] = PhoneCo;
                    dr["Website"] = Website;
                    dr["EmailCo"] = EmailCo;
                    dr["LinkedInCo"] = LinkedInCo;

                    // Thông tin từng người lãnh đạo
                    dr["FullNamePe"] = leader.name ?? "";
                    dr["PositionPe"] = leader.position ?? "";
                    dr["LinkedInPe"] = leader.linkedin ?? "";
                    dr["EmailPe"] = leader.email ?? "";
                    dr["PhonePe"] = leader.phone ?? "";                    
                    // Thêm vào DataTable (Grid sẽ tự cập nhật)
                    mydata.Rows.Add(dr);
                }
            }
            else
            {
                // Nếu không tìm thấy lãnh đạo, vẫn tạo 1 dòng thông tin công ty
                DataRow dr = mydata.NewRow();
                //dr["STT"] = sttCount++;
                dr["STT"] = mydata.Rows.Count + 1;
                dr["CompanyName"] = item.name ?? "";
                dr["Website"] = Website;
                dr["CompanyAddress"] = CompanyAddress;
                dr["Industry"] = Industry;
                mydata.Rows.Add(dr);
            }
            totalStaffCrawl = mydata.Rows.Count + 1;
            this.Invoke(new Action(() => {
                lblOutput.Text = $"OUTPUT: {totalStaffCrawl}"; // Cần thêm Label này vào giao diện

                if (dgvOutput.Rows.Count > 0)
                {
                    int lastIndex = dgvOutput.Rows.Count - 1;
                    // Chỉ cuộn nếu dòng cuối cùng chưa hiển thị trên màn hình
                    dgvOutput.FirstDisplayedScrollingRowIndex = lastIndex;

                    // Highlight nhẹ nhàng
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
        public Task SaveCrawlResult(dynamic company)
        {
            try
            {
                // 1. Xử lý Website
                string companyname = company?.name?.ToString() ?? "";
                if(companyname=="") return Task.CompletedTask;
                string website = company.website?.ToString() ?? "";
                if (!string.IsNullOrEmpty(website) && website.ToUpper() != "N/A")
                {
                    string web = website.ToLower().Trim();
                    if (!web.StartsWith("http")) web = "https://" + web;
                    try
                    {
                        var uri = new Uri(web);
                        string host = uri.Host;
                        if (host.StartsWith("www.")) host = host.Substring(4);
                        website = host;
                    }
                    catch
                    {
                        website = website.Replace("https://", "").Replace("http://", "").Replace("www.", "").TrimEnd('/');
                    }
                }

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
                    else
                    {
                        sqlCompany = @"UPDATE Company SET CompanyName=$name, Address=$addr, Website=$web, Industry=$industry, Email=$mail, 
                               Linkedin=$link, Phone=$phone, LastUpdate=$date WHERE ID=$id";
                    }

                    using (var cmdComp = new SqliteCommand(sqlCompany, conn, trans))
                    {
                        cmdComp.Parameters.AddRange(new[] {
                            new SqliteParameter("$id", companyId),
                            new SqliteParameter("$name", company.name?.ToString() ?? ""),
                            new SqliteParameter("$addr", company.address?.ToString() ?? ""),
                            new SqliteParameter("$web", website),
                            new SqliteParameter("$industry", company.industry?.ToString() ?? ""),
                            new SqliteParameter("$mail", company.email?.ToString() ?? ""),
                            new SqliteParameter("$link", company.linkedin?.ToString() ?? ""),
                            new SqliteParameter("$phone", company.phone?.ToString() ?? ""),
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
                                sqlPers = @"UPDATE Person SET Position=$pos, Linkedin=$link, Email=$mail, 
                                            Phone=$phone, LastUpdate=$date 
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
        private void btnStop_Click(object sender, EventArgs e)
        {
            if (_cts != null) _cts.Cancel();
            QuitDriver();
            btnStop.Enabled = false;
        }
        private void QuitDriver()
        {
            try { if (driver != null) { driver.Quit(); driver.Dispose(); driver = null; } } catch { }
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
            if (dgvOutput.Columns[e.ColumnIndex].Name == "Unblock" && e.RowIndex >= 0)
            {
                string peLinkedIn = dgvOutput.Rows[e.RowIndex].Cells["Pe_LinkedIn"].Value.ToString();

                // Gọi API SaleQL (Giả lập)
                dgvOutput.Rows[e.RowIndex].Cells["Unblock"].Value = "Loading...";

                var contact = await CallSaleQLAPI(peLinkedIn); // Hàm này bạn tự định nghĩa API Key

                if (contact != null)
                {
                    dgvOutput.Rows[e.RowIndex].Cells["Pe_Email"].Value = contact.Email;
                    dgvOutput.Rows[e.RowIndex].Cells["Pe_Phone"].Value = contact.Phone;
                    dgvOutput.Rows[e.RowIndex].Cells["Unblock"].Value = "Done";
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
         private async Task<string> CallGeminiAPI(string prompt)
        {
            // 1. Lấy key từ DB
            string apiKey = "";
            DataTable dtConfig = DatabaseHelper.ExecuteQuery("SELECT aistudio_key FROM Config LIMIT 1");
            if (dtConfig.Rows.Count > 0) apiKey = dtConfig.Rows[0]["aistudio_key"].ToString();
            if (string.IsNullOrEmpty(apiKey)) return "{}";

            try
            {
                // 2. Khởi tạo Client
                var client = new GenerativeModel(apiKey: apiKey, model: "gemini-3-flash-preview");

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
            DataTable dt = DatabaseHelper.ExecuteQuery("SELECT saleql_key FROM Config LIMIT 1");

            if (dt.Rows.Count == 0 || string.IsNullOrEmpty(dt.Rows[0]["saleql_key"].ToString()))
            {
                MessageBox.Show("Lỗi: Chưa tìm thấy SaleQL Key trong Database. Hãy lưu Key ở Form Config trước!");
                return null;
            }

            string apiKey = dt.Rows[0]["saleql_key"].ToString();
            // Endpoint chuẩn của SaleQL để lấy thông tin từ LinkedIn URL
            string url = $"https://api.saleql.com/v1/person/enrich?linkedin_url={Uri.EscapeDataString(linkedInUrl)}";

            using (HttpClient client = new HttpClient())
            {
                try
                {
                    // SaleQL thường yêu cầu API Key trong Header
                    client.DefaultRequestHeaders.Add("X-API-KEY", apiKey);
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
                        else { result.Email = "Not found"; }

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
                        else { result.Phone = "Not found"; }

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

        private void FormatGridView(string col1,string col2)
        {
            // 1. STT: Cho nhỏ lại và căn giữa
            dgvInput.Columns["STT"].Width = 40;
            dgvInput.Columns["STT"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            dgvInput.Columns["STT"].Resizable = DataGridViewTriState.False;

            // 2. Status: Cho nhỏ lại, vừa đủ hiển thị chữ "Completed"
            dgvInput.Columns["Status"].Width = 80;
            dgvInput.Columns["Status"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;

            // 3. Tên công ty (Shipper): Cho to ra và tự động lấp đầy khoảng trống
            dgvInput.Columns[""+ col1 + ""].AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;

            // 4. Các cột khác (Địa chỉ...): Cho hiển thị vừa phải
            if (dgvInput.Columns.Contains(""+col2+""))
            {
                dgvInput.Columns[""+ col2 + ""].Width = 600;
            }

            // Tùy chỉnh thêm để lưới trông sạch sẽ hơn
            dgvInput.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dgvInput.RowHeadersVisible = false; // Ẩn cột đầu dòng trống
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
            mydata = new DataTable();
            mydata.Columns.Add("STT");
            mydata.Columns.Add("CompanyName");
            mydata.Columns.Add("CompanyAddress");
            mydata.Columns.Add("Industry");
            mydata.Columns.Add("PhoneCo");
            mydata.Columns.Add("Website");
            mydata.Columns.Add("EmailCo");
            mydata.Columns.Add("LinkedInCo");
            mydata.Columns.Add("FullNamePe");
            mydata.Columns.Add("PositionPe");
            mydata.Columns.Add("LinkedInPe");
            mydata.Columns.Add("EmailPe");
            mydata.Columns.Add("PhonePe");
            mydata.Columns.Add("RowType");
            dgvOutput.DataSource = mydata;

            DataGridViewButtonColumn btnColumn = new DataGridViewButtonColumn();
            btnColumn.Name = "btnUnBlock";
            btnColumn.HeaderText = "Action";
            btnColumn.Text = "UnBlock";
            btnColumn.UseColumnTextForButtonValue = true;
            btnColumn.FlatStyle = FlatStyle.Flat;

            // 1. Căn lề nút sát bên trái của ô
            btnColumn.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleLeft;

            // 2. Chỉnh màu sắc
            btnColumn.DefaultCellStyle.BackColor = Color.FromArgb(40, 167, 69);
            btnColumn.DefaultCellStyle.ForeColor = Color.White;
            btnColumn.DefaultCellStyle.SelectionBackColor = Color.FromArgb(33, 136, 56);
            btnColumn.DefaultCellStyle.SelectionForeColor = Color.White;

            // 3. Sử dụng Padding để tạo khoảng cách và làm nút nhỏ lại
            // Padding(Left, Top, Right, Bottom)
            // - Top và Bottom = 5: Tạo khoảng cách để nút không dính chùm vào nút dòng trên/dưới
            // - Right = 20: Đẩy phần đuôi nút ngắn lại, kết hợp với Left=2 để nó sát lề trái
            btnColumn.DefaultCellStyle.Padding = new Padding(2, 5, 20, 5);

            // 4. Ép độ rộng của cột nhỏ lại vừa đủ
            btnColumn.Width = 80;
            btnColumn.AutoSizeMode = DataGridViewAutoSizeColumnMode.None;

            // 5. Chỉnh font chữ nhỏ lại cho cân đối với nút nhỏ
            btnColumn.DefaultCellStyle.Font = new Font("Segoe UI", 8, FontStyle.Regular);
            dgvOutput.Columns.Add(btnColumn);

            // 2. Tắt chế độ tự động điều chỉnh độ rộng của toàn bộ lưới
            dgvOutput.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None;
            dgvOutput.RowHeadersVisible = false;
            // 3. Cho phép xuống dòng để dữ liệu dài không bị mất
            dgvOutput.DefaultCellStyle.WrapMode = DataGridViewTriState.True;
            dgvOutput.AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.AllCells;
            // 4. Lặp qua tất cả các cột để set độ rộng 150 và tắt AutoSize từng cột
            foreach (DataGridViewColumn col in dgvOutput.Columns)
            {
                col.AutoSizeMode = DataGridViewAutoSizeColumnMode.None; // Tắt tự giãn
                col.Width = 150; // Set độ rộng cố định 150
            }

            // 5. Riêng cột STT bạn có thể để nhỏ hơn (ví dụ 50) nếu muốn, hoặc cứ để 150 theo ý bạn
            if (dgvOutput.Columns.Contains("STT"))
            {
                dgvOutput.Columns["STT"].Width = 50;
            }

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

        private void GenerateColumnFilters()
        {
            flpColumns.Controls.Clear();
            // Thiết lập hướng xếp hàng ngang
            flpColumns.FlowDirection = FlowDirection.LeftToRight;
            flpColumns.WrapContents = true; // Tự xuống dòng nếu quá nhiều cột

            foreach (DataGridViewColumn col in dgvOutput.Columns)
            {
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
            string savedConfig = Properties.Settings.Default.ColumnConfigs;
            if (string.IsNullOrEmpty(savedConfig)) return;

            string[] hiddenColumns = savedConfig.Split(',');

            foreach (DataGridViewColumn col in dgvOutput.Columns)
            {
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
        private bool IsCompanyProcessed(string companyName)
        {
            // FIX: Chỉ skip nếu đã có Website thực sự (khác N/A và khác rỗng)
            // Nếu lần trước search mà website = N/A hoặc trống → cho phép chạy lại để cập nhật
            string sql = @"SELECT COUNT(*) FROM Company 
                           WHERE TRIM(CompanyName) = TRIM($name) COLLATE NOCASE
                           AND Website IS NOT NULL 
                           AND TRIM(Website) != '' 
                           AND UPPER(TRIM(Website)) != 'N/A'";

            DataTable dt = DatabaseHelper.ExecuteQuery(sql, new[] { new SqliteParameter("$name", companyName) });

            if (dt.Rows.Count > 0 && Convert.ToInt32(dt.Rows[0][0]) > 0)
            {
                return true; // Đã chạy và có website thực sự → skip
            }
            return false; // Chưa chạy hoặc website trống/N/A → cho phép search lại
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
                        prompt = "Trích xuất thông tin từ text sau: \n***" + fullPageText + "***. nếu dữ liệu Đang cập nhật hay không có hay không có thông tin công khai về công ty hoặc nhân sự thì trả về N/A. Lấy TẤT CẢ số điện thoại và email tìm thấy. Nếu có nhiều, mỗi số nằm trên 1 dòng (cách nhau bằng \\n). QUAN TRỌNG: Nếu là số Fax, hãy thêm tiền tố 'Fax: ' vào trước số đó và để ở dòng bên dưới số điện thoại. \nTrả về JSON theo đúng schema: { 'companies': [{ 'name': '', 'website': '', 'address': '', 'phone': 'số1\\nFax: số2', 'email': 'mail1\\nmail2', 'linkedin': '', 'leaders': [{ 'name': '', 'position': '', 'linkedin': '', 'email': '', 'phone': '' }] }] }";
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
                                    SaveCrawlResult(item);
                                    CompanyAddress = item.address ?? "";
                                    Industry = item.industry ?? "";
                                    PhoneCo = item.phone ?? "";                                    
                                    Website = item.website ?? "";
                                    EmailCo = item.email ?? "";
                                    LinkedInCo = item.linkedin ?? "";

                                    // Lấy danh sách lãnh đạo
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

                                    // --- ROW CHA: Công ty ---
                                    DataRow dr = mydata.NewRow();
                                    dr["STT"] = row["STT"]?.ToString() ?? sttCount.ToString();
                                    sttCount++;
                                    dr["CompanyName"] = CompanyName;
                                    dr["CompanyAddress"] = CompanyAddress;
                                    dr["Industry"] = Industry;
                                    dr["PhoneCo"] = PhoneCo;
                                    dr["Website"] = Website;
                                    dr["EmailCo"] = EmailCo;
                                    dr["LinkedInCo"] = LinkedInCo;
                                    dr["RowType"] = "Company";
                                    this.Invoke(new Action(() =>
                                    {
                                        mydata.Rows.Add(dr);
                                        row["Status"] = "Completed";
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
    }
}
