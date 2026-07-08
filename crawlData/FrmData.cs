using ClosedXML.Excel;
using DocumentFormat.OpenXml.Spreadsheet;
using ExcelDataReader;
using GenerativeAI;
using GenerativeAI.Types;
using Microsoft.Data.Sqlite;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using OpenQA.Selenium.Remote;
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
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.ListView;

namespace crawlData
{
    public partial class FrmData : Form
    {
        // Khai báo ở đầu Class Form
        private CancellationTokenSource _cts;
        DataTable mydata;
        public FrmData()
        {
            InitializeComponent();      
            InitGridOutput();
            LoadColumnSettings();
            GenerateColumnFilters();

        }
        private void btnSearch_Click(object sender, EventArgs e)
        {
            string Companies = txtCompanyName.Text;
            string Industries = txtIndustry.Text;
            string Positions = txtPosition.Text;
            LoadToGrid(Companies, Industries, Positions);
            if (!dgvOutput.Columns.Contains("btnUnBlock"))
            {
                DataGridViewButtonColumn btnColumn = new DataGridViewButtonColumn();
                btnColumn.Name = "btnUnBlock";
                btnColumn.HeaderText = "Action";
                btnColumn.Text = "UnBlock";
                btnColumn.UseColumnTextForButtonValue = true;
                btnColumn.FlatStyle = FlatStyle.Flat;
                btnColumn.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleLeft;
                btnColumn.DefaultCellStyle.BackColor = System.Drawing.Color.FromArgb(40, 167, 69);
                btnColumn.DefaultCellStyle.ForeColor = System.Drawing.Color.White;
                btnColumn.DefaultCellStyle.SelectionBackColor = System.Drawing.Color.FromArgb(33, 136, 56);
                btnColumn.DefaultCellStyle.SelectionForeColor = System.Drawing.Color.White;
                btnColumn.DefaultCellStyle.Padding = new Padding(2, 5, 20, 5);
                btnColumn.Width = 80;
                btnColumn.AutoSizeMode = DataGridViewAutoSizeColumnMode.None;
                btnColumn.DefaultCellStyle.Font = new System.Drawing.Font("Segoe UI", 8, FontStyle.Regular);
                btnColumn.DisplayIndex = dgvOutput.Columns.Count;
                dgvOutput.Columns.Add(btnColumn);
            }
        }
        private void DataGridView1_RowPostPaint(object sender, DataGridViewRowPostPaintEventArgs e)
        {
            dgvOutput.Rows[e.RowIndex].Cells[0].Value = (e.RowIndex + 1).ToString();
        }
        private void btnClose_Click(object sender, EventArgs e)
        {
            this.Close();
        }
        private void LoadToGrid(string companies, string industries,string positions)
        {
            // 1️⃣ Tách & lọc dữ liệu từ RichTextBox
            var companyList = companies
                .Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries)
                .Select(x => x.Trim())
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .ToList();

            var industryList = industries
                .Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries)
                .Select(x => x.Trim())
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .ToList();

            var positionList = positions
              .Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries)
              .Select(x => x.Trim())
              .Where(x => !string.IsNullOrWhiteSpace(x))
              .ToList();

            // ❗ Chỉ chặn khi CẢ HAI list đều rỗng
            if (!companyList.Any() && !industryList.Any() && !positionList.Any())
            {
                MessageBox.Show("Vui lòng nhập ít nhất 1 điều kiện tìm kiếm");
                return;
            }

            // 2️⃣ Build WHERE clause
            var conditions = new List<string>();
            var parameters = new List<SqliteParameter>();

            // CompanyName LIKE
            if (companyList.Any())
            {
                var companyLikes = new List<string>();
                for (int i = 0; i < companyList.Count; i++)
                {
                    string paramName = $"$c{i}";
                    companyLikes.Add($"TRIM(c.CompanyName) LIKE '%' || {paramName} || '%' COLLATE NOCASE");
                    parameters.Add(new SqliteParameter(paramName, companyList[i]));
                }
                conditions.Add("(" + string.Join(" OR ", companyLikes) + ")");
            }

            // Industry LIKE
            if (industryList.Any())
            {
                var industryLikes = new List<string>();
                for (int i = 0; i < industryList.Count; i++)
                {
                    string paramName = $"$i{i}";
                    industryLikes.Add($"TRIM(c.Industry) LIKE '%' || {paramName} || '%' COLLATE NOCASE");
                    parameters.Add(new SqliteParameter(paramName, industryList[i]));
                }
                conditions.Add("(" + string.Join(" OR ", industryLikes) + ")");
            }
            if (positionList.Any())
            {
                var positionListLikes = new List<string>();
                for (int i = 0; i <positionList.Count; i++)
                {
                    string paramName = $"$j{i}";
                    positionListLikes.Add($"TRIM(p.Position) LIKE '%' || {paramName} || '%' COLLATE NOCASE");
                    parameters.Add(new SqliteParameter(paramName, positionList[i]));
                }
                conditions.Add("(" + string.Join(" OR ", positionListLikes) + ")");
            }
            string whereClause = "WHERE " + string.Join(" AND ", conditions);

            // 3️⃣ SQL hoàn chỉnh
            string sql = $@"SELECT '' as STT,c.ID as CompanyID,c.CompanyName AS CompanyName, c.Address AS CompanyAddress, c.Industry AS Industry, c.Phone AS PhoneCo, c.Website AS Website, c.Email AS EmailCo, c.Linkedin AS LinkedInCo,p.ID as PersonID, p.FullName AS FullNamePe, p.Position AS PositionPe, p.Linkedin AS LinkedInPe, p.Email AS EmailPe, p.Phone AS PhonePe 
                    FROM Company c
                    LEFT JOIN Person p ON c.ID = p.CompanyID
                    {whereClause};
                    ";

            // 4️⃣ Execute & bind grid → chuyển thành cha-con
            DataTable rawData = DatabaseHelper.ExecuteQuery(sql, parameters.ToArray());
            mydata.Rows.Clear();
            
            string lastCompanyId = "";
            int stt = 0;
            int personIndex = 0;
            
            foreach (DataRow raw in rawData.Rows)
            {
                string companyId = raw["CompanyID"]?.ToString() ?? "";
                
                if (companyId != lastCompanyId)
                {
                    // ROW CHA: Công ty
                    stt++;
                    personIndex = 0;
                    DataRow drCompany = mydata.NewRow();
                    drCompany["STT"] = stt;
                    drCompany["CompanyID"] = companyId;
                    drCompany["CompanyName"] = raw["CompanyName"]?.ToString() ?? "";
                    drCompany["CompanyAddress"] = raw["CompanyAddress"]?.ToString() ?? "";
                    drCompany["Industry"] = raw["Industry"]?.ToString() ?? "";
                    drCompany["PhoneCo"] = raw["PhoneCo"]?.ToString() ?? "";
                    drCompany["Website"] = raw["Website"]?.ToString() ?? "";
                    drCompany["EmailCo"] = raw["EmailCo"]?.ToString() ?? "";
                    drCompany["LinkedInCo"] = raw["LinkedInCo"]?.ToString() ?? "";
                    mydata.Rows.Add(drCompany);
                    lastCompanyId = companyId;
                }
                
                // ROW CON: Nhân sự (nếu có)
                string fullName = raw["FullNamePe"]?.ToString() ?? "";
                if (!string.IsNullOrEmpty(fullName))
                {
                    personIndex++;
                    DataRow drPerson = mydata.NewRow();
                    drPerson["STT"] = "  ├ " + personIndex;
                    drPerson["CompanyName"] = "";
                    drPerson["PersonID"] = raw["PersonID"]?.ToString() ?? "";
                    drPerson["FullNamePe"] = fullName;
                    drPerson["PositionPe"] = raw["PositionPe"]?.ToString() ?? "";
                    drPerson["LinkedInPe"] = raw["LinkedInPe"]?.ToString() ?? "";
                    drPerson["EmailPe"] = raw["EmailPe"]?.ToString() ?? "";
                    drPerson["PhonePe"] = raw["PhonePe"]?.ToString() ?? "";
                    mydata.Rows.Add(drPerson);
                }
            }
            
            dgvOutput.DataSource = mydata;
            lblOutput.Text = "OUTPUT: " + mydata.Rows.Count.ToString();
        }
        


        private async void dgvOutput_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            if (dgvOutput.Columns[e.ColumnIndex].Name == "btnUnBlock" && e.RowIndex >= 0)
            {
                string peLinkedIn = dgvOutput.Rows[e.RowIndex].Cells["LinkedInPe"].Value.ToString();

                // Gọi API SaleQL (Giả lập)
                dgvOutput.Rows[e.RowIndex].Cells["btnUnBlock"].Value = "Loading...";

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
            string url = $"https://api-public.salesql.com/v1/persons/enrich?linkedin_url={Uri.EscapeDataString(linkedInUrl)}&api_key="+ apiKey;
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
        private string GetValueAfterLabel(string text, string label)
        {
            var line = text.Split('\n').FirstOrDefault(l => l.Contains(label));
            if (line != null && line.Contains(":"))
            {
                return line.Substring(line.IndexOf(":") + 1).Trim();
            }
            return "Đang cập nhật";
        }
        private void InitGridOutput()
        {
            mydata = new DataTable();
            mydata.Columns.Add("STT");
            mydata.Columns.Add("CompanyID");
            mydata.Columns.Add("CompanyName");
            mydata.Columns.Add("CompanyAddress");
            mydata.Columns.Add("Industry");
            mydata.Columns.Add("PhoneCo");
            mydata.Columns.Add("Website");
            mydata.Columns.Add("EmailCo");
            mydata.Columns.Add("PersonID");
            mydata.Columns.Add("LinkedInCo");
            mydata.Columns.Add("FullNamePe");
            mydata.Columns.Add("PositionPe");
            mydata.Columns.Add("LinkedInPe");
            mydata.Columns.Add("EmailPe");
            mydata.Columns.Add("PhonePe");
            dgvOutput.DataSource = mydata;           

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
            Properties.Settings.Default.ColumnDataConfigs = string.Join(",", hiddenColumns);
            Properties.Settings.Default.Save();
        }
        private void LoadColumnSettings()
        {
            string savedConfig = Properties.Settings.Default.ColumnDataConfigs;
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

                        // Header
                        for (int i = 0; i < headerTexts.Length; i++)
                        {
                            var cell = worksheet.Cell(1, i + 1);
                            cell.Value = headerTexts[i];
                            cell.Style.Font.Bold = true;
                            cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#4F81BD");
                            cell.Style.Font.FontColor = XLColor.White;
                            cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                        }

                        // Data - cha con với merge cells
                        int rowIndex = 2;
                        int companySTT = 0;
                        int companyStartRow = -1;

                        foreach (DataRow row in mydata.Rows)
                        {
                            string sttVal = row["STT"]?.ToString() ?? "";
                            bool isChild = sttVal.Contains("├");

                            if (!isChild)
                            {
                                // Merge nhóm trước
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

                                worksheet.Cell(rowIndex, 1).Value = companySTT.ToString();
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

                        // Merge nhóm cuối
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
        public class SaleQLResult
        {
            public string Email { get; set; }
            public string Phone { get; set; }
        }

      
        private void btnDeleteData_Click(object sender, EventArgs e)
        {
            var confirm = MessageBox.Show(
                "Bạn có chắc chắn muốn XÓA TẤT CẢ dữ liệu công ty và nhân sự?\n\nHành động này không thể hoàn tác!",
                "Xác nhận xóa dữ liệu",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning);

            if (confirm != DialogResult.Yes) return;

            try
            {
                // Xóa Person trước (FK → Company)
                DatabaseHelper.ExecuteNonQuery("DELETE FROM Person");
                // Xóa Company
                DatabaseHelper.ExecuteNonQuery("DELETE FROM Company");

                // Clear grid
                mydata.Clear();
                dgvOutput.DataSource = mydata;
                lblOutput.Text = "OUTPUT: 0";

                MessageBox.Show("Đã xóa toàn bộ dữ liệu công ty và nhân sự thành công!",
                    "Thành công", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi khi xóa dữ liệu: " + ex.Message,
                    "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}
