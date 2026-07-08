using Microsoft.Data.Sqlite;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace crawlData
{
    public partial class FrmConfig : Form
    {
        public FrmConfig()
        {
            InitializeComponent();
            LoadConfig();
        }
        private void LoadConfig()
        {
            try
            {
                // Truy vấn lấy dòng cấu hình đầu tiên
                string sql = "SELECT * FROM Config LIMIT 1";
                DataTable dt = DatabaseHelper.ExecuteQuery(sql);

                if (dt.Rows.Count > 0)
                {
                    DataRow row = dt.Rows[0];

                    // Điền dữ liệu vào các TextBox (Thay tên TextBox cho đúng với project của bạn)
                    txtAiStudioKey.Text = row["aistudio_key"]?.ToString() ?? "";
                    txtOpenAIKey.Text = row["openai_key"]?.ToString() ?? "";
                    txtSaleQLKey.Text = row["salesql_key"]?.ToString() ?? (row["salesql_key"]?.ToString() ?? "");
                    txtProxy.Text = row["proxy"]?.ToString() ?? "";
                    
                    txtLinkedInID.Text = row["linkedin_id"]?.ToString() ?? "";
                    txtLinkedInPass.Text = row["linkedin_pass"]?.ToString() ?? "";
                    txtLinkedInCookies.Text = row["linkedin_cookies"]?.ToString() ?? "";
                    txtLinkedInKeywords.Text = row["linkedin_keywords"]?.ToString() ?? "manager|director|vp|president|chief|founder|owner|leader|executive|ceo|cto|cfo|coo|head|lead";

                    // 2Captcha Key
                    try { txt2CaptchaKey.Text = row["captcha_key"]?.ToString() ?? ""; } catch { }

                    // Gemini Model selection
                    try 
                    { 
                        string model = row["gemini_model"]?.ToString() ?? "";
                        if (string.IsNullOrEmpty(model) || !cboGeminiModel.Items.Contains(model))
                            cboGeminiModel.SelectedIndex = 0;
                        else
                            cboGeminiModel.SelectedItem = model;
                    } 
                    catch { cboGeminiModel.SelectedIndex = 0; }

                    // Nếu bạn có các cấu hình khác như đường dẫn Chrome, UserData
                    //txtChromePath.Text = row["chrome_path"]?.ToString() ?? "";
                    //txtUserDataPath.Text = row["userdata_path"]?.ToString() ?? "";

                    //lblStatus.Text = "Đã tải cấu hình từ database.";
                }
                else
                {
                    //lblStatus.Text = "Chưa có cấu hình cũ.";
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Không thể tải cấu hình: " + ex.Message);
            }
        }
        private void btnClose_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void btnSave_Click(object sender, EventArgs e)
        {
            try
            {
                // 1. Kiểm tra xem bảng Config có dòng nào chưa
                DataTable dt = DatabaseHelper.ExecuteQuery("SELECT * FROM Config LIMIT 1");

                string sql;
                SqliteParameter[] paras = {
                    new SqliteParameter("$aistudio_key", txtAiStudioKey.Text.Trim()),
                    new SqliteParameter("$openai_key", txtOpenAIKey.Text.Trim()),
                    new SqliteParameter("$salesql_key", txtSaleQLKey.Text.Trim()),
                    new SqliteParameter("$proxy", txtProxy.Text.Trim()),
                    new SqliteParameter("$linkedin_id", txtLinkedInID.Text.Trim()),
                    new SqliteParameter("$linkedin_pass", txtLinkedInPass.Text.Trim()),
                    new SqliteParameter("$linkedin_cookies", txtLinkedInCookies.Text.Trim()),
                    new SqliteParameter("$captcha_key", txt2CaptchaKey.Text.Trim()),
                    new SqliteParameter("$gemini_model", cboGeminiModel.SelectedItem?.ToString() ?? "gemini-2.5-flash"),
                    new SqliteParameter("$linkedin_keywords", txtLinkedInKeywords.Text.Trim())
                };

                if (dt.Rows.Count > 0)
                {
                    sql = "UPDATE Config SET aistudio_key = $aistudio_key, openai_key = $openai_key, salesql_key = $salesql_key, proxy = $proxy, linkedin_id = $linkedin_id, linkedin_pass = $linkedin_pass, linkedin_cookies = $linkedin_cookies, captcha_key = $captcha_key, gemini_model = $gemini_model, linkedin_keywords = $linkedin_keywords";
                }
                else
                {
                    sql = "INSERT INTO Config (aistudio_key, openai_key, salesql_key, proxy, linkedin_id, linkedin_pass, linkedin_cookies, captcha_key, gemini_model, linkedin_keywords) VALUES ($aistudio_key, $openai_key, $salesql_key, $proxy, $linkedin_id, $linkedin_pass, $linkedin_cookies, $captcha_key, $gemini_model, $linkedin_keywords)";
                }

                DatabaseHelper.ExecuteNonQuery(sql, paras);
                MessageBox.Show("Đã lưu thành công!");
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi: " + ex.Message);
            }
        }
    }
}
